using MonitoringBot.Application.Events;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public sealed class EntitiesChangeMessagingService<TEntity> : MessagesSendingService
{
    public EntitiesChangeMessagingService(TelegramBotClient telegramBotClient, List<long> botUsers) : base(telegramBotClient, botUsers)
    {
    }

    public async Task OnMessageProduced(object? sender, MessageCreatedEventArgs args)
    {
        await TrySendMessageForAllAsync(botUsers, args.Message);
    }
}
