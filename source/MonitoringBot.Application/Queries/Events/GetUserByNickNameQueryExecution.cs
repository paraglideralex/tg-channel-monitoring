using Microsoft.IdentityModel.Tokens;

using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

using Serilog;

namespace MonitoringBot.Application.Queries.Events;

public sealed class GetUserByNickNameQueryExecution(
    UserRepository userRepository)
{
    public async Task<ChannelMember?> ExecuteAsync(GetUserByNameQuery query, ServiceContext serviceContext)
    {
        if (query.UserNickName.IsNullOrEmpty())
        {
            Log.Error($"{nameof(query.UserNickName)} is null or empty in {nameof(GetUserByNickNameQueryExecution)}." +
                $"Returning null.");

            return null;
        }
            
        return await userRepository.FindByNickNameAsync(query.UserNickName!, serviceContext.CancellationToken);
    }
}
