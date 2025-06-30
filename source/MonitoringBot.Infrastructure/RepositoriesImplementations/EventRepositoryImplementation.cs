using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Projections;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence;

namespace MonitoringBot.Infrastructure.RepositoriesImplementations;

public class EventsRepositoryImplementation<TEntity>(MonitoringBotDbContextBase dbContext)
    : EventRepository<TEntity>
{
    public override async Task AddRangeAsync(IEnumerable<EntitiesChangedDomainEventBase<TEntity>> events, CancellationToken cancellationToken = default)
    {
        var entities = events
            .Select(EntityChangedEventsMapping.ToEntity<EntitiesChangedDomainEventBase<TEntity>, TEntity>).ToList();

        await dbContext.Events.AddRangeAsync(entities, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public override async Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> GetEventsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Events
            .Where(e =>
                e.EntityType == typeof(TEntity).Name &&
                e.TimeStamp > from &&
                e.TimeStamp <= to)
            .OrderBy(e => e.TimeStamp)
            .ToListAsync(cancellationToken);

        return rows.Select(EntityChangedEventsMapping.MapToDomainEvent<TEntity>).ToList();
    }

    public override async Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> GetEventsByFilterAsync(
        EventsQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Events.AsQueryable();

        if(filter.Id is not null)
            query = query.Where(q => q.Id == filter.Id);
        if(filter.EventType is not null)
            query = query.Where(q => q.EventType == filter.EventType);
        if (filter.EntityType is not null)
            query = query.Where(q => q.EntityType == filter.EntityType);
        if (filter.EntityIdProjection is not null)
            query = query.Where(q => q.EntityIdProjection == filter.EntityIdProjection);
        if (filter.EntityNameProjection is not null)
            query = query.Where(q => q.EntityNameProjection == filter.EntityNameProjection);
        if (filter.EntityAggregateNameProjection is not null)
            query = query.Where(q => q.AggregateNameProjection == filter.EntityAggregateNameProjection);
        if (filter.FromNonInclusive is not null)
            query = query.Where(q => q.TimeStamp > filter.FromNonInclusive);
        if (filter.Toinclusive is not null)
            query = query.Where(q => q.TimeStamp > filter.Toinclusive);

        var events = await query.ToListAsync(cancellationToken);
        return events.Select(EntityChangedEventsMapping.MapToDomainEvent<TEntity>).ToList();
    }

    public override async Task<EntitiesChangedDomainEventBase<TEntity>?> GetLatestEventByTypeAndIdentity(
        string eventType,
        long userIdentityProjection,
        CancellationToken cancellation = default)
    {
        var target = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.EntityIdProjection == userIdentityProjection.ToString() &&
                        e.EventType == eventType)
            .OrderByDescending(e => e.TimeStamp)
            .FirstOrDefaultAsync();

        return target is null
            ? null
            : EntityChangedEventsMapping.MapToDomainEvent<TEntity>(target);
    }

    public override async Task<List<EventTypeWithDate>> GetEventTypesInPeriodAsync(
        DateTime fromNonInclusive,
        DateTime toInclusive,
        CancellationToken cancellation = default)
    {
        var target = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.TimeStamp > fromNonInclusive &&
                        e.TimeStamp <= toInclusive)
            .OrderByDescending(e => e.TimeStamp)
            .Select(e => new EventTypeWithDate(e.TimeStamp, e.EventType))
            .ToListAsync();

        return target;
    }

    public async Task<long> CountEntitiesIncreaseByPeriodAsync(DateTime dateTimeTo, DateTime? dateTimeFrom = null)
    {
        var dateFromSigned = dateTimeFrom ?? DateTime.MinValue;

        var result = await dbContext.Events
            .Where(e => e.TimeStamp < dateTimeTo &&
                        e.TimeStamp >= dateFromSigned &&
                        (e.EventType == nameof(SubscriberJoinedEvent) || e.EventType == nameof(SubscriberLeftEvent)))
            .GroupBy(e => 1)
            .Select(g => new
            {
                Joined = g.Count(e => e.EventType == nameof(SubscriberJoinedEvent)),
                Left = g.Count(e => e.EventType == nameof(SubscriberLeftEvent))
            })
            .FirstOrDefaultAsync();

        var activeSubscribers = (result?.Joined ?? 0) - (result?.Left ?? 0);

        return activeSubscribers;
    }
}
