namespace MonitoringBot.Domain.Events;

public class EntitiesChangedDomainEventBase<TEntity> : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime TimeStamp { get; init; } = DateTime.Now;
    public TEntity? Entity { get; init; }
    public string EntityIdProjection { get; init; }
    public string EntityNameProjection { get; init; }
    public string ChannelName { get; init; }
}
