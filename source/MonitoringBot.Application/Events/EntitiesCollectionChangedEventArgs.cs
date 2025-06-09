namespace MonitoringBot.Application.Events;

public class EntitiesCollectionChangedEventArgs<T> : EventArgs
{
    public long DifferenceCount { get; }
    public IEnumerable<T> EntitiesDifference { get; }
    public string EventType { get; }

    public EntitiesCollectionChangedEventArgs(long differenceCount, IEnumerable<T> entitiesDifference, string eventType)
    {
        DifferenceCount = differenceCount;
        EntitiesDifference = entitiesDifference;
        EventType = eventType;
    }
}

