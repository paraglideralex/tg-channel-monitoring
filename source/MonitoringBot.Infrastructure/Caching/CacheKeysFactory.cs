namespace MonitoringBot.Infrastructure.Caching;

public sealed class CacheKeysFactory
{
    public string BotUsersKey() => "CachedBotUsers";
    public string BotUsersCount() => "BotUsersCount";
}
