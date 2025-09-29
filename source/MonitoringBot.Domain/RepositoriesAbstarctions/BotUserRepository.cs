using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Domain.RepositoriesAbstarctions;

public abstract class BotUserRepository
{
    public abstract Task AddBotUserAsync(BotUser user, DateTime timeStamp, CancellationToken token);
    public abstract Task UpdateBotUserAsync(BotUser user, bool isCurrent, DateTime timeStamp, CancellationToken token);
    public abstract Task<IReadOnlyCollection<BotUser>> GetCurrentUsers(CancellationToken token);
    public abstract Task<IReadOnlyCollection<BotUser>> GetAllUsers(CancellationToken token);
}
