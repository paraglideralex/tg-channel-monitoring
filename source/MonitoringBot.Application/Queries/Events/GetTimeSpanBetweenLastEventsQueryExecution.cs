using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.RepositoriesAbstarctions;

namespace MonitoringBot.Application.Queries.Events;

public class GetTimeSpanBetweenLastEventsQueryExecution<TEntity>(
    EventRepository<TEntity> eventRepository,
    ITimeProvider timeProvider)
{
    public async Task<TimeSpan?> ExecuteAsync(
        GetLastPreviousEventQuery query)
    {
        if (query.LastAction is null)
            return null;

        var opposite = OppositeAction(query);
        var previous = opposite is null 
            ? null 
            : await eventRepository.GetLatestEventByTypeAndIdentity(opposite, query.UserIdentity);

        return previous is not null
            ? timeProvider.UtcNow - previous?.TimeStamp
            : null;
    }

    private string? OppositeAction(GetLastPreviousEventQuery query)
    {
        var eventType = query.LastAction;

        return eventType switch
        {
            nameof(SubscriberJoinedEvent) => nameof(SubscriberLeftEvent),
            nameof(SubscriberLeftEvent) => nameof(SubscriberJoinedEvent),
            _ => null
        };
    }
}