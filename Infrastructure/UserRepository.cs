namespace MonitoringBot.Infrastructure;

public abstract class UserRepository
{
    public abstract Task<long> Count();

    public abstract Task<List<long>> Keys();

    public abstract Task Add(ChannelMember user);

    public abstract Task AddRange(IEnumerable<ChannelMember> users);

    public abstract Task Delete(ChannelMember user);

    public abstract Task DeleteRange(IEnumerable<ChannelMember> users);

    public abstract Task<List<ChannelMember>> FindByIds(IEnumerable<long> identities);

    public abstract Task<List<ChannelMember>> FindByIds(IEnumerable<ChannelMember> usersCollection, IEnumerable<long> identities);

    public abstract Task<List<ChannelMember>> Difference(IEnumerable<ChannelMember> users);

    public abstract Task<List<ChannelMember>> TakeLast(int count = 5);
}
