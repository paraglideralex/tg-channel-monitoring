using MonitoringBot.Domain.Abstractions;

namespace MonitoringBot.Domain.Entities;

public sealed class ChannelMember : SearchableEntity
{
    public long Id { get; init; }
    public string? NickName { get; init; }
    public bool IsBot { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public DateTime? TimeStamp { get; init; }
    public DateTime? JoinedAt { get; init; }
    public ChannelMember() { }

    public ChannelMember(long id, string? nickName, bool isBot, string? firstName, string? lastName, string? phone, DateTime? timeStamp, DateTime? joinedAt)
    {
        Id = id;
        NickName = nickName;
        IsBot = isBot;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        TimeStamp = timeStamp;
        JoinedAt = joinedAt;
    }

    public override bool Equals(object? obj) => obj is ChannelMember other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();

    public override string IdProjection() => Id.ToString();
    public override string NameProjection() => NickName?.ToString() ?? "undefined";
}