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

    protected internal override string CreateMessage(EntitiesChangedEventArgs<ChannelMember> e) => 
        monitoringPresentation.FormatLeftOrJoinedUsers(
            e.DifferenceCount,
            e.EntitiesDifference);
}
