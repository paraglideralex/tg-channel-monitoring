using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Presentation;

public class MonitoringPresentation(MessageBuilder messageBuilder)
{
    public string FormatJoinedUsers(IEnumerable<ChannelMember> usersDifference, string eventType)
    {
        string message = "Ура, новые подпищщики!🍾🎊🎈\r\n\r\n";
        message += FormatOneOrManyMembers(usersDifference, eventType);
        return message;
    }

    public string FormatLeftUsers(IEnumerable<ChannelMember> usersDifference, string eventType)
    {
        string message = "Неееет! От нас свалили!👎💩🐀\r\n\r\n";
        message += FormatOneOrManyMembers(usersDifference, eventType);
        return message;
    }

    public async Task<string> FormatOneOrManyMembers(IEnumerable<ChannelMember> usersDifference, string eventType)
    {
        return usersDifference.Count() == 1
                    ? await messageBuilder.FormatMemberAsync(usersDifference.FirstOrDefault() ?? new ChannelMember(), eventType)
                    : await messageBuilder.FormatMembersAsync(usersDifference, eventType);
    }
}
