using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Services;

public class EntitiesChangeDetector<TEventArgument> : IEntitiesChangeDetector<TEventArgument>
{
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEventArgument>, Task>? EntitiesJoined;
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEventArgument>, Task>? EntitiesLeft;

    private bool HasValidItems(List<TEventArgument> list) =>
        list is not null && list.Count > 0;

    private List<TEventArgument> ExistInFirstAbsentInSecond(
        IEnumerable<TEventArgument> firstCollection,
        IEnumerable<TEventArgument> secondCollection) =>

        firstCollection.Except(secondCollection).ToList();

    public async Task ExecuteMonitoringAsync(IEnumerable<TEventArgument> usersCollectionFromApi,
        IEnumerable<TEventArgument> usersCollectionFromRepository)
    {
        var left = ExistInFirstAbsentInSecond(usersCollectionFromRepository, usersCollectionFromApi);
        var joined = ExistInFirstAbsentInSecond(usersCollectionFromApi, usersCollectionFromRepository);
        
        if (HasValidItems(left))
        {
            await RaiseEntityCollectionChangedEvent(
                EntitiesLeft,
                new EntitiesCollectionChangedEventArgs<TEventArgument>(
                    left.Count,
                    left));
        }

        if(HasValidItems(joined))
        {
            await RaiseEntityCollectionChangedEvent(
                EntitiesJoined,
                new EntitiesCollectionChangedEventArgs<TEventArgument>(
                    joined.Count,
                    joined));
        }
    }

    private async Task RaiseEntityCollectionChangedEvent(
        Func<object?, EntitiesCollectionChangedEventArgs<TEventArgument>, Task>? @event,
        EntitiesCollectionChangedEventArgs<TEventArgument> e)
    {
        if (@event != null)
            await @event(this, e);
    }
}
