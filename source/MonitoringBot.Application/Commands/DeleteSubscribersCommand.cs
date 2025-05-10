using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;

namespace MonitoringBot.Application.Commands;

public sealed class DeleteSubscribersCommand(UserRepository userRepository)
    : BaseCommand<IEnumerable<ChannelMember>>
{
    protected override bool Validate(IEnumerable<ChannelMember> channelMembers)
    {
        var basic = base.Validate(channelMembers);
        bool eachNotNull = channelMembers.AllNotNull();
        return basic && eachNotNull;
    }

    protected override async Task ExecuteCoreAsync(IEnumerable<ChannelMember> arguments) =>
        await userRepository.DeleteRange(arguments);
}
