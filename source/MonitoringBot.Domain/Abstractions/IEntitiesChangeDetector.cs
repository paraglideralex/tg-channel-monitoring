using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Abstractions;

public interface IEntitiesChangeDetector<TEntity>
{
    event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesJoined;
    event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesLeft;
    public Task ExecuteMonitoringAsync(
        IEnumerable<TEntity> usersCollectionFromApi,
        IEnumerable<TEntity> usersCollectionFromRepository);
}
