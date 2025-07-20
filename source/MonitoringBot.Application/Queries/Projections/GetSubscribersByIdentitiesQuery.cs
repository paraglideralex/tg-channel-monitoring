using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries.Projections;

public sealed class GetSubscribersByIdentitiesQuery(UserRepository userRepository)
{
    public async Task<List<ChannelMember>> ExecuteAsync(IEnumerable<long> identities, ServiceContext context)
    {
        return await userRepository.FindByIdsAsync(identities, context.CancellationToken);
    }
}
