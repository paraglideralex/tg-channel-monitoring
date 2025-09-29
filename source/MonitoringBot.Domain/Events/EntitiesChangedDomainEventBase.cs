namespace MonitoringBot.Domain.Events;

public class EntitiesChangedDomainEventBase<TEntity> : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int CurrentTimeSequenceNumber { get; init; }
    public DateTime TimeStamp { get; init; }
    public TEntity? Entity { get; init; }
    public string EntityIdProjection { get; init; }
    public string EntityNameProjection { get; init; }
    public string ChannelName { get; init; }
    public override string ToString() => $"{GetType().Name}_{TimeStamp}_{CurrentTimeSequenceNumber}_{EntityIdProjection}_{EntityNameProjection}_{ChannelName}";
}
