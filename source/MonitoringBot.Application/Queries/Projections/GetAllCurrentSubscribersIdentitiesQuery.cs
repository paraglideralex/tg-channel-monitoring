using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries.Projections;

public sealed class GetAllCurrentSubscribersIdentitiesQuery(UserRepository userRepository)
{
    public async Task<List<long>> ExecuteAsync(ServiceContext context)
    {
        return await userRepository.AllSubscribedIdentitiesAsync(context.CancellationToken);
    }
}
