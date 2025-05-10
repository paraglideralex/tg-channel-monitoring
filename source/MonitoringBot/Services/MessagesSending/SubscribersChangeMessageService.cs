using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Presentation;

using Telegram.BotAPI;

namespace MonitoringBot.Services.MessagesSending;

public sealed class SubscribersChangeMessageService : EntitiesChangeMessagingService<ChannelMember>
{
    private MonitoringPresentation monitoringPresentation;
    public SubscribersChangeMessageService(TelegramBotClient telegramBotClient, List<long> botUsers,
        MonitoringPresentation monitoringPresentation) : base(telegramBotClient, botUsers)
    {
        this.monitoringPresentation = monitoringPresentation;
    }

    protected internal override string CreateJoinedMessage(EntitiesCollectionChangedEventArgs<ChannelMember> e) => 
        monitoringPresentation!.FormatJoinedUsers(e.EntitiesDifference!);

    protected internal override string CreateLeftMessage(EntitiesCollectionChangedEventArgs<ChannelMember> e) =>
        monitoringPresentation!.FormatLeftUsers(e.EntitiesDifference!);

}
