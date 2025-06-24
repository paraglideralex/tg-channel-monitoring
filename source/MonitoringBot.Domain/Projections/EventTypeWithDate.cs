namespace MonitoringBot.Domain.Projections;

public sealed record EventTypeWithDate
{
    public DateTime TimeStamp { get; init; }
    public string? EventType { get; init; } = null;
}
