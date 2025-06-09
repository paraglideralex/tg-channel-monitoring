using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;

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
}
