using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Abstractions;

public abstract class EntitiesQuantityCounterBase<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>
    where TEntity : ISearchableEntity
    where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
    where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
{
    public abstract long EntitiesIncrementByEvents(IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> eventsCollection);

    protected abstract int EventIncrementConverter(string eventType);
}
