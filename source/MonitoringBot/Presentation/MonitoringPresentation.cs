using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Presentation;

public class MonitoringPresentation(MessageBuilder messageBuilder)
{
    public string FormatJoinedUsers(IEnumerable<ChannelMember> usersDifference)
    {
        string message = "Ура, новые подпищщики!🍾🎊🎈\r\n\r\n";
        message += FormatOneOrManyMembers(usersDifference);
        return message;
    }

    public string FormatLeftUsers(IEnumerable<ChannelMember> usersDifference)
    {
        string message = "Неееет! От нас свалили!👎💩🐀\r\n\r\n";
        message += FormatOneOrManyMembers(usersDifference);
        return message;
    }

    public string FormatOneOrManyMembers(IEnumerable<ChannelMember> usersDifference)
    {
        return usersDifference.Count() == 1
                    ? messageBuilder.FormatMember(usersDifference.FirstOrDefault() ?? new ChannelMember())
                    : messageBuilder.FormatMembers(usersDifference);
    }
}
