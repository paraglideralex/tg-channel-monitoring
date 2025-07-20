using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;

namespace MonitoringBot.Services.MessagesSending;

public class MessagesSendingService // TODO: в будущем получать юзеров бота из репозитория, так нельзя
{
    public MessagesSendingService(
        TelegramBotClient telegramBotClient,
        TelegramBotSettings telegramApiSettings)
    {
        this.telegramBotClient = telegramBotClient;
        this.botUsers = telegramApiSettings.ChatIdsCollection;
    }

    protected TelegramBotClient telegramBotClient;
    protected List<long> botUsers;

    private const int telegramMessageLengthLimit = 3950; // 4096, но тут с запасом
    public async Task TrySendMessageForAllAsync(List<long> chatIdsCollection, string? message, ServiceContext serviceContext)
    {
        var tasks = new List<Task>();
        foreach (var id in chatIdsCollection)
            tasks.Add(TrySendMessageAsync(id, message, serviceContext.CancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task TrySendMessageAsync(long id, string? message, CancellationToken cancellationToken)
    {
        try
        {
            await telegramBotClient.SendMessageAsync(
                id,
                message.TakeAndFormatFirst(telegramMessageLengthLimit) ?? "Пустое сообщение",
                parseMode: FormatStyles.HTML,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Log.Error($"Сообщение в ответ на запрос не было отправлено id: {id}, message:{message.TakeAndFormatFirst(300)}");
        }
    }
}
