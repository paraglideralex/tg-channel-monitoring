using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Domain.Events.ChannelMembers;
public sealed class SubscriberLeftEvent : EntitiesChangedDomainEventBase<ChannelMember>
{
}
