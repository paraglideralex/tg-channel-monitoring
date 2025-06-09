using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Events;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Domain.Events.ChannelMembers;

namespace MonitoringBot.Application.Services;

public class EventsMonitoringProcessor<TEntity>(
    GetEventsInPeriodQueryExecution<TEntity> getEventsInPeriodQueryExecution) : IEventsMonitoringProcessor<TEntity>
{
    protected DateTime lastCheckTimeStamp;
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesJoined;
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? EntitiesLeft;

    private bool HasValidItems(List<TEntity?>? list) =>
        list is not null && list.Count > 0;

    public async Task ExecuteMonitoringAsync()
    {
        var allEvents = await getEventsInPeriodQueryExecution.ExecuteAsync(
            new GetEventsInPeriodQuery
            {
                From = lastCheckTimeStamp,
                To = DateTime.Now
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
                    nameof(SubscriberLeftEvent)));
        }

        if (HasValidItems(joined))
        {
            await RaiseEntityCollectionChangedEvent(
                EntitiesJoined,
                new EntitiesCollectionChangedEventArgs<TEntity>(
                    joined.Count,
                    joined!,
                    nameof(SubscriberJoinedEvent)));
        }

        lastCheckTimeStamp = DateTime.Now;
    }

    private async Task RaiseEntityCollectionChangedEvent(
        Func<object?, EntitiesCollectionChangedEventArgs<TEntity>, Task>? @event,
        EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        if (@event != null)
            await @event(this, e);
    }
}
