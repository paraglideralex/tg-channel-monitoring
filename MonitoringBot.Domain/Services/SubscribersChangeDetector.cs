using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Services;

public class SubscribersChangeDetector : ISubscribersChangeDetector
{
    public event Func<object?, SubscribersChangedEventArgs, Task>? SubscribersChanged;

    private List<ChannelMember> UsersDifference(
        IEnumerable<ChannelMember> usersCollectionFromApi,
        IEnumerable<ChannelMember> usersCollectionFromRepository) => 

        usersCollectionFromRepository.SymmetricDifference(usersCollectionFromApi).ToList();

    private int UsersDifferenceCount(
        IEnumerable<ChannelMember> usersCollectionFromApi,
        IEnumerable<ChannelMember> usersCollectionFromRepository) =>

        usersCollectionFromApi.Count() - usersCollectionFromRepository.Count();

    public async Task ExecuteMonitoring(
        IEnumerable<ChannelMember> usersCollectionFromApi,
        IEnumerable<ChannelMember> usersCollectionFromRepository)
    {
        var currentStepDifferenceCount = UsersDifferenceCount(usersCollectionFromApi, usersCollectionFromRepository);
        var currentStepMemberDifference = UsersDifference(usersCollectionFromApi, usersCollectionFromRepository);

        if (currentStepDifferenceCount is not 0)
            await OnSubscribersChanged(new SubscribersChangedEventArgs(currentStepDifferenceCount, currentStepMemberDifference));
    }

    protected virtual async Task OnSubscribersChanged(SubscribersChangedEventArgs e)
    {
        if (SubscribersChanged is not null)
            await SubscribersChanged(this, e);
    }
}
