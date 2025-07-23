using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Events;
using MonitoringBot.Infrastructure;

using Serilog;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public sealed class EntitiesChangeMessagingService<TEntity>(
    TelegramBotClient telegramBotClient,
    IBotUsersService botUsersService) : MessagesSendingService(telegramBotClient)
{
    public async Task OnMessageProduced(object? sender, MessageCreatedEventArgs args, ServiceContext serviceContext)
    {
        var keys = botUsersService.GetCachedBotUsersIds();

        if (keys is not null)
            await TrySendMessageForAllAsync(keys, args.Message, serviceContext);
        else
            Log.Warning("Bot users were not received from cache.");
    }
}
