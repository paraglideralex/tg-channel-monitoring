using MonitoringBot.Infrastructure.Persistence.Entities;

namespace MonitoringBot.Domain.RepositoriesAbstarctions;

public abstract class ApiUsersRepository
{
    public abstract Task<IReadOnlyCollection<TLUser>> GetUsersByIdsAsync(IReadOnlyCollection<long> ids);

    public abstract Task<IReadOnlyCollection<TLUser>> GetCurrentUsersAsync();

    public abstract Task<List<long>> GetCurrentUsersIdentitiesAsync();

    public abstract Task RefreshUsersAsync(
        IReadOnlyCollection<TLUser> users,
        DateTime timeStamp);
}
