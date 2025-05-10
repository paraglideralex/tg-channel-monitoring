using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

using Serilog;

namespace MonitoringBot.Application.Commands;

public class AddSubscribersCommand(UserRepository userRepository) 
    : BaseCommand<IEnumerable<ChannelMember>>
{
    protected override bool Validate(IEnumerable<ChannelMember> channelMembers)
    {
        var basic = base.Validate(channelMembers);
        bool eachNotNull = channelMembers.Any(x => x != null); 
        return basic && eachNotNull;
    }
    protected override async Task ExecuteCoreAsync(IEnumerable<ChannelMember> arguments)
    {
        await userRepository.AddRange(arguments);
        Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", arguments.Select(u => u.NickName))}");
    }
}
