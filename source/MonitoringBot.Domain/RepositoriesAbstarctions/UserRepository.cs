using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Infrastructure;

public abstract class UserRepository
{
    public abstract Task<long> CountAsync();

    public abstract Task<List<long>> Keys();
    public abstract Task<List<ChannelMember>> All();
    public abstract Task<List<ChannelMember>> AllSubscribed();

    public abstract Task AddAsync(ChannelMember user, string? lastAction);

    public abstract Task AddRange(IEnumerable<ChannelMember> users, string? lastAction);

    public abstract Task UpdateAsync(ChannelMember user, string? lastAction);
    public abstract Task UpdateRangeAsync(IEnumerable<ChannelMember> users, string? lastAction);

    public abstract Task Delete(ChannelMember user);

    public abstract Task DeleteRange(IEnumerable<ChannelMember> users);

    public abstract Task<List<ChannelMember>> FindByIds(IEnumerable<long> identities);

    public abstract Task<List<ChannelMember>> TakeLast(int count = 5);
    public abstract Task<List<ChannelMember>> TakeLastByAction(string lastAction, int count = 5);
}
