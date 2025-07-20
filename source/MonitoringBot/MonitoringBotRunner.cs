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

using System.Threading;

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
        TelegramApiSettings telegramApiSettings)
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

    private int checkPeriodSeconds;
    private DateTime basicTimeStamp;
    private IEnumerable<Update>? updates;
    private DateTime beginWorkingFrom;
    private CancellationTokenSource cancellationTokenSource;
    
    private async Task ProcessMonitoringByPeriodAsync(CancellationToken cancellationToken)
    {
        if ((timeProvider.UtcNow - basicTimeStamp).TotalSeconds > checkPeriodSeconds)
        {
            await monitoringService.ProcessMonitoringAsync(cancellationTokenSource.Token);
            basicTimeStamp = timeProvider.UtcNow;
        }
    }

    private async Task CheckAndProcessInputsAsync()
    {
        if (updates is not null && updates.Any())
        {
            var currentUpdateContext = new ServiceContext { CancellationToken = cancellationTokenSource.Token, CorrelationId = Guid.NewGuid() };

            Log.Information("Получен пользовательский ввод.");
            foreach (var update in updates)
            {
                string? message = update?.Message?.Text;
                if (message is null)
                {
                    Log.Warning($"Сообщение равно null");
                    continue;
                }

                Log.Information($"От пользователя {update!.Message!.Chat.Id} получена команда {message}.");

                long chatId = update.Message.Chat.Id;
                if (!telegramBotSettings.ChatIdsCollection.Contains(chatId))
                {
                    telegramBotSettings.ChatIdsCollection.Add(chatId);
                    Log.Information($"В рассылку бота добавлен новый пользователь с Id {chatId}");
                }

                string? resultMessage = message switch
                {
                    "/info" => messageBuilder.Info(telegramApiSettings.ChannelReferenceLink),
                    "/last" => await messageBuilder.LastAsync(cancellationToken: cancellationTokenSource.Token),
                    "/last_subscribed" => await messageBuilder.LastSubscribedAsync(cancellationToken: cancellationTokenSource.Token),
                    "/last_unsubscribed" => await messageBuilder.LastUnsubscribedAsync(cancellationToken: cancellationTokenSource.Token),
                    "/change_period" => "будет менять период мониторинга",
                    "/check" => await messageBuilder.CheckDiagnosticsAsync(
                                      checkPeriodSeconds, 
                                      telegramBotSettings.ChatIdsCollection.Count,
                                      beginWorkingFrom,
                                      fetchUsersBackgroundService.GetState(),
                                      telegramApiSettings.ChannelReferenceLink,
                                      cancellationTokenSource.Token),
                    "/count_history" => await messageBuilder.CountHistoryAsync(telegramApiSettings.ChannelReferenceLink, currentUpdateContext),
                    "/history_snapshot" => await messageBuilder.HistorySnapshotAsync(
                        telegramApiSettings.ChannelReferenceLink, 
                        currentUpdateContext),
                    _ => "это не известная мне команда..."
                };

                if (resultMessage is not null)
                {
                    await messagesSendingService.TrySendMessageAsync(chatId, resultMessage, cancellationTokenSource.Token);
                    Log.Information($"Пользователю {chatId} отправлен ответ на {message}: '{resultMessage.TakeAndFormatFirst(300)}'.");
                }
            }

            var offset = updates.Last().UpdateId + 1;

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
        cancellationTokenSource = new CancellationTokenSource();

        checkPeriodSeconds = telegramBotSettings.CheckPeriodSeconds;
        basicTimeStamp = timeProvider.UtcNow;
        beginWorkingFrom = timeProvider.UtcNow;

        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor.OnSubscribersQuantityChanged;

        eventsMonitoringProcessor.EntitiesJoined += entitiesChangeMessageProducer.OnEntitiesJoined;
        eventsMonitoringProcessor.EntitiesLeft += entitiesChangeMessageProducer.OnEntitiesLeft;

        entitiesChangeMessageProducer.MessageProduced += messagesSendingService.OnMessageProduced;

        updates = await telegramBotClient.GetUpdatesAsync(cancellationToken: cancellationTokenSource.Token);
        await fetchUsersBackgroundService.LoginAsync();
        bool apiServiceInitializationSuccess = await fetchUsersBackgroundService.InitializeServiceAsync();
        if(!apiServiceInitializationSuccess)
        {
            var message = $"Не удалось корректно инициализировать сервис сбора подписчиков '${nameof(TelegramChannelService)}'.";
            Log.Fatal(message);
            await messagesSendingService.TrySendMessageForAllAsync(
                telegramBotSettings.ChatIdsCollection, 
                message,
                new() { CancellationToken = cancellationTokenSource.Token, CorrelationId = Guid.NewGuid()});
            return;
        }

        fetchUsersBackgroundService.Start();

        await messagesSendingService.TrySendMessageForAllAsync(
            telegramBotSettings.ChatIdsCollection,
            "Я загрузился🚀! Наблюдаю...  👀🔎",
            new() { CancellationToken = cancellationTokenSource.Token, CorrelationId = Guid.NewGuid() });

        Log.Information($"{GetType()} загрузился успешно.");

        //await monitoringService.ProcessMonitoringAsync(); // TODO: получить существующих юзеров для мониторинга из базы, когда она таки-будет

        await snapshotCollectingJobScheduler.ScheduleAsync(new() 
            { 
                AggregateName = telegramApiSettings.ChannelReferenceLink, 
                TimeStamp = timeProvider.UtcNow 
            }, 
            cancellationTokenSource.Token);
    }

    public async Task MainLoopAsync()
    {
        while (true)
        {
            try
            {
                await ProcessMonitoringByPeriodAsync(cancellationTokenSource.Token);
                await CheckAndProcessInputsAsync();
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Monitoring stopped");
                break;
            }
        }
    }
}
