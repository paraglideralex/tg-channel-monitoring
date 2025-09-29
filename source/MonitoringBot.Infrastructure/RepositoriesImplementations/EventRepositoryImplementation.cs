using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Projections;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

using System.Threading;
using System.Transactions;

namespace MonitoringBot.Infrastructure.RepositoriesImplementations;

public class EventsRepositoryImplementation<TEntity>(IDbContextFactory<MonitoringBotDbContextBase> factory)
    : EventRepository<TEntity>
{
    public override async Task AddRangeAsync(IEnumerable<EntitiesChangedDomainEventBase<TEntity>> events, CancellationToken cancellationToken)
    {
        var entities = events
            .Select(EntityChangedEventsMapping.ToEntity<EntitiesChangedDomainEventBase<TEntity>, TEntity>).ToList();

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        await dbContext.Events.AddRangeAsync(entities, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public override async Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> GetEventsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
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
        CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
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
            query = query.Where(q => q.TimeStamp <= filter.Toinclusive);

        var events = await query.ToListAsync(cancellationToken);
        return events.Select(EntityChangedEventsMapping.MapToDomainEvent<TEntity>).ToList();
    }

    public override async Task<EntitiesChangedDomainEventBase<TEntity>?> GetLatestEventByTypeAndIdentity(
        string eventType,
        long userIdentityProjection,
        CancellationToken cancellation)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellation);
        var target = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.EntityIdProjection == userIdentityProjection.ToString() &&
                        e.EventType == eventType)
            .OrderByDescending(e => e.TimeStamp)
            .FirstOrDefaultAsync(cancellation);

        return target is null
            ? null
            : EntityChangedEventsMapping.MapToDomainEvent<TEntity>(target);
    }

    public override async Task<List<EventTypeWithDate>> GetEventTypesInPeriodAsync(
        string aggregateName,
        DateTime fromNonInclusive,
        DateTime toInclusive,
        CancellationToken cancellation)
    {
        await using var dbContext = await factory.CreateDbContextAsync();

        var target = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.AggregateNameProjection == aggregateName &&
                        e.TimeStamp > fromNonInclusive &&
                        e.TimeStamp <= toInclusive)
            .OrderByDescending(e => e.TimeStamp)
            .Select(e => new EventTypeWithDate(e.TimeStamp, e.EventType))
            .ToListAsync();

        return target;
    }

    public override async Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> GetEventsFromLastSnapshotAsync(
        AggregateSnapshot aggregateSnapshot,
        DateTime dateTimeTo,
        CancellationToken cancellation)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var result = await dbContext.Events
            .Where(e =>
                e.AggregateNameProjection == aggregateSnapshot.AggregateName &&
                    (e.TimeStamp > aggregateSnapshot.LastEventTimeStamp ||
                     e.TimeStamp == aggregateSnapshot.LastEventTimeStamp 
                        && e.CurrentTimeSequenceNumber > aggregateSnapshot.LastEventSequenceNumberForTimeStamp) &&
                e.CurrentTimeSequenceNumber > aggregateSnapshot.LastEventSequenceNumberForTimeStamp &&
                e.TimeStamp <= dateTimeTo &&
                (e.EventType == nameof(SubscriberJoinedEvent) || e.EventType == nameof(SubscriberLeftEvent)))
            .OrderBy(e => e.TimeStamp)
            .ThenBy(e => e.CurrentTimeSequenceNumber)
            .Select(e => EntityChangedEventsMapping.MapToDomainEvent<TEntity>(e))
            .ToListAsync();

        return result;
    }
}
