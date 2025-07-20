using MonitoringBot.Application.Events;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Settings;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public sealed class EntitiesChangeMessagingService<TEntity> : MessagesSendingService
{
    public EntitiesChangeMessagingService(TelegramBotClient telegramBotClient, TelegramBotSettings telegramBotSettings)
        : base(telegramBotClient, telegramBotSettings)
    {
    }

    public async Task OnMessageProduced(object? sender, MessageCreatedEventArgs args, ServiceContext serviceContext)
    {
        await TrySendMessageForAllAsync(botUsers, args.Message, serviceContext);
    }
}
