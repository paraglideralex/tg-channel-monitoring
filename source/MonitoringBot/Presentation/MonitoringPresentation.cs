using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Presentation;

public class MonitoringPresentation(
    MessageBuilder messageBuilder)
{
    public async Task<string> FormatJoinedUsers(IEnumerable<ChannelMember> usersDifference, string eventType)
    {
        string message = "Ура, новые подпищщики!🍾🎊🎈\r\n\r\n";
        message += await FormatOneOrManyMembers(usersDifference, eventType);
        return message;
    }

    public async Task<string> FormatLeftUsers(IEnumerable<ChannelMember> usersDifference, string eventType)
    {
        string message = "Неееет! От нас свалили!👎💩🐀\r\n\r\n";
        message += await FormatOneOrManyMembers(usersDifference, eventType);
        return message;
    }

    public async Task<string> FormatOneOrManyMembers(IEnumerable<ChannelMember> usersDifference, string eventType)
    {
        return usersDifference.Count() == 1
                    ? await messageBuilder.NotificationMessageForOneAsync(usersDifference.FirstOrDefault() ?? new ChannelMember(), eventType)
                    : await messageBuilder.NotificationMessageForManyAsync(usersDifference, eventType);
    }
}
