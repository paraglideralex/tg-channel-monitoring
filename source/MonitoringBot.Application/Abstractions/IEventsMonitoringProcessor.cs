using MonitoringBot.Application.Events;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Abstractions;

public interface IEventsMonitoringProcessor<TEntity>
{
    event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, ServiceContext, Task>? EntitiesJoined;
    event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, ServiceContext, Task>? EntitiesLeft;
    Task ExecuteMonitoringAsync(ServiceContext serviceContext);
}
