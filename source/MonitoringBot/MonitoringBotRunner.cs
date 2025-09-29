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
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

public class MonitoringBotRunner<TEntity, TOnJoinedEvent, TOnLeftEvent>(
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
    IBotUsersService botUsersService)
        where TEntity : ISearchableEntity
        where TOnJoinedEvent : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEvent : EntitiesChangedDomainEventBase<TEntity>, new()
{
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
                    { } s when s.StartsWith("/isDick", StringComparison.OrdinalIgnoreCase) => await messageBuilder.IsDickAsync(s, currentUpdateContext),
                    "//stop_service" => await StopAsync(chatId),
                    "//restart_service" => await RestartAsync(chatId),
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
        var initializationServiceContext = new ServiceContext { CancellationToken = cancellationContext.Token, CorrelationId = Guid.NewGuid() };

        checkPeriodSeconds = telegramBotSettings.CheckPeriodSeconds;
        basicTimeStamp = timeProvider.UtcNow;
        beginWorkingFrom = timeProvider.UtcNow;

        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor.OnSubscribersQuantityChanged;

        eventsMonitoringProcessor.EntitiesJoined += entitiesChangeMessageProducer.OnEntitiesJoined;
        eventsMonitoringProcessor.EntitiesLeft += entitiesChangeMessageProducer.OnEntitiesLeft;

        entitiesChangeMessageProducer.MessageProduced += messagesSendingService.OnMessageProduced;

        updates = await telegramBotClient.GetUpdatesAsync(cancellationToken: cancellationContext.Token);

        await botUsersService.SynchronizeAsync(initializationServiceContext);
        var cachedBotUsersIds = botUsersService.GetCachedBotUsersIds();

        await fetchUsersBackgroundService.LoginAsync();
        bool apiServiceInitializationSuccess = await fetchUsersBackgroundService.InitializeServiceAsync();
        if(!apiServiceInitializationSuccess)
        {
            var message = $"Не удалось корректно инициализировать сервис сбора подписчиков '${nameof(TelegramChannelService)}'.";
            Log.Fatal(message);
            await messagesSendingService.TrySendMessageForAllAsync(
                cachedBotUsersIds, 
                message,
                initializationServiceContext);
            return;
        }

        fetchUsersBackgroundService.Start();

        await messagesSendingService.TrySendMessageForAllAsync(
            cachedBotUsersIds,
            messageBuilder.Loaded(),
            initializationServiceContext);

        Log.Information($"{GetType().Name} загрузился успешно.");

        await snapshotCollectingJobScheduler.ScheduleAsync(new() 
            { 
                AggregateName = telegramApiSettings.ChannelReferenceLink, 
                TimeStamp = timeProvider.UtcNow 
            },
            initializationServiceContext,
            cancellationContext.Token);

        isActive = true;
    }

    private async Task<string> StopAsync(long? userId)
    {
        await cancellationContext.CancelAsync();
        await snapshotCollectingJobScheduler.PauseAsync();
        isActive = false;

        Log.Warning("⛔Работа по мониторингу остановлена.⛔");
        return messageBuilder.Paused();
    }

    private async Task<string> RestartAsync(long? userId)
    {
        await cancellationContext.RestartAsync();
        var baseServiceContext = new ServiceContext { CancellationToken = cancellationContext.Token, CorrelationId = Guid.NewGuid() };
        await snapshotCollectingJobScheduler.ResumeAsync();

        fetchUsersBackgroundService.Start();
        isActive = true;

        Log.Warning("🔄Работа по мониторингу возобновлена.✅");
        return messageBuilder.Resumed();
    }

    private async Task AddBotUserIfNew(Chat? chat, ServiceContext serviceContext)
    {
        long? chatId = chat?.Id;

        if (chat is not null && chatId is not null)
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

            await botUsersService.AddBotUserIfNew(newUser, serviceContext);
        }
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
