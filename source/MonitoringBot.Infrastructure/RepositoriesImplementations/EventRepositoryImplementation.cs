using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using TL;

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
                e.AggregateType == typeof(TEntity).Name &&
                e.TimeStamp >= from &&
                e.TimeStamp <= to)
            .OrderBy(e => e.TimeStamp)
            .ToListAsync(cancellationToken);

        return rows.Select(EntityChangedEventsMapping.MapToDomainEvent<TEntity>).ToList();
    }
}
