using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Application.Abstractions;

public interface IMonitoringService<TEntity, TOnJoinedEvent, TOnLeftEvent>
    where TEntity : ISearchableEntity
    where TOnJoinedEvent : EntitiesChangedDomainEventBase<TEntity>, new()
    where TOnLeftEvent : EntitiesChangedDomainEventBase<TEntity>, new()
{
    public Task ProcessMonitoringAsync();
}
