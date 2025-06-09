using MonitoringBot.Application.Events;

namespace MonitoringBot.Application.Abstractions;

public interface IEventsMonitoringProcessor<TEntity>
{
    event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesJoined;
    event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesLeft;
    Task ExecuteMonitoringAsync();
}
