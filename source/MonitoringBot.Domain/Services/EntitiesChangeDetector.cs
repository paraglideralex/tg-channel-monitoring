using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Services;

public class EntitiesChangeDetector<TEventArgument> : IEntitiesChangeDetector<TEventArgument>
{
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEventArgument>, Task>? EntitiesChanged;

    public event Func<object?, EntitiesCollectionChangedEventArgs<TEventArgument>, Task>? EntitiesJoined;
    public event Func<object?, EntitiesCollectionChangedEventArgs<TEventArgument>, Task>? EntitiesLeft;

    private bool HasValidItems(List<TEventArgument> list) =>
        (list is null || list.Count == 0) is false;

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


    private List<TEventArgument> UsersDifference(
        IEnumerable<TEventArgument> usersCollectionFromApi,
        IEnumerable<TEventArgument> usersCollectionFromRepository) => 

        usersCollectionFromRepository.SymmetricDifference(usersCollectionFromApi).ToList();

    private int UsersDifferenceCount(
        IEnumerable<TEventArgument> usersCollectionFromApi,
        IEnumerable<TEventArgument> usersCollectionFromRepository) =>

        usersCollectionFromApi.Count() - usersCollectionFromRepository.Count();

    public async Task ExecuteMonitoring(
        IEnumerable<TEventArgument> usersCollectionFromApi,
        IEnumerable<TEventArgument> usersCollectionFromRepository)
    {
        var currentStepDifferenceCount = UsersDifferenceCount(usersCollectionFromApi, usersCollectionFromRepository);
        var currentStepMemberDifference = UsersDifference(usersCollectionFromApi, usersCollectionFromRepository);

        if (currentStepDifferenceCount is not 0)
            await OnSubscribersChanged(new EntitiesCollectionChangedEventArgs<TEventArgument>(currentStepDifferenceCount, currentStepMemberDifference));
    }

    protected virtual async Task OnSubscribersChanged(EntitiesCollectionChangedEventArgs<TEventArgument> e)
    {
        if (EntitiesChanged is not null)
            await EntitiesChanged(this, e);
    }

    private async Task RaiseEntityCollectionChangedEvent(
        Func<object?, EntitiesCollectionChangedEventArgs<TEventArgument>, Task>? @event,
        EntitiesCollectionChangedEventArgs<TEventArgument> e)
    {
        if (@event != null)
            await @event(this, e);
    }
}
