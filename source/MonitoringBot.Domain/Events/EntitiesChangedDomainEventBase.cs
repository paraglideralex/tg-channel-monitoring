namespace MonitoringBot.Domain.Events;

public class EntitiesChangedDomainEventBase<T> : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime TimeStamp { get; init; } = DateTime.Now;
    public T? Entity { get; init; }
    public string EntityIdProjection { get; init; }
    public string EntityNameProjection { get; init; }
    public string ChannelName { get; init; }
}
