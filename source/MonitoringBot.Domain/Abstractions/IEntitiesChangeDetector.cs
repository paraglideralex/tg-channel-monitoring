using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Abstractions;

public interface IEntitiesChangeDetector<TEntity>
{
    event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesChanged;
    public Task ExecuteMonitoring(
        IEnumerable<TEntity> usersCollectionFromApi,
        IEnumerable<TEntity> usersCollectionFromRepository);
}
