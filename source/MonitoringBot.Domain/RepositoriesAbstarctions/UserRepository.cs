using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Infrastructure;

public abstract class UserRepository
{
    public abstract Task<long> CountAsync();

    public abstract Task<long> CountByLastActionAsync(string lastAction);
    public abstract Task<List<long>> Keys();
    public abstract Task<List<ChannelMember>> All();
    public abstract Task<List<ChannelMember>> AllSubscribed();

    public abstract Task AddAsync(ChannelMember user, string? lastAction);

    public abstract Task AddRangeAsync(IEnumerable<ChannelMember> users, string? lastAction);

    public abstract Task UpdateAsync(ChannelMember user, string? lastAction, DateTime timeStam);
    public abstract Task UpdateRangeAsync(IEnumerable<ChannelMember> users, string? lastAction, DateTime timeStamp);

    public abstract Task DeleteAsync(ChannelMember user);

    public abstract Task DeleteRangeAsync(IEnumerable<ChannelMember> users);

    public abstract Task<ChannelMember?> FindById(long identity);
    public abstract Task<List<ChannelMember>> FindByIds(IEnumerable<long> identities);

    public abstract Task<List<ChannelMember>> TakeLast(int count = 5);
    public abstract Task<List<ChannelMember>> TakeLastByAction(string lastAction, int count = 5);
}
