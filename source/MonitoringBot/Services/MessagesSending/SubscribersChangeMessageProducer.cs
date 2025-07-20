using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;
using MonitoringBot.Presentation;

namespace MonitoringBot.Services.MessagesSending;

public sealed class SubscribersChangeMessageProducer(
    MonitoringPresentation monitoringPresentation) : EntitiesChangeMessageProducer<ChannelMember>
{
    protected override async Task<string> CreateJoinedMessageAsync(EntitiesCollectionChangedEventArgs<ChannelMember> e, ServiceContext serviceContext) =>
        await monitoringPresentation!.FormatJoinedUsers(e.EntitiesDifference!, e.EventType, serviceContext);

    protected override async Task<string> CreateLeftMessageAsync(EntitiesCollectionChangedEventArgs<ChannelMember> e, ServiceContext serviceContext) =>
        await monitoringPresentation!.FormatLeftUsers(e.EntitiesDifference!, e.EventType, serviceContext);
}
