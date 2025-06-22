using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Presentation;

namespace MonitoringBot.Services.MessagesSending;

public sealed class SubscribersChangeMessageProducer(
    MonitoringPresentation monitoringPresentation) : EntitiesChangeMessageProducer<ChannelMember>
{
    protected override string CreateJoinedMessage(EntitiesCollectionChangedEventArgs<ChannelMember> e) =>
        monitoringPresentation!.FormatJoinedUsers(e.EntitiesDifference!, e.EventType);

    protected override string CreateLeftMessage(EntitiesCollectionChangedEventArgs<ChannelMember> e) =>
        monitoringPresentation!.FormatLeftUsers(e.EntitiesDifference!, e.EventType);
}
