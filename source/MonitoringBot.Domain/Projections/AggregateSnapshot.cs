namespace MonitoringBot.Domain.Projections;

public sealed record AggregateSnapshot
{
    public required Guid Id { get; init; }
    public required string AggregateName { get; init; }
    public long? TotalEntities { get; init; }
    public Guid? LastProcessedEventId { get; init; }
    public DateTime? LastEventTimeStamp { get; init; }
    public int? LastEventSequenceNumberForTimeStamp { get; init; }

    public required DateTime? TimeStamp { get; set; }

    public override string ToString() => $"{AggregateName}_{TimeStamp}_{LastEventTimeStamp}_" +
        $"{LastEventSequenceNumberForTimeStamp}_{TotalEntities}";
}
