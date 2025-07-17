using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Projections;
using MonitoringBot.Domain.RepositoriesAbstarctions;

namespace MonitoringBot.Application.Services;

public sealed class AggregateSnapshotCreator<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>(
    ITimeProvider timeProvider,
    EventRepository<TEntity> eventRepository,
    SnapshotRepository snapshotRepository,
    EntitiesQuantityCounterBase<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs> entitiesQuantityCounter)
        where TEntity : ISearchableEntity
        where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()

{
    public async Task<AggregateSnapshot> ExecuteAsync(string aggregateName)
    {
        var timeStamp = timeProvider.UtcNow;
        var lastSnapshotByTime = await snapshotRepository.GetClosestPreviousAsync(aggregateName, timeStamp);

        //IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> lastEvents = lastSnapshotByTime is null
        //    ? await eventRepository.GetEventsByFilterAsync(new EventsQueryFilter
        //    {
        //        EntityAggregateNameProjection = aggregateName,
        //        Toinclusive = timeStamp
        //    })
        //    : await eventRepository.GetEventsFromLastSnapshotAsync(lastSnapshotByTime, timeStamp);

        var lastEvents = await eventRepository.GetEventsByFilterAsync(new EventsQueryFilter
        {
            EntityAggregateNameProjection = aggregateName,
            Toinclusive = timeStamp
        });

        if (lastEvents.Count == 0)
            return LastOrDefault(lastSnapshotByTime, aggregateName);

        var newCount = entitiesQuantityCounter.EntitiesIncrementByEvents(lastEvents);

        //var lastSnapshotCount = lastSnapshotByTime?.TotalEntities ?? 0;

        var newTotalCount = newCount;

        if (newTotalCount is 0)
            return LastOrDefault(lastSnapshotByTime, aggregateName);

        var lastEvent = lastEvents.ToArray()[^1];

        return new()
        {
            Id = Guid.NewGuid(),
            AggregateName = aggregateName,
            LastEventSequenceNumberForTimeStamp = lastEvent.CurrentTimeSequenceNumber,
            LastEventTimeStamp = lastEvent.TimeStamp,
            LastProcessedEventId = lastEvent.Id,
            TotalEntities = newTotalCount,
            TimeStamp = timeStamp
        };
    }

    // TODO: тут ограничение по уникальному ключу - доработать чтобы менялся гуид
    private AggregateSnapshot LastOrDefault(AggregateSnapshot? last, string aggregateName) =>
        last is null
            ? new()
            {
                Id = Guid.NewGuid(),
                AggregateName = aggregateName,
                TotalEntities = 0,
                TimeStamp = timeProvider.UtcNow
            }
            : last;

}
