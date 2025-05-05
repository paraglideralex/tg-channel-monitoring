using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Presentation;

public class MonitoringPresentation(MessageBuilder messageBuilder)
{
    public string? FormatLeftOrJoinedUsers(long currentStepDifferenceCount, IEnumerable<ChannelMember> usersDifference)
    {
        string? message = "";
        if (currentStepDifferenceCount > 0)
        {
            if (usersDifference != null)
            {
                message = "Ура, новые подпищщики!🍾🎊🎈\r\n\r\n";
                message += FormatOneOrManyMembers(usersDifference);
                return message;
            }
        }
        if (currentStepDifferenceCount < 0)
        {
            if (usersDifference != null)
            {
                message = "Неееет! От нас свалили!👎💩🐀\r\n\r\n";
                message += FormatOneOrManyMembers(usersDifference);
                return message;

            }
        }
        return message;
    }

    public string FormatOneOrManyMembers(IEnumerable<ChannelMember> usersDifference)
    {
        return usersDifference.Count() == 1
                    ? messageBuilder.FormatMember(usersDifference.FirstOrDefault() ?? new ChannelMember())
                    : messageBuilder.FormatMembers(usersDifference);
    }
}
