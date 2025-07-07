using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Services;

public sealed class EntitiesQuantityCounter<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs> 
    : EntitiesQuantityCounterBase<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>
    where TEntity : ISearchableEntity
    where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
    where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
{
    public override long EntitiesIncrementByEvents(IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> eventsCollection)
    {
        long counter = 0;
        foreach (var entity in eventsCollection)
            counter += EventIncrementConverter(entity.GetType().Name);

        return counter;
    }

    protected override int EventIncrementConverter(string eventType)
    {
        if (eventType == typeof(TOnJoinedEventArgs).Name)
            return 1;
        if (eventType == typeof(TOnLeftEventArgs).Name)
            return -1;

        return 0;
    }
}
