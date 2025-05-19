using MonitoringBot.Application.Queries.Arguments;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries;

public class GetEventsInPeriodQueryExecution<TEntity>(
    EventRepository<TEntity> eventRepository)
{
    public async Task<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>> ExecuteAsync(
        GetEventsInPeriodQuery query)
    {

        return await eventRepository.GetEventsByPeriodAsync(query.From, query.To); // TODO: переписать под сравнение только лишь айдишников
    }
}
