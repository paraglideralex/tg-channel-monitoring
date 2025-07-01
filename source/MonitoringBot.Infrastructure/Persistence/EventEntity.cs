namespace MonitoringBot.Infrastructure.Persistence;

public class EventEntity
{
    public Guid Id { get; set; }
    public long SequenceNumber { get; }
    public string AggregateNameProjection { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Data { get; set; } = null!;
    public DateTime TimeStamp { get; set; }
    public string EntityIdProjection { get; set; } = null!;
    public string EntityNameProjection { get; set;} = null!;

    public override string ToString() => $"{GetType().Name}_{TimeStamp}_{EventType}_" +
        $"{EntityIdProjection}_{EntityNameProjection}_{AggregateNameProjection}";
}
