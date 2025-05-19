namespace MonitoringBot.Domain.Events;

public class EntitiesChangedDomainEventBase<T> : IDomainEvent
{
    public DateTime TimeStamp { get; init; } = DateTime.Now;
    public T? Entity { get; init; }
}
