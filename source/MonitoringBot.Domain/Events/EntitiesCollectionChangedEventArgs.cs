namespace MonitoringBot.Domain.Events;

public class EntitiesCollectionChangedEventArgs<T> : EventArgs
{
    public long DifferenceCount { get; }
    public IEnumerable<T> EntitiesDifference { get; }

    public EntitiesCollectionChangedEventArgs(long differenceCount, IEnumerable<T> entitiesDifference)
    {
        DifferenceCount = differenceCount;
        EntitiesDifference = entitiesDifference;
    }
}

