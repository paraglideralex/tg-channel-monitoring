using MonitoringBot.Domain.Events;

using System.Collections.Generic;

namespace MonitoringBot.Domain.Abstractions;

public interface IEntitiesChangeDetector<TEntity>
{
    //event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesJoined;
    //event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesLeft;
    public IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> ExecuteMonitoring(
        IEnumerable<TEntity> usersCollectionFromApi,
        IEnumerable<TEntity> usersCollectionFromRepository);
}
