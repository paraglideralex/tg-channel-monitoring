namespace MonitoringBot.Infrastructure.Persistence.Entities.Snapshots;

public sealed record AggregateSnapshotEntity
{
    public required Guid Id { get; set; }
    public required string AggregateName { get; set; }
    public long? TotalEntities { get; set; }
    public Guid? LastProcessedEventId { get; set; }
    public DateTime? LastEventTimeStamp { get; set; }
    public int? LastEventSequenceNumberForTimeStamp { get; set; }
    public required DateTime? TimeStamp { get; set; }

    public override string ToString() => $"{AggregateName}_{TimeStamp}_{LastEventTimeStamp}_" +
        $"{LastEventSequenceNumberForTimeStamp}_{TotalEntities}";
}
