using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

using NetTopologySuite.Operation.Distance3D;

namespace MonitoringBot.Infrastructure.RepositoriesImplementations;

public class UsersRepositoryImplementation(
    IDbContextFactory<MonitoringBotDbContextBase> factory) : UserRepository
{
    public override async Task AddRangeAsync(IEnumerable<ChannelMember> users, string? lastAction, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var existingIds = await context.ChannelMembers
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var newUsers = users.Where(u => !existingIds.Contains(u.Id)).ToList();

        if (newUsers.Count != 0)
        {
            var entities = newUsers.Select(u => u.ToEntity(lastAction));
            context.ChannelMembers.AddRange(entities);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task UpdateAsync(ChannelMember user, string? lastAction, DateTime timeStamp, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var existing = await context.ChannelMembers.FindAsync(user.Id, cancellationToken);

        if(existing is null)
            throw new InvalidOperationException($"User with Id: {user.Id} does not exist.");

        existing.NickName = user.NickName;
        existing.IsBot = user.IsBot;
        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.Phone = user.Phone;
        existing.ChannelReference = user.ChannelReference;
        existing.LastAction = lastAction;
        existing.TimeStamp = timeStamp;

        await context.SaveChangesAsync(cancellationToken);
    }

    public override async Task UpdateRangeAsync(IEnumerable<ChannelMember> users, string? lastAction, DateTime timeStamp, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var userIds = users.Select(e => e.Id).ToList();

        var existingEntities = await context.ChannelMembers
            .Where(cm => userIds.Contains(cm.Id))
            .ToListAsync(cancellationToken);

        foreach (var existing in existingEntities)
        {
            var updated = users.First(e => e.Id == existing.Id);

            existing.NickName = updated.NickName;
            existing.IsBot = updated.IsBot;
            existing.FirstName = updated.FirstName;
            existing.LastName = updated.LastName;
            existing.Phone = updated.Phone;
            existing.ChannelReference = updated.ChannelReference;
            existing.LastAction = lastAction;
            existing.TimeStamp = timeStamp;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public override async Task<long> CountByLastActionAsync(string lastAction, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        return await context.ChannelMembers
            .AsNoTracking()
            .Where(x => x.LastAction == lastAction)
            .LongCountAsync(cancellationToken);
    }

    public override async Task<ChannelMember?> FindByIdAsync(long identity, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var entity = await context.ChannelMembers
            .AsNoTracking()
            .Where(u => u.Id == identity)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDomain();
    }

    public override async Task<List<ChannelMember>> FindByIdsAsync(IEnumerable<long> identities, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var entities = await context.ChannelMembers
            .AsNoTracking()
            .Where(u => identities.Contains(u.Id))
            .Select(u => u.ToDomain())
            .ToListAsync(cancellationToken);

        return entities;
    }

    public override async Task<List<ChannelMember>> TakeLast(int count = 5, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var ordered = context.ChannelMembers
            .AsNoTracking()
            .OrderByDescending(m => m.TimeStamp);

        var taken = ordered.Count() >= count ? ordered.Take(count) : ordered;
        return await taken.Select(t => t.ToDomain()).ToListAsync(cancellationToken);
    }

    public override async Task<List<ChannelMember>> TakeLastByActionAsync(string lastAction, CancellationToken cancellationToken, int count = 5)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var filtered = context.ChannelMembers
            .AsNoTracking()
            .Where(cm => cm.LastAction == lastAction);

        var ordered = filtered.OrderByDescending(m => m.TimeStamp);
        var taken = ordered.Count() >= count ? ordered.Take(count) : ordered;
        return await taken.Select(t => t.ToDomain()).ToListAsync(cancellationToken);
    }

    public override async Task<List<ChannelMember>> AllSubscribed()
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.ChannelMembers
            .AsNoTracking()
            .Where(cm => cm.LastAction == nameof(SubscriberJoinedEvent))
            .Select(u => u.ToDomain()).ToListAsync();
    }

    public override async Task<List<long>> AllSubscribedIdentitiesAsync(CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        return await context.ChannelMembers
            .AsNoTracking()
            .Where(cm => cm.LastAction == nameof(SubscriberJoinedEvent))
            .Select(u => u.Id).ToListAsync(cancellationToken);
    }

    public override async Task<ChannelMember?> FindByNickNameAsync(string nickName, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        
        var user = await context.ChannelMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(cm => cm.NickName == nickName, cancellationToken);

        return user?.ToDomain();
    }
}
