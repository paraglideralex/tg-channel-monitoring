using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries;

public sealed class GetAllCurrentSubscribersQuery(UserRepository userRepository)
{
    public async Task<List<ChannelMember>> ExecuteAsync()
    {
        return await userRepository.All(); // TODO: переписать под сравнение только лишь айдишников
    }
}
