using MonitoringBot.Application.Abstractions;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Application.Services;

public abstract class EntitiesChangeEventsCreator<TIdentity, TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>(
    IEntitiesChangeDetector<TIdentity> entitiesChangeDetector,
    ITimeProvider timeProvider)
    : IEntitiesChangeEventsCreator<TIdentity, TEntity>
    where TEntity : ISearchableEntity
    where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
    where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
{
    public IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> ProduceEvents(
        IEnumerable<TIdentity> usersIdsFromApi,
        IEnumerable<TIdentity> usersIdsFromRepository,
        string aggregateName)
    {
        var detectionResult = entitiesChangeDetector.FindChanges(usersIdsFromApi, usersIdsFromRepository, aggregateName);

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

    protected abstract Task<IEnumerable<TEntity>> UploadJoinedEntitiesAsync();
    protected abstract Task<IEnumerable<TEntity>> UploadLeftEntitiesAsync();
}
