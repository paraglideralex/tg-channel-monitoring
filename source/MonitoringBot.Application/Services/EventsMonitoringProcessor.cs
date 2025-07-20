using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Events;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Services;

public class EventsMonitoringProcessor<TEntity>(
    GetEventsInPeriodQueryExecution<TEntity> getEventsInPeriodQueryExecution,
    ITimeProvider timeProvider) : IEventsMonitoringProcessor<TEntity>
{
    protected DateTime lastCheckTimeStamp = timeProvider.UtcNow;
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, ServiceContext, Task>? EntitiesJoined;
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, ServiceContext, Task>? EntitiesLeft;

    private bool HasValidItems(List<TEntity?>? list) =>
        list is not null && list.Count > 0;

    public async Task ExecuteMonitoringAsync(ServiceContext serviceContext)
    {
        var allEvents = await getEventsInPeriodQueryExecution.ExecuteAsync(
            new GetEventsInPeriodQuery
            {
                FromNonInclusive = lastCheckTimeStamp,
                ToInclusive = timeProvider.UtcNow
            });

        var joined = new List<TEntity?>();
        var left = new List<TEntity?>();

        foreach (var eventType in allEvents)
        {
            if(eventType.GetType().Name == nameof(SubscriberJoinedEvent))
                joined.Add(eventType.Entity);
            if(eventType.GetType().Name == nameof(SubscriberLeftEvent))
                left.Add(eventType.Entity);
        }

        if (HasValidItems(left))
        {
            await RaiseEntityCollectionChangedEvent(
                EntitiesLeft,
                new EntitiesCollectionChangedEventArgs<TEntity>(
                    left.Count,
                    left!,
                    nameof(SubscriberLeftEvent)),
                serviceContext);
        }

        if (HasValidItems(joined))
        {
            await RaiseEntityCollectionChangedEvent(
                EntitiesJoined,
                new EntitiesCollectionChangedEventArgs<TEntity>(
                    joined.Count,
                    joined!,
                    nameof(SubscriberJoinedEvent)),
                serviceContext);
        }

        lastCheckTimeStamp = timeProvider.UtcNow;
    }

    private async Task RaiseEntityCollectionChangedEvent(
        Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, ServiceContext, Task>? @event,
        EntitiesCollectionChangedEventArgs<TEntity> e,
        ServiceContext serviceContext)
    {
        if (@event != null)
            await @event(this, e, serviceContext);
    }
}
