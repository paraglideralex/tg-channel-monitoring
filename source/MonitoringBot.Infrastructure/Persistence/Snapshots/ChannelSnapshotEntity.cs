namespace MonitoringBot.Infrastructure.Persistence.Snapshots;

public sealed class ChannelSnapshotEntity
{
    public required string ChannelName { get; set; }
    public DateTime SnapshotAt { get; set; }
    public long TotalSubscribers { get; set; }
    public Guid LastProcessedEventId { get; set; }
}
