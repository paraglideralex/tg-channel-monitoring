using MonitoringBot.Domain.Events;

using Serilog;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public abstract class EntitiesChangeMessagingService<TEntity> : MessagesSendingService
{
    public EntitiesChangeMessagingService(TelegramBotClient telegramBotClient, List<long> botUsers) : base(telegramBotClient, botUsers)
    {
    }

    protected internal abstract string CreateMessage(EntitiesChangedEventArgs<TEntity> e);

    public async Task OnSubscribersChanged(object? sender, EntitiesChangedEventArgs<TEntity> e)
    {
        var resultingMessage = CreateMessage(e);

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
