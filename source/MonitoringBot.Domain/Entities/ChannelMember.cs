using MonitoringBot.Domain.Abstractions;

namespace MonitoringBot.Domain.Entities;

public sealed class ChannelMember : ISearchableEntity
{
    public long Id { get; init; }
    public string? NickName { get; init; }
    public bool IsBot { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? ChannelReference { get; init; }
    public string? LastAction { get; init; }
    public DateTime? TimeStamp { get; init; }
    public DateTime? Created { get; init; }
    public ChannelMember() { }

    public ChannelMember(long id, string? nickName, bool isBot, string? firstName, 
        string? lastName, string? phone, DateTime? timeStamp, DateTime? joinedAt, 
        string channelReference, string? lastAction)
    {
        Id = id;
        NickName = nickName;
        IsBot = isBot;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        TimeStamp = timeStamp;
        Created = joinedAt;
        ChannelReference = channelReference;
        LastAction = lastAction;
    }

    public override string ToString() => $"{Id}_{NickName}_{FirstName}_{LastName}_{LastAction}_{TimeStamp}_{Created}";

    public override bool Equals(object? obj) => obj is ChannelMember other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();

    public string IdProjection() => Id.ToString();
    public string NameProjection() => NickName ?? "undefined";

    public string AggregateProjection() => ChannelReference ?? "undefined";
}
