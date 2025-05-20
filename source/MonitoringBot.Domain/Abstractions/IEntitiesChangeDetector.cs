using MonitoringBot.Domain.Events;

using System.Collections.Generic;

namespace MonitoringBot.Domain.Abstractions;

public interface IEntitiesChangeDetector<TEntity>
{
    public IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> ProduceEvents(
        IEnumerable<TEntity> usersCollectionFromApi,
        IEnumerable<TEntity> usersCollectionFromRepository,
        string aggregateName);
}
