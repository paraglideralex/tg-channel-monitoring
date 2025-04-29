using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Abstractions;

public interface ISubscribersChangeDetector
{
    event Func<object?, SubscribersChangedEventArgs, Task>? SubscribersChanged;
    public Task ExecuteMonitoring(
        IEnumerable<ChannelMember> usersCollectionFromApi,
        IEnumerable<ChannelMember> usersCollectionFromRepository);
}
