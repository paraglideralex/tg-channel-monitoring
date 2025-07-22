using Microsoft.Extensions.Caching.Memory;

using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public sealed class EntitiesChangeMessagingService<TEntity> : MessagesSendingService
{
    private readonly IMemoryCache memoryCache;
    CacheKeysFactory cacheKeysFactory;
    public EntitiesChangeMessagingService(
        TelegramBotClient telegramBotClient,
        TelegramBotSettings telegramBotSettings,
        IMemoryCache memoryCache,
        CacheKeysFactory cacheKeysFactory)
        : base(telegramBotClient, telegramBotSettings)
    {
        this.memoryCache = memoryCache;
        this.cacheKeysFactory = cacheKeysFactory;
    }

    public async Task OnMessageProduced(object? sender, MessageCreatedEventArgs args, ServiceContext serviceContext)
    {
        _ = memoryCache.TryGetValue(cacheKeysFactory.BotUsersKey(), out HashSet<BotUser>? botUsersObject);
        var keys = botUsersObject?.Select(u => u.Id).ToList();

        if (keys is not null)
            await TrySendMessageForAllAsync(keys, args.Message, serviceContext);
        else
            Log.Warning("Bot users were not received from cache.");
    }
}
