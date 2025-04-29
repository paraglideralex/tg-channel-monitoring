using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Services;

public class EntitiesChangeDetector<TEventArgument> : IEntitiesChangeDetector<TEventArgument>
{
    public event Func<object?, EntitiesChangedEventArgs<TEventArgument>, Task>? EntitiesChanged;

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
            await OnSubscribersChanged(new EntitiesChangedEventArgs<TEventArgument>(currentStepDifferenceCount, currentStepMemberDifference));
    }

    protected virtual async Task OnSubscribersChanged(EntitiesChangedEventArgs<TEventArgument> e)
    {
        if (EntitiesChanged is not null)
            await EntitiesChanged(this, e);
    }
}
