namespace MonitoringBot.Domain.Events;

public class EntitiesChangedEventArgs<T> : EventArgs
{
    public long DifferenceCount { get; }
    public IEnumerable<T> EntitiesDifference { get; }

    public EntitiesChangedEventArgs(long differenceCount, IEnumerable<T> entitiesDifference)
    {
        DifferenceCount = differenceCount;
        EntitiesDifference = entitiesDifference;
    }
}

