using MonitoringBot.Application.Abstractions;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Application.Services;

public sealed class EntitiesChangeEventsCreator<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>(
    ITimeProvider timeProvider)
    : IEntitiesChangeEventsCreator<TEntity>
        where TEntity : ISearchableEntity
        where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
{
    public IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> ProduceEvents(
        IEnumerable<TEntity> joined,
        IEnumerable<TEntity> left,
        string aggregateName)
    {
        var events = new List<EntitiesChangedDomainEventBase<TEntity>>();
        int currentTimeSequenceNumber = 0;

        events.AddRange(joined.Select(j => new TOnJoinedEventArgs
        {
            Entity = j,
            EntityIdProjection = j.IdProjection(),
            EntityNameProjection = j.NameProjection(),
            ChannelName = aggregateName,
            TimeStamp = timeProvider.UtcNow,
            CurrentTimeSequenceNumber = currentTimeSequenceNumber++
        }));

        events.AddRange(left.Select(l => new TOnLeftEventArgs
        {
            Entity = l,
            EntityIdProjection = l.IdProjection(),
            EntityNameProjection = l.NameProjection(),
            ChannelName = aggregateName,
            TimeStamp = timeProvider.UtcNow,
            CurrentTimeSequenceNumber = currentTimeSequenceNumber++
        }));

        return events;
    }
}
