using MonitoringBot.Domain.Events;

namespace MonitoringBot.Application.Abstractions;

public interface IEntitiesChangeEventsCreator<TIdentity, TEntity>
{
    public IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> ProduceEvents(
        IEnumerable<TIdentity> usersIdsFromApi,
        IEnumerable<TIdentity> usersIdsFromRepository,
        string aggregateName);
}
