using Microsoft.Extensions.Caching.Memory;

using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.BotUsers;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Caching;

using Serilog;

namespace MonitoringBot.Application.Services;

public sealed class BotUsersService(
    AddBotUserCommand addBotUserCommand,
    IMemoryCache memoryCache,
    CacheKeysFactory cacheKeysFactory,
    GetActiveBotUsersQuery getActiveBotUsersQuery) : IBotUsersService
{
    public async Task<IReadOnlyCollection<BotUser>> GetActiveBotUsersAsync(ServiceContext serviceContext) =>
        await getActiveBotUsersQuery.ExecuteAsync(serviceContext);

    public async Task AddBotUserIfNew(BotUser? newUser, ServiceContext serviceContext)
    {
        var currentUsers = GetCachedUsers();

        if (newUser is not null && !currentUsers.Any(u => u.Id == newUser.Id))
        {
            currentUsers.Add(newUser);
            memoryCache.Set(cacheKeysFactory.BotUsersKey(), currentUsers);
            memoryCache.Set(cacheKeysFactory.BotUsersCount(), currentUsers.Count);

            await addBotUserCommand.ExecuteAsync(
                newUser,
                serviceContext);

            Log.Information($"В рассылку бота добавлен новый пользователь с Id {newUser.Id}");
        }
    }

    public List<long> GetCachedBotUsersIds() => GetCachedUsers().Select(u => u.Id).ToList();

    public int BotUsersCount()
    {
        var count = memoryCache.TryGetValue(cacheKeysFactory.BotUsersCount(), out int? countObject);
        return countObject ?? 0;
    }

    public async Task SynchronizeAsync(ServiceContext serviceContext)
    {
        var dataBaseUsers = await getActiveBotUsersQuery.ExecuteAsync(serviceContext);
        memoryCache.Set(cacheKeysFactory.BotUsersKey(), dataBaseUsers.ToHashSet());
        memoryCache.Set(cacheKeysFactory.BotUsersCount(), dataBaseUsers.Count);
    }

    private HashSet<BotUser> GetCachedUsers()
    {
        _ = memoryCache.TryGetValue(cacheKeysFactory.BotUsersKey(), out object? cachedUsersObject);

        return cachedUsersObject is HashSet<BotUser> users ? users : [];
    }
}
