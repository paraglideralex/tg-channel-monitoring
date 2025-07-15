using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure.Persistence.Entities;

namespace MonitoringBot.Infrastructure.Extensions;

public static class TLUserMapper
{
    public static ApiUserEntity ToEntity(this TLUser tLUser, bool isCurrent) =>
    new ()
    {
        ChannelReference = tLUser.ChannelReference,
        FirstName = tLUser.FirstName,
        LastName = tLUser.LastName,
        Id = tLUser.Id,
        IsBot = isCurrent,
        Joined = tLUser.JoinedTicks?.ToUtcDateTime(),
        TimeStamp = tLUser.TimeStampTicks?.ToUtcDateTime(),
        NickName = tLUser.NickName,
        Phone = tLUser.Phone,
        IsCurrent = isCurrent
    };

    public static TLUser ToTLUser(this ApiUserEntity tLUser) =>
    new()
    {
        ChannelReference = tLUser.ChannelReference,
        FirstName = tLUser.FirstName,
        LastName = tLUser.LastName,
        Id = tLUser.Id,
        IsBot = tLUser.IsBot,
        JoinedTicks = tLUser.Joined?.Ticks,
        TimeStampTicks = tLUser.TimeStamp?.Ticks,
        NickName = tLUser.NickName,
        Phone = tLUser.Phone
    };

    public static ChannelMember ToDomain(this TLUser user) =>
    new()
    {
        ChannelReference = user.ChannelReference,
        Created = user.JoinedTicks?.ToUtcDateTime(),
        FirstName = user.FirstName,
        Id = user.Id,
        IsBot = user.IsBot,
        LastAction = null,
        LastName = user.LastName,
        NickName = user.NickName,
        Phone = user.Phone,
        TimeStamp = user.TimeStampTicks?.ToUtcDateTime()
    };

}
