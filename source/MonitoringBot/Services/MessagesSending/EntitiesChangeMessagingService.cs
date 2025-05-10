using MonitoringBot.Domain.Events;

using Serilog;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public abstract class EntitiesChangeMessagingService<TEntity> : MessagesSendingService
{
    public EntitiesChangeMessagingService(TelegramBotClient telegramBotClient, List<long> botUsers) : base(telegramBotClient, botUsers)
    {
    }

    protected internal abstract string CreateMessage(EntitiesCollectionChangedEventArgs<TEntity> e);

    public async Task OnSubscribersChanged(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        if (e.DifferenceCount != 0)
        {
            var resultingMessage = CreateMessage(e);
            await TrySendMessageForAllAsync(botUsers, resultingMessage);
            Log.Information($"Обработано изменение количества участников на {e.DifferenceCount}");
        }
        else
        {
            Log.Information($"Количество пользователей не изменилось, " +
                $"{nameof(EntitiesChangeMessagingService<TEntity>)} ничего не делает.");
        }
    }
}
