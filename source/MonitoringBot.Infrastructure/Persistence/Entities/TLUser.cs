using MonitoringBot.Infrastructure.Extensions;

namespace MonitoringBot.Infrastructure.Persistence.Entities;

public sealed record TLUser
{
    public long Id { get; init; }
    public string? NickName { get; init; }
    public bool IsBot { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? ChannelReference { get; init; }
    public long? JoinedTicks { get; init; }
    public long? TimeStampTicks { get; init; }

    public override string ToString() =>
        $"{ChannelReference}_{Id}_{NickName}_Joined:{TimeStampTicks?.ToUtcDateTime()}_{TimeStampTicks?.ToUtcDateTime()}";
}
