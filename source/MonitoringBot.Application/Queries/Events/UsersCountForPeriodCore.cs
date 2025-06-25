using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Projections;

namespace MonitoringBot.Application.Queries.Events;

public sealed class UsersCountForPeriodCore
{
    public List<DateWithUsersCount> Execute(long currentCount,
        List<EventTypeWithDate> eventTypesToDate,
        DateTime periodEndInclusive,
        TimeSpan step)
    {
        if (eventTypesToDate.Count is 0)
            return [];

        var dateWithUserSequence = new List<DateWithUsersCount> { new(periodEndInclusive, currentCount) };

        if (eventTypesToDate.Count is 1)
            return dateWithUserSequence;

        var counter = currentCount;
        var firstDateFloored = periodEndInclusive.Date;

        var currentStepDate = firstDateFloored;
        for (int j = 1; j < eventTypesToDate.Count; j++)
        {
            var current = eventTypesToDate[j];
            var previous = eventTypesToDate[j - 1];

            var currentTime = GetPreviousDateTimeAligned(current.TimeStamp, step);
            var isFromCurrentPeriod = currentTime + step <= currentStepDate;

            var currentStepDifference = StepDifference(previous.EventType);
            counter += currentStepDifference;

            if (isFromCurrentPeriod)
            {
                currentStepDate = GetPreviousDateTimeAligned(current.TimeStamp, step);
                dateWithUserSequence.Add(new(currentStepDate, counter));
            }
        }
        return dateWithUserSequence;
    }

    private DateTime GetPreviousDateTimeAligned(DateTime inputDateTime, TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
            throw new ArgumentException("TimeSpan must be positive.", nameof(step));

        long ticksPerStamp = step.Ticks;
        long inputTicks = inputDateTime.Ticks;
        long alignedTicks = inputTicks / ticksPerStamp * ticksPerStamp;

        DateTime alignedDateTime = new(alignedTicks);

        if (alignedDateTime == inputDateTime)
            alignedDateTime = alignedDateTime.AddTicks(-ticksPerStamp);

        return alignedDateTime;
    }

    private int StepDifference(string? eventType) =>
    eventType switch
    {
        nameof(SubscriberJoinedEvent) => -1,
        nameof(SubscriberLeftEvent) => 1,
        _ => 0
    };
}

public record struct DateWithUsersCount(DateTime Date, long UsersCount);