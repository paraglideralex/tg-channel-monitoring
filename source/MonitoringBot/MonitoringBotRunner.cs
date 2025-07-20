using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.BackgroundJobs;
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
        CancellationContext cancellationContext)
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
    private readonly CancellationContext cancellationContext; 

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

                long userId = update!.Message!.Chat.Id;

                Log.Information($"От пользователя {userId} получена команда {message}.");

                long chatId = update.Message.Chat.Id;
                if (!telegramBotSettings.ChatIdsCollection.Contains(chatId))
                {
                    telegramBotSettings.ChatIdsCollection.Add(chatId);
                    Log.Information($"В рассылку бота добавлен новый пользователь с Id {chatId}");
                }

                string? resultMessage = message switch
                {
                    "/info" => messageBuilder.Info(telegramApiSettings.ChannelReferenceLink),
                    "/last" => await messageBuilder.LastAsync(cancellationToken: cancellationContext.Token),
                    "/last_subscribed" => await messageBuilder.LastSubscribedAsync(cancellationToken: botCancellationTokenSource.Token),
                    "/last_unsubscribed" => await messageBuilder.LastUnsubscribedAsync(cancellationToken: botCancellationTokenSource.Token),
                    "/change_period" => "будет менять период мониторинга",
                    "/check" => await messageBuilder.CheckDiagnosticsAsync(
                                      checkPeriodSeconds, 
                                      telegramBotSettings.ChatIdsCollection.Count,
                                      beginWorkingFrom,
                                      fetchUsersBackgroundService.GetState(),
                                      telegramApiSettings.ChannelReferenceLink,
                                      isActive,
                                      botCancellationTokenSource.Token),
                    "/count_history" => await messageBuilder.CountHistoryAsync(telegramApiSettings.ChannelReferenceLink, currentUpdateContext),
                    "/history_snapshot" => await messageBuilder.HistorySnapshotAsync(
                        telegramApiSettings.ChannelReferenceLink, 
                        currentUpdateContext),
                    "//stop_service" => await StopAsync(userId),
                    "//restart_service" => await RestartAsync(userId),
                    _ => "это не известная мне команда..."
                };

                if (resultMessage is not null)
                {
                    await messagesSendingService.TrySendMessageAsync(chatId, resultMessage, botCancellationTokenSource.Token);
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
        await fetchUsersBackgroundService.LoginAsync();
        bool apiServiceInitializationSuccess = await fetchUsersBackgroundService.InitializeServiceAsync();
        if(!apiServiceInitializationSuccess)
        {
            var message = $"Не удалось корректно инициализировать сервис сбора подписчиков '${nameof(TelegramChannelService)}'.";
            Log.Fatal(message);
            await messagesSendingService.TrySendMessageForAllAsync(
                telegramBotSettings.ChatIdsCollection, 
                message,
                baseServiceContext);
            return;
        }

        fetchUsersBackgroundService.Start();

        await messagesSendingService.TrySendMessageForAllAsync(
            telegramBotSettings.ChatIdsCollection,
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
