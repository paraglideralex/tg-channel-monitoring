using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Persistence;

public static class ChannelMemberMapper
{
    public static ChannelMemberEntity ToEntity(this ChannelMember user) => new()
    {
        Id = user.Id,
        NickName = user.NickName,
        IsBot = user.IsBot,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        Created = user.Created,
        TimeStamp = user.TimeStamp,
        ChannelReference = user.ChannelReference
    };

    public static ChannelMemberEntity ToEntity(this ChannelMember user, string? lastAction) => new()
    {
        Id = user.Id,
        NickName = user.NickName,
        IsBot = user.IsBot,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        Created = user.Created,
        TimeStamp = user.TimeStamp,
        ChannelReference = user.ChannelReference,
        LastAction = lastAction
    };

    public static ChannelMember ToDomain(this ChannelMemberEntity entity) => new(
        entity.Id,
        entity.NickName,
        entity.IsBot,
        entity.FirstName ?? string.Empty,
        entity.LastName ?? string.Empty,
        entity.Phone ?? string.Empty,
        entity.TimeStamp ?? DateTime.MinValue,
        entity.Created ?? DateTime.MinValue,
        entity.ChannelReference,
        entity.LastAction
    );
}
