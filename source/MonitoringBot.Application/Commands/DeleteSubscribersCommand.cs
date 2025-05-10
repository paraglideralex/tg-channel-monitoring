using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Commands;

internal class DeleteSubscribersCommand(UserRepository userRepository)
    : BaseCommand<IEnumerable<ChannelMember>>
{
    protected override bool Validate(IEnumerable<ChannelMember> channelMembers)
    {
        var basic = base.Validate(channelMembers);
        bool eachNotNull = channelMembers.Any(x => x != null);
        return basic && eachNotNull;
    }

    protected override async Task ExecuteCoreAsync(IEnumerable<ChannelMember> arguments) =>
        await userRepository.DeleteRange(arguments);
}
