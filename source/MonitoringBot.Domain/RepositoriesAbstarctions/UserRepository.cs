using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Infrastructure;

public abstract class UserRepository
{
    public abstract Task<long> CountByLastActionAsync(string lastAction, CancellationToken cancellationToken);
    public abstract Task<List<ChannelMember>> AllSubscribed();

    public abstract Task AddRangeAsync(IEnumerable<ChannelMember> users, string? lastAction, CancellationToken cancellationToken);

    public abstract Task UpdateAsync(ChannelMember user, string? lastAction, DateTime timeStam, CancellationToken cancellationToken);
    public abstract Task UpdateRangeAsync(IEnumerable<ChannelMember> users, string? lastAction, DateTime timeStamp, CancellationToken cancellationToken);

    public abstract Task<ChannelMember?> FindByIdAsync(long identity, CancellationToken cancellationToken);
    public abstract Task<List<ChannelMember>> FindByIdsAsync(IEnumerable<long> identities, CancellationToken cancellationToken);

    public abstract Task<List<ChannelMember>> TakeLast(int count = 5, CancellationToken cancellationToken = default);
    public abstract Task<List<ChannelMember>> TakeLastByActionAsync(string lastAction, CancellationToken cancellationToken, int count = 5);
    public abstract Task<List<long>> AllSubscribedIdentitiesAsync(CancellationToken cancellationToken);
    public abstract Task<ChannelMember?> FindByNickNameAsync(string nickName, CancellationToken cancellationToken);
}
