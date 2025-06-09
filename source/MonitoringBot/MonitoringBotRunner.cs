using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
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
        GetAllCurrentSubscribersQuery getAllCurrentSubscribersQuery,
        EntitiesChangeMessagingService<ChannelMember> messagesSendingService,
        TelegramBotClient telegramBotClient,
        FetchUsersBackgroundService fetchUsersBackgroundService,
        MessageBuilder messageBuilder,
        SubscribersChangeProcessor subscribersChangeProcessor,
        MonitoringPresentation monitoringPresentation,
        List<long> chatIdCollection,
        int checkPeriodSeconds,
        string channelReference,
        IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor,
        IMonitoringService<TEntity, TOnJoinedEvent, TOnLeftEvent> monitoringService)
    {
        this.getAllCurrentSubscribersQuery = getAllCurrentSubscribersQuery;
        this.messagesSendingService = messagesSendingService;
        this.telegramBotClient = telegramBotClient;
        this.fetchUsersBackgroundService = fetchUsersBackgroundService;
        this.messageBuilder = messageBuilder;
        this.subscribersChangeProcessor = subscribersChangeProcessor;
        this.monitoringPresentation = monitoringPresentation;
        this.chatIdCollection = chatIdCollection;
        this.checkPeriodSeconds = checkPeriodSeconds;
        this.channelReference = channelReference;
        this.eventsMonitoringProcessor = eventsMonitoringProcessor;
        this.monitoringService = monitoringService;
    }

    private readonly GetAllCurrentSubscribersQuery getAllCurrentSubscribersQuery;
    private readonly EntitiesChangeMessagingService<ChannelMember> messagesSendingService;
    private readonly TelegramBotClient telegramBotClient;
    private readonly FetchUsersBackgroundService fetchUsersBackgroundService;
    private readonly MessageBuilder messageBuilder;
    private readonly SubscribersChangeProcessor subscribersChangeProcessor;
    private readonly MonitoringPresentation monitoringPresentation;
    private readonly List<long> chatIdCollection;
    private readonly string channelReference;
    private readonly IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor;
    private readonly IMonitoringService<TEntity, TOnJoinedEvent, TOnLeftEvent> monitoringService;

    private int checkPeriodSeconds;
    private DateTime basicTimeStamp;
    private IEnumerable<Update>? updates;
    private DateTime beginWorkingFrom;
    
    private async Task ProcessMonitoringByPeriodAsync()
    {
        if ((DateTime.Now - basicTimeStamp).TotalSeconds > checkPeriodSeconds)
        {
            await monitoringService.ProcessMonitoringAsync();
            basicTimeStamp = DateTime.Now;
        }
    }

    private async Task CheckAndProcessInputsAsync()
    {
        if (updates is not null && updates.Any())
        {
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

                string? resultMessage = message switch
                {
                    "/info" => messageBuilder.Info(channelReference),
                    "/last" => await messageBuilder.Last(),
                    "/last_subscribed" => await messageBuilder.LastSubscribed(),
                    "/last_unsubscribed" => await messageBuilder.LastUnsubscribed(),
                    "/change_period" => "будет менять период мониторинга",
                    "/check" => await messageBuilder.CheckDiagnostics(
                                      checkPeriodSeconds, 
                                      chatIdCollection.Count,
                                      beginWorkingFrom,
                                      fetchUsersBackgroundService.GetState()),
                    _ => null
                };

                long chatId = update.Message.Chat.Id;
                if (!chatIdCollection.Contains(chatId))
                {
                    chatIdCollection.Add(chatId);
                    Log.Information($"В рассылку бота добавлен новый пользователь с Id {chatId}");
                }

                if (resultMessage is not null)
                {
                    await messagesSendingService.TrySendMessageAsync(chatId, resultMessage);
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
        basicTimeStamp = DateTime.Now;
        beginWorkingFrom = DateTime.Now;

        updates = await telegramBotClient.GetUpdatesAsync();
        await fetchUsersBackgroundService.LoginAsync();
        bool apiServiceInitializationSuccess = await fetchUsersBackgroundService.InitializeServiceAsync();
        if(!apiServiceInitializationSuccess)
        {
            var message = $"Не удалось корректно инициализировать сервис сбора подписчиков '${nameof(TelegramChannelService)}'.";
            Log.Fatal(message);
            await messagesSendingService.TrySendMessageForAllAsync(chatIdCollection, message);
            return;
        }

        fetchUsersBackgroundService.Start();

        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor.OnSubscribersQuantityChanged;

        eventsMonitoringProcessor.EntitiesJoined += messagesSendingService.OnEntitiesJoined;
        eventsMonitoringProcessor.EntitiesLeft += messagesSendingService.OnEntitiesLeft;

        await messagesSendingService.TrySendMessageForAllAsync(chatIdCollection, "Я загрузился🚀! Наблюдаю...  👀🔎");
        Log.Information($"{GetType()} загрузился успешно.");

        await monitoringService.ProcessMonitoringAsync(); // TODO: получить существующих юзеров для мониторинга из базы, когда она таки-будет
    }

    public async Task MainLoopAsync()
    {
        while (true)
        {
            await ProcessMonitoringByPeriodAsync();
            await CheckAndProcessInputsAsync();
        }
    }
}
