using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Abstractions;

public interface IEntitiesChangeDetector<TEntity>
{
    event Func<object?, EntitiesChangedEventArgs<TEntity>, Task>? EntitiesChanged;
    public Task ExecuteMonitoring(
        IEnumerable<TEntity> usersCollectionFromApi,
        IEnumerable<TEntity> usersCollectionFromRepository);
}
