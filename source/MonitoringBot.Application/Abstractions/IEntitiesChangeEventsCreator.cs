using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Application.Abstractions;

public interface IEntitiesChangeEventsCreator<TEntity>
{
    public IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> ProduceEvents(
        IEnumerable<TEntity> usersIdsFromApi,
        IEnumerable<TEntity> usersIdsFromRepository,
        string aggregateName);
}
