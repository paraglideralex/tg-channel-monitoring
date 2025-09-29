using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;
using MonitoringBot.Infrastructure.Persistence.Entities;
using EFCore.BulkExtensions;


namespace MonitoringBot.Infrastructure.RepositoriesImplementations;

public sealed class ApiUsersRepositoryImplementation(
    IDbContextFactory<MonitoringBotDbContextBase> factory) : ApiUsersRepository
{
    public override async Task<IReadOnlyCollection<TLUser>> GetCurrentUsersAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ApiUsers
            .Where(u => u.IsCurrent)
            .Select(u => u.ToTLUser())
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<long>> GetCurrentUsersIdentitiesAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ApiUsers
            .Where(u => u.IsCurrent)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    public override async Task<IReadOnlyCollection<TLUser>> GetUsersByIdsAsync(
        IReadOnlyCollection<long> ids, 
        CancellationToken cancellationToken)
    {
        var idsSet = new HashSet<long>(ids);

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ApiUsers
            .Where(u => idsSet.Contains(u.Id))
            .Select(u => u.ToTLUser())
            .ToListAsync(cancellationToken);
    }

    public override async Task RefreshUsersAsync(
        IReadOnlyCollection<TLUser> usersFromApi,
        DateTime timeStamp,
        CancellationToken cancellationToken)
    {
        var currentUserIds = usersFromApi.Select(u => u.Id).ToHashSet();

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var existingUserIds = await dbContext.ApiUsers
            .Where(s => s.IsCurrent)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var existingUserIdSet = existingUserIds.ToHashSet();

        var newUsers = usersFromApi
            .Where(u => !existingUserIdSet.Contains(u.Id))
            .Select(u => u.ToEntity(true))
            .ToList();

        var idsToMarkNotCurrent = existingUserIdSet.Except(currentUserIds).ToList();

        var usersToMarkNotCurrent = new List<ApiUserEntity>();

        if (idsToMarkNotCurrent.Count > 0)
        {
            usersToMarkNotCurrent = await dbContext.ApiUsers
                .Where(s => s.IsCurrent && idsToMarkNotCurrent.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var user in usersToMarkNotCurrent)
            {
                user.IsCurrent = false;
                user.TimeStamp = timeStamp;
            }
        }

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (newUsers.Count > 0)
            await dbContext.BulkInsertOrUpdateAsync(newUsers);

        if (usersToMarkNotCurrent.Count > 0)
            await dbContext.BulkUpdateAsync(usersToMarkNotCurrent);

        await transaction.CommitAsync(cancellationToken);
    }
}
