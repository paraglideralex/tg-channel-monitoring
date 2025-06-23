using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Presentation;

namespace MonitoringBot.Services.MessagesSending;

public sealed class SubscribersChangeMessageProducer(
    MonitoringPresentation monitoringPresentation) : EntitiesChangeMessageProducer<ChannelMember>
{
    protected override async Task<string> CreateJoinedMessageAsync(EntitiesCollectionChangedEventArgs<ChannelMember> e) =>
        await monitoringPresentation!.FormatJoinedUsers(e.EntitiesDifference!, e.EventType);

    protected override async Task<string> CreateLeftMessageAsync(EntitiesCollectionChangedEventArgs<ChannelMember> e) =>
        await monitoringPresentation!.FormatLeftUsers(e.EntitiesDifference!, e.EventType);
}
