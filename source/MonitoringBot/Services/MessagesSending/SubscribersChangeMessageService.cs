using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Presentation;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public sealed class SubscribersChangeMessageService(
    TelegramBotClient telegramBotClient, 
    List<long> botUsers,
    MonitoringPresentation monitoringPresentation) : 
    EntitiesChangeMessagingService<ChannelMember>(telegramBotClient, botUsers)
{
    protected override string CreateJoinedMessage(EntitiesCollectionChangedEventArgs<ChannelMember> e) => 
        monitoringPresentation!.FormatJoinedUsers(e.EntitiesDifference!);

    protected override string CreateLeftMessage(EntitiesCollectionChangedEventArgs<ChannelMember> e) =>
        monitoringPresentation!.FormatLeftUsers(e.EntitiesDifference!);
}
