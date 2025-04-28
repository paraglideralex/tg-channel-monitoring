using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Presentation;

using Serilog;

using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;

namespace MonitoringBot.Services.MessagesSending;

public class MessagesSendingService(
    TelegramBotClient telegramBotClient,
    MonitoringPresentation monitoringPresentation,
    List<long> botUsers) // TODO: в будущем получать юзеров бота из репозитория, так нельзя
{
    private const int telegramMessageLengthLimit = 3950; // 4096, но тут с запасом
    public async Task TrySendMessageForAllAsync(List<long> chatIdsCollection, string? message)
    {
        var tasks = new List<Task>();
        foreach (var id in chatIdsCollection)
            tasks.Add(TrySendMessageAsync(id, message));

        await Task.WhenAll(tasks);
    }

    public async Task TrySendMessageAsync(long id, string? message)
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

    public async Task OnSubscribersChanged(object? sender, SubscribersChangedEventArgs e)
    {
        var resultingMessage = monitoringPresentation.FormatLeftOrJoinedUsers(
            e.DifferenceCount,
            e.MemberDifference);

        if (e.DifferenceCount != 0)
        {
            await TrySendMessageForAllAsync(botUsers, resultingMessage);
            Log.Information($"Обработано изменение количества участников на {e.DifferenceCount}");
        }
        else
        {
            Log.Information($"Ничего не происходить {DateTime.Now}");
        }
    }
}
