using Microsoft.Extensions.Caching.Memory;

using MonitoringBot;
using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.BackgroundJobs;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.BotUsers;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Infrastructure.Settings;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;

using Serilog;

using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

public class MonitoringBotRunner<TEntity, TOnJoinedEvent, TOnLeftEvent>
    where TEntity : ISearchableEntity
    where TOnJoinedEvent : EntitiesChangedDomainEventBase<TEntity>, new()
    where TOnLeftEvent : EntitiesChangedDomainEventBase<TEntity>, new()
{
    public MonitoringBotRunner(
        EntitiesChangeMessagingService<ChannelMember> messagesSendingService,
        TelegramBotClient telegramBotClient,
        FetchUsersBackgroundServiceBase fetchUsersBackgroundService,
        MessageBuilder messageBuilder,
        SubscribersChangeProcessor subscribersChangeProcessor,
        IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor,
        IMonitoringService<TEntity, TOnJoinedEvent, TOnLeftEvent> monitoringService,
        EntitiesChangeMessageProducer<ChannelMember> entitiesChangeMessageProducer,
        ITimeProvider timeProvider,
        SnapshotCollectingJobScheduler<TEntity, TOnJoinedEvent, TOnLeftEvent> snapshotCollectingJobScheduler,
        TelegramBotSettings telegramBotSettings,
        TelegramApiSettings telegramApiSettings,
        MonitoringCancellationContext cancellationContext,
        AddBotUserCommand addBotUserCommand,
        IMemoryCache memoryCache,
        CacheKeysFactory cacheKeysFactory,
        GetActiveBotUsersQuery getActiveBotUsersQuery)
    {
        this.messagesSendingService = messagesSendingService;
        this.telegramBotClient = telegramBotClient;
        this.fetchUsersBackgroundService = fetchUsersBackgroundService;
        this.messageBuilder = messageBuilder;
        this.subscribersChangeProcessor = subscribersChangeProcessor;
        this.eventsMonitoringProcessor = eventsMonitoringProcessor;
        this.monitoringService = monitoringService;
        this.entitiesChangeMessageProducer = entitiesChangeMessageProducer;
        this.timeProvider = timeProvider;
        this.snapshotCollectingJobScheduler = snapshotCollectingJobScheduler;
        this.telegramBotSettings = telegramBotSettings;
        this.telegramApiSettings = telegramApiSettings;
        this.cancellationContext = cancellationContext;
        this.addBotUserCommand = addBotUserCommand;
        this.memoryCache = memoryCache;
        this.cacheKeysFactory = cacheKeysFactory;
        this.getActiveBotUsersQuery = getActiveBotUsersQuery;
    }

    private readonly EntitiesChangeMessagingService<ChannelMember> messagesSendingService;
    private readonly TelegramBotClient telegramBotClient;
    private readonly FetchUsersBackgroundServiceBase fetchUsersBackgroundService;
    private readonly MessageBuilder messageBuilder;
    private readonly SubscribersChangeProcessor subscribersChangeProcessor;
    private readonly IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor;
    private readonly IMonitoringService<TEntity, TOnJoinedEvent, TOnLeftEvent> monitoringService;
    private readonly EntitiesChangeMessageProducer<ChannelMember> entitiesChangeMessageProducer;
    private readonly ITimeProvider timeProvider;
    private readonly SnapshotCollectingJobScheduler<TEntity, TOnJoinedEvent, TOnLeftEvent> snapshotCollectingJobScheduler;
    private readonly TelegramBotSettings telegramBotSettings;
    private readonly TelegramApiSettings telegramApiSettings;
    private readonly MonitoringCancellationContext cancellationContext;
    private readonly AddBotUserCommand addBotUserCommand;
    private readonly IMemoryCache memoryCache;
    private readonly CacheKeysFactory cacheKeysFactory;
    private readonly GetActiveBotUsersQuery getActiveBotUsersQuery;

    private int checkPeriodSeconds;
    private DateTime basicTimeStamp;
    private IEnumerable<Update>? updates;
    private DateTime beginWorkingFrom;
    private bool isActive;
    private readonly CancellationTokenSource botCancellationTokenSource = new();

    private async Task ProcessMonitoringByPeriodAsync(CancellationToken cancellationToken)
    {
        if ((timeProvider.UtcNow - basicTimeStamp).TotalSeconds > checkPeriodSeconds && !cancellationToken.IsCancellationRequested)
        {
            await monitoringService.ProcessMonitoringAsync(cancellationContext.Token);
            basicTimeStamp = timeProvider.UtcNow;
        }

        if(cancellationToken.IsCancellationRequested)
            Log.Debug("Monitoring was not executed due to cancellation.");
    }

    private async Task CheckAndProcessInputsAsync()
    {
        if (updates is not null && updates.Any())
        {
            var currentUpdateContext = new ServiceContext { CancellationToken = botCancellationTokenSource.Token, CorrelationId = Guid.NewGuid() };

            Log.Information("Получен пользовательский ввод.");
            foreach (var update in updates)
            {
                string? message = update?.Message?.Text;
                if (message is null)
                {
                    Log.Warning($"Сообщение равно null");
                    continue;
                }

                long? chatId = update?.Message?.Chat.Id;

                Log.Information($"От пользователя {chatId} получена команда {message}.");

                //if (chatId is not null && !telegramBotSettings.ChatIdsCollection.Contains(chatId.Value))
                //{
                //    telegramBotSettings.ChatIdsCollection.Add(chatId.Value);
                //    Log.Information($"В рассылку бота добавлен новый пользователь с Id {chatId.Value}");
                //}

                await AddBotUserIfNew(update?.Message?.Chat, currentUpdateContext);

                string? resultMessage = message switch
                {
                    "/info" => messageBuilder.Info(telegramApiSettings.ChannelReferenceLink),
                    "/last" => await messageBuilder.LastAsync(cancellationToken: cancellationContext.Token),
                    "/last_subscribed" => await messageBuilder.LastSubscribedAsync(cancellationToken: botCancellationTokenSource.Token),
                    "/last_unsubscribed" => await messageBuilder.LastUnsubscribedAsync(cancellationToken: botCancellationTokenSource.Token),
                    "/change_period" => "будет менять период мониторинга",
                    "/check" => await messageBuilder.CheckDiagnosticsAsync(
                                      checkPeriodSeconds,
                                      beginWorkingFrom,
                                      fetchUsersBackgroundService.GetState(),
                                      telegramApiSettings.ChannelReferenceLink,
                                      isActive,
                                      botCancellationTokenSource.Token),
                    "/count_history" => await messageBuilder.CountHistoryAsync(telegramApiSettings.ChannelReferenceLink, currentUpdateContext),
                    "/history_snapshot" => await messageBuilder.HistorySnapshotAsync(
                        telegramApiSettings.ChannelReferenceLink, 
                        currentUpdateContext),
                    "//stop_service" => await StopAsync(chatId.Value),
                    "//restart_service" => await RestartAsync(chatId.Value),
                    _ => "это не известная мне команда..."
                };

                if (resultMessage is not null && chatId.HasValue)
                {
                    await messagesSendingService.TrySendMessageAsync(chatId.Value, resultMessage, botCancellationTokenSource.Token);
                    Log.Information($"Пользователю {chatId} отправлен ответ на {message}: '{resultMessage.TakeAndFormatFirst(300)}'.");
                }
            }

            var offset = updates?.Last().UpdateId + 1;

            try
            {
                updates = await telegramBotClient.GetUpdatesAsync(offset);
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}\nОбновления не получены");
            }
        }
        else
        {
            try
            {
                updates = await telegramBotClient.GetUpdatesAsync();
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}\nОбновления не получены");
            }
        }
    }

    public async Task InitializeAsync()
    {
        var baseServiceContext = new ServiceContext { CancellationToken = cancellationContext.Token, CorrelationId = Guid.NewGuid() };

        checkPeriodSeconds = telegramBotSettings.CheckPeriodSeconds;
        basicTimeStamp = timeProvider.UtcNow;
        beginWorkingFrom = timeProvider.UtcNow;

        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor.OnSubscribersQuantityChanged;

        eventsMonitoringProcessor.EntitiesJoined += entitiesChangeMessageProducer.OnEntitiesJoined;
        eventsMonitoringProcessor.EntitiesLeft += entitiesChangeMessageProducer.OnEntitiesLeft;

        entitiesChangeMessageProducer.MessageProduced += messagesSendingService.OnMessageProduced;

        updates = await telegramBotClient.GetUpdatesAsync(cancellationToken: cancellationContext.Token);

        var botUsers = await getActiveBotUsersQuery.ExecuteAsync(baseServiceContext);

        await fetchUsersBackgroundService.LoginAsync();
        bool apiServiceInitializationSuccess = await fetchUsersBackgroundService.InitializeServiceAsync();
        if(!apiServiceInitializationSuccess)
        {
            var message = $"Не удалось корректно инициализировать сервис сбора подписчиков '${nameof(TelegramChannelService)}'.";
            Log.Fatal(message);
            await messagesSendingService.TrySendMessageForAllAsync(
                GetCurrentBotUsersIds(botUsers), 
                message,
                baseServiceContext);
            return;
        }

        fetchUsersBackgroundService.Start();

        await messagesSendingService.TrySendMessageForAllAsync(
            GetCurrentBotUsersIds(botUsers),
            "Я загрузился🚀! Наблюдаю...  👀🔎",
            new() { CancellationToken = cancellationContext.Token, CorrelationId = Guid.NewGuid() });

        Log.Information($"{GetType().Name} загрузился успешно.");

        //await monitoringService.ProcessMonitoringAsync(); // TODO: получить существующих юзеров для мониторинга из базы, когда она таки-будет

        await snapshotCollectingJobScheduler.ScheduleAsync(new() 
            { 
                AggregateName = telegramApiSettings.ChannelReferenceLink, 
                TimeStamp = timeProvider.UtcNow 
            },
            baseServiceContext,
            cancellationContext.Token);

        isActive = true;
    }

    private async Task<string> StopAsync(long userId)
    {
        await cancellationContext.CancelAsync();
        await snapshotCollectingJobScheduler.PauseAsync();
        isActive = false;

        Log.Warning("⛔Работа по мониторингу остановлена.⛔");
        return "Работа по мониторингу остановлена.";
    }

    private async Task<string> RestartAsync(long userId)
    {
        await cancellationContext.RestartAsync();
        var baseServiceContext = new ServiceContext { CancellationToken = cancellationContext.Token, CorrelationId = Guid.NewGuid() };
        await snapshotCollectingJobScheduler.ResumeAsync();

        fetchUsersBackgroundService.Start();
        isActive = true;

        Log.Warning("🔄Работа по мониторингу возобновлена.✅");
        return "🔄Работа по мониторингу возобновлена.✅";
    }

    private async Task AddBotUserIfNew(Chat? chat, ServiceContext serviceContext)
    {
        long? chatId = chat?.Id;
        var currentUsers = GetCachedUsers();

        if (chatId is not null && !currentUsers.Any(u => u.Id == chatId.Value))
        {
            var newUser = new BotUser
            {
                Id = chatId.Value,
                FirstName = chat.FirstName,
                LastName = chat.LastName,
                IsForum = chat.IsForum,
                Title = chat.Title,
                Type = chat.Type,
                UserName = chat.Username
            };

            currentUsers.Add(newUser);
            memoryCache.Set<HashSet<BotUser>>(cacheKeysFactory.BotUsersKey(), currentUsers);
            memoryCache.Set<int>(cacheKeysFactory.BotUsersCount(), currentUsers.Count);

            await addBotUserCommand.ExecuteAsync(
                newUser,
                serviceContext);

            Log.Information($"В рассылку бота добавлен новый пользователь с Id {chatId.Value}");
        }
    }

    private List<long> GetCurrentBotUsersIds(IReadOnlyCollection<BotUser> users) => users.Select(u => u.Id).ToList();

    private HashSet<BotUser> GetCachedUsers()
    {
        _ = memoryCache.TryGetValue(cacheKeysFactory.BotUsersKey(), out object? cachedUsersObject);

        if (cachedUsersObject is HashSet<BotUser> users)
            return users;
        return [];
    }

    public async Task MainLoopAsync()
    {
        while (true)
        {
            try
            {
                await ProcessMonitoringByPeriodAsync(cancellationContext.Token);
                await CheckAndProcessInputsAsync();
            }
            catch (OperationCanceledException ex)
            {
                Log.Fatal($"Bot stopped due to unexpected cancellation: {ex.Message}");
                break;
            }
        }
    }
}
