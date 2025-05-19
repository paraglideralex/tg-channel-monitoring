using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Domain.Events.ChannelMembers;

public sealed class SubscriberJoinedEvent : EntitiesChangedDomainEventBase<ChannelMember>
{
}
