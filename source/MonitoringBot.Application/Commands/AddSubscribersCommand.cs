using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;

using Serilog;

namespace MonitoringBot.Application.Commands;

public sealed class AddSubscribersCommand(UserRepository userRepository) 
    : BaseCommand<IEnumerable<ChannelMember>>
{
    protected override bool Validate(IEnumerable<ChannelMember> channelMembers)
    {
        var basic = base.Validate(channelMembers);
        bool eachNotNull = channelMembers.AllNotNull();
        bool notEmpty = channelMembers.Any();
        return basic && eachNotNull && notEmpty;
    }
    protected override async Task ExecuteCoreAsync(IEnumerable<ChannelMember> arguments)
    {
        await userRepository.AddRange(arguments);
        Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", arguments.Select(u => u.NickName))}");
    }
}
