namespace MonitoringBot.Application.Queries.Snapshots.Arguments;

public sealed record ClosestSnapshotByTimeQuery
{
    public required string AggregateName { get; init; }
    public required DateTime TimeStamp { get; init; }
}
