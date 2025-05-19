namespace MonitoringBot.Infrastructure.Persistence;

public class EventEntity
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Data { get; set; } = null!;
    public DateTime TimeStamp { get; set; }
}
