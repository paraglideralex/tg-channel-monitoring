using MonitoringBot.Infrastructure;

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
        JoinedAt = user.JoinedAt,
        TimeStamp = user.TimeStamp
    };

    public static ChannelMember ToDomain(this ChannelMemberEntity entity) => new(
        entity.Id,
        entity.NickName,
        entity.IsBot,
        entity.FirstName ?? string.Empty,
        entity.LastName ?? string.Empty,
        entity.Phone ?? string.Empty,
        entity.TimeStamp ?? DateTime.MinValue,
        entity.JoinedAt ?? DateTime.MinValue
    );
}
