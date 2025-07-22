namespace MonitoringBot;

public sealed class CacheKeysFactory
{
    public string BotUsersKey() => "CachedBotUsers";
    public string BotUsersCount() => "BotUsersCount";
}
