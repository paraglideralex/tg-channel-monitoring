using MonitoringBot.Application;
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
        int checkPeriodSeconds)
    {
        this.telegramBotClient = telegramBotClient;
        this.telegramService = telegramService;
        this.messageBuilder = messageBuilder;
        this.monitoringEngine = monitoringEngine;
        this.monitoringPresentation = monitoringPresentation;
        this.chatIdCollection = chatIdCollection;
        this.checkPeriodSeconds = checkPeriodSeconds;
    }

    private readonly TelegramBotClient telegramBotClient;
    private readonly TelegramChannelService telegramService;
    private readonly MessageBuilder messageBuilder;
    private readonly MonitoringEngine monitoringEngine;
    private readonly MonitoringPresentation monitoringPresentation;
    private readonly List<long> chatIdCollection;
    private int checkPeriodSeconds = 10;

    private DateTime basicDate;
    private DateTime basic;
    private IEnumerable<Update> updates;
    
    private async Task ProcessMonitoringAsync()
    {
        if ((DateTime.Now - basicDate).TotalSeconds > checkPeriodSeconds)
        {

            var users = await telegramService.GetChannelMembersAsync();

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
                await TrySendMessageAsync(335442317, $"Ничего не происходить {DateTime.Now}");
                Log.Information($"Ничего не происходить {DateTime.Now}");
            }

            basicDate = DateTime.Now;
        }
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
            await telegramBotClient.SendMessageAsync(id, message ?? "Пустое сообщение", parseMode: FormatStyles.HTML);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Log.Error($"Сообщение не было отправлено id: {id}, message:{message}");
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
                    "/info" => "Данный бот предоставляет информацию о хороших новых подписчиках ❤️ и плохих отписавшихся 💩",
                    "/last" => await messageBuilder.Last(),
                    "/check" => "тут будет мгновенная стата",
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
                    Log.Information($"Пользователю {chatId} отправлен ответ '{resultMessage}'.");
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
        basicDate = DateTime.Now;
        basic = DateTime.Now;

        updates = await telegramBotClient.GetUpdatesAsync();
        await telegramService.LoginAsync();

        await TrySendMessageForAllAsync(chatIdCollection, "Я загрузился🚀! Наблюдаю...  👀🔎");
        Log.Information($"{GetType()} загрузился успешно.");
    }

    public async Task MainLoopAsync()
    {
        while (true)
        {
            await ProcessMonitoringAsync();
            await CheckAndProcessInputsAsync();
        }
    }

    public void Dispose()
    {
        telegramService?.Dispose();
    }
}
