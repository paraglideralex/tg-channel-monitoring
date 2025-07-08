using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries.Events;

/// <summary>
/// Subscribers quantity is returned to each period according to actual count at the END of each period divided by "Step" parameter
/// </summary>
public sealed class GetUsersCountForPeriodQueryExecution(
    UserRepository userRepository,
    EventRepository<ChannelMember> eventsRepository,
    UsersCountForPeriodCore usersCountForPeriodCore)
{
    public async Task<DateWithUsersCount[]> ExecuteAsync(GetUsersCountForPeriodQuery query)
    {
        var countTask = await userRepository.CountByLastActionAsync(nameof(SubscriberJoinedEvent));
        var eventTypesOrdered = await eventsRepository.GetEventTypesInPeriodAsync(
            query.AggregateName,
            query.FromNonInclusive,
            query.ToInclusive);

        var currentCount = countTask;
        var eventTypesToDate = eventTypesOrdered;

        return usersCountForPeriodCore.Execute(currentCount, eventTypesToDate, query.FromNonInclusive, query.ToInclusive, query.Step);
    }
}
