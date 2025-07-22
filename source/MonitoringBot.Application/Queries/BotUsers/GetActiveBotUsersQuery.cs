using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries.BotUsers;

public sealed class GetActiveBotUsersQuery(BotUserRepository botUserRepository)
{
    public async Task<IReadOnlyCollection<BotUser>> ExecuteAsync(ServiceContext serviceContext) => 
        await botUserRepository.GetCurrentUsers(serviceContext.CancellationToken);
}
