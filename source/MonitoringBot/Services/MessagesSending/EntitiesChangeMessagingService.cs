using MonitoringBot.Application.Events;

using Serilog;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public abstract class EntitiesChangeMessagingService<TEntity> : MessagesSendingService
{
    public EntitiesChangeMessagingService(TelegramBotClient telegramBotClient, List<long> botUsers) : base(telegramBotClient, botUsers)
    {
    }

    protected abstract string CreateLeftMessage(EntitiesCollectionChangedEventArgs<TEntity> e);
    protected abstract string CreateJoinedMessage(EntitiesCollectionChangedEventArgs<TEntity> e);

    public async Task OnEntitiesLeft(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        var message = CreateLeftMessage(e);
        await OnEntitiesChanged(sender, e, message);
    }

    public async Task OnEntitiesJoined(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        var message = CreateJoinedMessage(e);
        await OnEntitiesChanged(sender, e, message);
    }

    protected async Task OnEntitiesChanged(object? sender,
        EntitiesCollectionChangedEventArgs<TEntity> e,
        string resultingMessage)
    {
        if (e.DifferenceCount != 0)
        {
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
