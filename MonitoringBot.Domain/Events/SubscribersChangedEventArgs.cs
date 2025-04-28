using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Domain.Events;

public class SubscribersChangedEventArgs : EventArgs
{
    public long DifferenceCount { get; }
    public IEnumerable<ChannelMember> MemberDifference { get; }

    public SubscribersChangedEventArgs(long differenceCount, IEnumerable<ChannelMember> memberDifference)
    {
        DifferenceCount = differenceCount;
        MemberDifference = memberDifference;
    }
}

