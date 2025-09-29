using MonitoringBot.Infrastructure.Persistence.Entities;

namespace MonitoringBot.Domain.RepositoriesAbstarctions;

public abstract class ApiUsersRepository
{
    public abstract Task<IReadOnlyCollection<TLUser>> GetUsersByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken);

    public abstract Task<IReadOnlyCollection<TLUser>> GetCurrentUsersAsync(CancellationToken cancellationToken);

    public abstract Task<List<long>> GetCurrentUsersIdentitiesAsync(CancellationToken cancellationToken);

    public abstract Task RefreshUsersAsync(
        IReadOnlyCollection<TLUser> users,
        DateTime timeStamp,
        CancellationToken cancellationToken);
}
