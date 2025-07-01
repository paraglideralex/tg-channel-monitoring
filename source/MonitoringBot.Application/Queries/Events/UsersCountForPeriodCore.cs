using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Projections;

namespace MonitoringBot.Application.Queries.Events;

public sealed class UsersCountForPeriodCore
{
    public DateWithUsersCount[] Execute(long currentCount,
        List<EventTypeWithDate> eventTypesToDate,
        DateTime fromNonInclusive,
        DateTime toInclusive,
        TimeSpan step)
    {
        var closestTime = PreviousDateTimeAlignedDown(toInclusive, step);
        var furthestTime = PreviousDateTimeAlignedDown(fromNonInclusive, step);

        var dict = DatesSequenceDictionary(step, furthestTime, closestTime);
        var keys = dict.Keys.ToArray();
        if (eventTypesToDate.Count is 0)
            return FillDictionaryWithConstant(dict, keys.Length, currentCount);

        long backCounter = currentCount;
        foreach(var concreteEvent in eventTypesToDate)
        {
            var previousDateTimeAlignedDown = PreviousDateTimeAlignedDown(concreteEvent.TimeStamp, step);
            var valueExists = dict.TryGetValue(previousDateTimeAlignedDown, out _);

            if (valueExists)
            {
                backCounter += StepDifference(concreteEvent.EventType);
                dict[previousDateTimeAlignedDown].Add(concreteEvent);
            }
        }

        long frontCounter = backCounter;
        var output = new DateWithUsersCount[keys.Length];
        for(int i = dict.Keys.Count - 1; i >= 0; i--)
        {
            var key = keys[i];
            foreach(var value in dict[key])
                frontCounter -= StepDifference(value.EventType);

            output[i] = new(key, frontCounter);
        }
        return output;
    }

    private Dictionary<DateTime, List<EventTypeWithDate>> DatesSequenceDictionary(
        TimeSpan step,
        DateTime laterBorderNonInclusive,
        DateTime earlierBorderInclusive)
    {
        Dictionary<DateTime, List<EventTypeWithDate>> dict = [];

        while (earlierBorderInclusive >= laterBorderNonInclusive)
        {
            dict[earlierBorderInclusive] = [];
            earlierBorderInclusive -= step;
        }

        return dict;
    }

    private DateTime PreviousDateTimeAlignedDown(DateTime inputDateTime, TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
            throw new ArgumentException("TimeSpan must be positive.", nameof(step));

        long ticksPerStamp = step.Ticks;
        long inputTicks = inputDateTime.Ticks;

        // return if input is divisible to step
        if (inputTicks % ticksPerStamp == 0)
            return inputDateTime;

        // else - align down
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

    private DateWithUsersCount[] FillDictionaryWithConstant(Dictionary<DateTime, List<EventTypeWithDate>> dictionary, int arrayLength, long constant)
    {
        var array = new DateWithUsersCount[arrayLength];

        int counter = 0;
        foreach (var key in dictionary.Keys)
        {
            array[counter] = new(key, constant);
            counter++;
        }
        return array;
    }
        
}

public record struct DateWithUsersCount(DateTime Date, long UsersCount);