namespace MonitoringBot.Domain.Entities;

public class EventsQueryFilter
{
    public Guid? Id { get; init; }
    public string? EventType {  get; init; }
    public string? EntityType { get; init; }
    public string? EntityIdProjection { get; init; }
    public string? EntityNameProjection { get; init; }
    public string? EntityAggregateNameProjection {  get; init; }
    public DateTime? FromNonInclusive { get; init; }
    public DateTime? Toinclusive { get; init; }
}
