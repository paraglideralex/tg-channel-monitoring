using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries.Events;

/// <summary>
/// Subscribers quantity is returned to each period according to actual count at the END of each period divided by "Step" parameter
/// </summary>
/// <param name="userRepository"></param>
/// <param name="eventsRepository"></param>
public sealed class GetUsersCountForPeriodQueryExecution(
    UserRepository userRepository,
    EventRepository<ChannelMember> eventsRepository)
{
    public async Task<List<DateWithUsersCount>> ExecuteAsync(GetUsersCountForPeriodQuery query)
    {
        var countTask = userRepository.CountByLastActionAsync(nameof(SubscriberJoinedEvent));
        var eventTypesOrdered = eventsRepository.GetEventTypesInPeriodAsync(
            query.FromNonInclusive,
            query.ToInclusive);

        await Task.WhenAll(countTask, eventTypesOrdered);

        var currentCount = countTask.Result;
        var eventTypesToDate = eventTypesOrdered.Result;

        if (eventTypesToDate.Count is 0)
            return [];

        var dateWithUserSequence = new List<DateWithUsersCount> { new(query.ToInclusive, currentCount) };

        if(eventTypesToDate.Count is 1)
            return dateWithUserSequence;

        var counter = currentCount;
        var firstDateFloored = query.ToInclusive.Date;

        var currentStepDate = firstDateFloored;
        for (int j = 1; j < eventTypesToDate.Count; j++)
        {
            var current = eventTypesToDate[j];
            var previous = eventTypesToDate[j - 1];

            var currentTime = GetPreviousDateTimeAligned(current.TimeStamp, query.Step);
            var isFromCurrentPeriod = currentTime + query.Step <= currentStepDate;

            var currentStepDifference = StepDifference(previous.EventType);
            counter += currentStepDifference;

            if (isFromCurrentPeriod)
            {
                currentStepDate = GetPreviousDateTimeAligned(current.TimeStamp, query.Step);
                dateWithUserSequence.Add(new(currentStepDate, counter));
            }

        }
        return dateWithUserSequence;
    }

    private int StepDifference(string? eventType) =>
    eventType switch
    {
        nameof(SubscriberJoinedEvent) => -1,
        nameof(SubscriberLeftEvent) => 1,
        _ => 0
    };

    //private long CountToCurrentDateBeginning(
    //    List<EventTypeWithDate> eventTypesToDateNonInclusive,
    //    DateTime nextDate,
    //    long previousDateUsersCount,
    //    int previousArrayIndex,
    //    TimeSpan span,
    //    out int nextArrayIndex)
    //{
    //    var dateWithUserSequence = new List<DateWithUsersCount>();
    //    nextArrayIndex = previousArrayIndex;
    //    long usersCounter = previousDateUsersCount;
    //    var date1 = nextDate;
    //    for (int i = previousArrayIndex; i < eventTypesToDateNonInclusive.Count; i++)
    //    {
    //        var current = eventTypesToDateNonInclusive[i];

    //        var isFromCurrentPeriod = GetPreviousDateTimeAligned(current.TimeStamp, span);

    //        if (current.TimeStamp <= nextDate)
    //        {
    //            nextArrayIndex = i;
    //            break;
    //        }

    //        var previous = eventTypesToDateNonInclusive[i - 1];
    //        var currentStepDifference = StepDifference(previous.EventType);
    //        usersCounter += currentStepDifference;
    //    }
    //    return usersCounter;
    //}


    //private long CountToCurrentDateBeginningK1(
    //    List<EventTypeWithDate> eventTypesToDateNonInclusive,
    //    DateTime nextDate,
    //    long previousDateUsersCount,
    //    int previousArrayIndex,
    //    out int nextArrayIndex)
    //{
        


    //    nextArrayIndex = previousArrayIndex;
    //    long usersCounter = previousDateUsersCount;
    //    for (int i = previousArrayIndex; i < eventTypesToDateNonInclusive.Count; i++)
    //    {
    //        var current = eventTypesToDateNonInclusive[i];

    //        if (current.TimeStamp <= nextDate)
    //        {
    //            nextArrayIndex = i;
    //            break;
    //        }

    //        var previous = eventTypesToDateNonInclusive[i - 1];
    //        var currentStepDifference = StepDifference(previous.EventType);
    //        usersCounter += currentStepDifference;
    //    }
    //    return usersCounter;
    //}

    public static DateTime GetPreviousDateTimeAligned(DateTime inputDateTime, TimeSpan stamp)
    {
        if (stamp <= TimeSpan.Zero)
            throw new ArgumentException("TimeSpan must be positive.", nameof(stamp));

        long ticksPerStamp = stamp.Ticks;
        long inputTicks = inputDateTime.Ticks;
        long alignedTicks = inputTicks / ticksPerStamp * ticksPerStamp;

        DateTime alignedDateTime = new(alignedTicks);

        if (alignedDateTime == inputDateTime)
            alignedDateTime = alignedDateTime.AddTicks(-ticksPerStamp);

        return alignedDateTime;
    }
}

public record struct DateWithUsersCount(DateTime Date, long UsersCount);
