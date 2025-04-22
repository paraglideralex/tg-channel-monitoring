using MonitoringBot.Application;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Presentation;

using Serilog;

using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.GettingUpdates;

public class MonitoringBotRunner : IDisposable
{
    public MonitoringBotRunner(
        TelegramBotClient telegramBotClient,
        TelegramChannelService telegramService,
        MessageBuilder messageBuilder,
        MonitoringEngine monitoringEngine,
        MonitoringPresentation monitoringPresentation,
        List<long> chatIdCollection,
        int checkPeriodSeconds,
        string channelReference)
    {
        this.telegramBotClient = telegramBotClient;
        this.telegramService = telegramService;
        this.messageBuilder = messageBuilder;
        this.monitoringEngine = monitoringEngine;
        this.monitoringPresentation = monitoringPresentation;
        this.chatIdCollection = chatIdCollection;
        this.checkPeriodSeconds = checkPeriodSeconds;
        this.channelReference = channelReference;
    }
    private const int telegramMessageLengthLimit = 3950; // 4096, но тут с запасом

    private readonly TelegramBotClient telegramBotClient;
    private readonly TelegramChannelService telegramService;
    private readonly MessageBuilder messageBuilder;
    private readonly MonitoringEngine monitoringEngine;
    private readonly MonitoringPresentation monitoringPresentation;
    private readonly List<long> chatIdCollection;
    private readonly string channelReference;

    private int checkPeriodSeconds;
    private DateTime basicTimeStamp;
    private IEnumerable<Update> updates;
    private DateTime beginWorkingFrom;
    
    private async Task ProcessMonitoringByPeriodAsync()
    {
        if ((DateTime.Now - basicTimeStamp).TotalSeconds > checkPeriodSeconds)
            await ProcessMonitoringAsync();
    }

    private async Task ProcessMonitoringAsync()
    {
        // Будет добавлен фоновый сервис мониторинга, получать через него из его поля UsersSnapshot
        var users = await telegramService.GetChannelMembersAsync(); // TODO: просто получать из хранилища или из поля сервиса
        if(users is null)
        {
            Log.Warning("Импорт подписчиков канала не выполнен, подписчики в этот раз не получены из телеграм-канала.");
            return;
        }

        await monitoringEngine.MonitoringStep(users);

        var resultingMessage = monitoringPresentation.FormatLeftOrJoinedUsers(
            monitoringEngine.CurrentStepDifferenceCount,
            monitoringEngine.CurrentStepMemberDifference);

        if (monitoringEngine.CurrentStepDifferenceCount != 0)
        {
            await TrySendMessageForAllAsync(chatIdCollection, resultingMessage);
            Log.Information($"Обработано изменение количества участников на {monitoringEngine.CurrentStepDifferenceCount}");
        }
        else
        {
            Log.Information($"Ничего не происходить {DateTime.Now}");
        }
        basicTimeStamp = DateTime.Now;
    }

    private async Task TrySendMessageForAllAsync(List<long> chatIdsCollection, string? message)
    {
        var tasks = new List<Task>();
        foreach (var id in chatIdCollection)
            tasks.Add(TrySendMessageAsync(id, message));

        await Task.WhenAll(tasks);
    }

    private async Task TrySendMessageAsync(long id, string? message)
    {
        try
        {
            await telegramBotClient.SendMessageAsync(
                id, 
                message.TakeAndFormatFirst(telegramMessageLengthLimit) ?? "Пустое сообщение",
                parseMode: FormatStyles.HTML);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Log.Error($"Сообщение в ответ на запрос не было отправлено id: {id}, message:{message.TakeAndFormatFirst(300)}");
        }
    }

    private async Task CheckAndProcessInputsAsync()
    {
        if (updates.Any())
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
                    "/change_period" => "будет менять период мониторинга",
                    "/check" => await messageBuilder.CheckDiagnostics(
                                      checkPeriodSeconds, 
                                      telegramService.LastsearchParicipantsDurationSeconds, 
                                      chatIdCollection.Count,
                                      beginWorkingFrom),
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
                    await TrySendMessageAsync(chatId, resultMessage);
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
        await telegramService.LoginAsync();

        await TrySendMessageForAllAsync(chatIdCollection, "Я загрузился🚀! Наблюдаю...  👀🔎");
        Log.Information($"{GetType()} загрузился успешно.");

        await ProcessMonitoringAsync(); // TODO: получить существующих юзеров для мониторинга из базы, когда она таки-будет
    }

    public async Task MainLoopAsync()
    {
        while (true)
        {
            await ProcessMonitoringByPeriodAsync();
            await CheckAndProcessInputsAsync();
        }
    }

    public void Dispose()
    {
        telegramService?.Dispose();
    }
}
