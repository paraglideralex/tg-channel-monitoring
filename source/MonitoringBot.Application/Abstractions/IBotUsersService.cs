using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Abstractions;

public interface IBotUsersService
{
    Task AddBotUserIfNew(BotUser? newUser, ServiceContext serviceContext);
    Task<IReadOnlyCollection<BotUser>> GetActiveBotUsersAsync(ServiceContext serviceContext);
    int BotUsersCount();
    Task SynchronizeAsync(ServiceContext serviceContext);
    List<long> GetCachedBotUsersIds();
}