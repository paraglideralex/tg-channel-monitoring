using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Presentation;

public class MonitoringPresentation(
    MessageBuilder messageBuilder)
{
    public async Task<string> FormatJoinedUsers(IEnumerable<ChannelMember> usersDifference, string eventType, ServiceContext serviceContext)
    {
        string message = "Ура, новые подпищщики!🍾🎊🎈\r\n\r\n";
        message += await FormatOneOrManyMembers(usersDifference, eventType, serviceContext);
        return message;
    }

    public async Task<string> FormatLeftUsers(IEnumerable<ChannelMember> usersDifference, string eventType, ServiceContext serviceContext)
    {
        string message = "Неееет! От нас свалили!👎💩🐀\r\n\r\n";
        message += await FormatOneOrManyMembers(usersDifference, eventType, serviceContext);
        return message;
    }

    public async Task<string> FormatOneOrManyMembers(IEnumerable<ChannelMember> usersDifference, string eventType, ServiceContext serviceContext)
    {
        return usersDifference.Count() == 1
                    ? await messageBuilder.NotificationMessageForOneAsync(usersDifference.FirstOrDefault() ?? new ChannelMember(), serviceContext, eventType)
                    : await messageBuilder.NotificationMessageForManyAsync(usersDifference, serviceContext, eventType);
    }
}
