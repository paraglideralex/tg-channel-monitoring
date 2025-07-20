using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries.Events;

public class GetEventsInPeriodQueryExecution<TEntity>(
    EventRepository<TEntity> eventRepository)
{
    public async Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> ExecuteAsync(
        GetEventsInPeriodQuery query, ServiceContext serviceContext)
    {

        return await eventRepository.GetEventsByPeriodAsync(query.FromNonInclusive, query.ToInclusive, serviceContext.CancellationToken); 
    }
}
