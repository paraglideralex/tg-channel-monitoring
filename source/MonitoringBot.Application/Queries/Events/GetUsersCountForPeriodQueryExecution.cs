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
        var countTask = userRepository.CountByLastActionAsync(nameof(SubscriberJoinedEvent));
        var eventTypesOrdered = eventsRepository.GetEventTypesInPeriodAsync(
            query.FromNonInclusive,
            query.ToInclusive);

        await Task.WhenAll(countTask, eventTypesOrdered);

        var currentCount = countTask.Result;
        var eventTypesToDate = eventTypesOrdered.Result;

        return usersCountForPeriodCore.Execute(currentCount, eventTypesToDate, query.FromNonInclusive, query.ToInclusive, query.Step);
    }
}
