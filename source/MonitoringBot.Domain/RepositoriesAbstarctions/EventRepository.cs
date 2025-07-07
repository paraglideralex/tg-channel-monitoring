using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Projections;

namespace MonitoringBot.Domain.RepositoriesAbstarctions;

public abstract class EventRepository<TEntity>
{
    public abstract Task AddRangeAsync(
        IEnumerable<EntitiesChangedDomainEventBase<TEntity>> events, 
        CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> GetEventsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> GetEventsByFilterAsync(
        EventsQueryFilter filter,
        CancellationToken cancellationToken = default);

    public abstract Task<EntitiesChangedDomainEventBase<TEntity>?> GetLatestEventByTypeAndIdentity(
        string eventType,
        long userIdentityProjection,
        CancellationToken cancellation = default);

    public abstract Task<List<EventTypeWithDate>> GetEventTypesInPeriodAsync(
        DateTime fromNonInclusive,
        DateTime toInclusive,
        CancellationToken cancellation = default);

    public abstract Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> GetEventsFromLastSnapshotAsync(
        AggregateSnapshot aggregateSnapshot,
        DateTime dateTimeTo,
        CancellationToken cancellation = default);
}
