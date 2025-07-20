using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure.Persistence.Entities;

namespace MonitoringBot.Infrastructure.Extensions;

public static class BotUserMapper
{
    public static BotUser ToDomain(this BotUserEntity entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        IsForum = entity.IsForum,
        Title = entity.Title,
        Type = entity.Type,
        UserName = entity.UserName
    };

    public static BotUserEntity ToEntity(this BotUser user, bool isCurrent, DateTime timeStamp) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        IsForum = user.IsForum,
        Title = user.Title,
        Type = user.Type,
        UserName = user.UserName,
        IsCurrent = isCurrent,
        TimeStamp = timeStamp
    };
}
