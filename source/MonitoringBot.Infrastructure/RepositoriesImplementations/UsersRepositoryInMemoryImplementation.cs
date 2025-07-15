using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

namespace MonitoringBot.Infrastructure.RepositoriesImplementations;

public class UsersRepositoryInMemoryImplementation(
    IDbContextFactory<MonitoringBotDbContextBase> factory) : UserRepository
{
    public override async Task AddAsync(ChannelMember user, string? lastAction)
    {
        await using var context = await factory.CreateDbContextAsync();

        var exists = await context.ChannelMembers
            .AnyAsync(u => u.Id == user.Id);
        if (!exists)
        {
            context.ChannelMembers.Add(user.ToEntity(lastAction));
            await context.SaveChangesAsync();
        }
    }

    public override async Task AddRangeAsync(IEnumerable<ChannelMember> users, string? lastAction)
    {
        await using var context = await factory.CreateDbContextAsync();

        var existingIds = await context.ChannelMembers
            .Select(u => u.Id)
            .ToListAsync();

        var newUsers = users.Where(u => !existingIds.Contains(u.Id)).ToList();

        if (newUsers.Count != 0)
        {
            var entities = newUsers.Select(u => u.ToEntity(lastAction));
            context.ChannelMembers.AddRange(entities);
            await context.SaveChangesAsync();
        }
    }

    public override async Task UpdateAsync(ChannelMember user, string? lastAction, DateTime timeStamp)
    {
        await using var context = await factory.CreateDbContextAsync();

        var existing = await context.ChannelMembers.FindAsync(user.Id);

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

        await context.SaveChangesAsync();
    }

    public override async Task UpdateRangeAsync(IEnumerable<ChannelMember> users, string? lastAction, DateTime timeStamp)
    {
        await using var context = await factory.CreateDbContextAsync();

        var userIds = users.Select(e => e.Id).ToList();

        var existingEntities = await context.ChannelMembers
            .Where(cm => userIds.Contains(cm.Id))
            .ToListAsync();

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

        await context.SaveChangesAsync();
    }

    public override async Task DeleteAsync(ChannelMember user)
    {
        await using var context = await factory.CreateDbContextAsync();

        var entity = await context.ChannelMembers.FindAsync(user.Id);
        if (entity != null)
        {
            context.ChannelMembers.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public override async Task DeleteRangeAsync(IEnumerable<ChannelMember> users)
    {
        await using var context = await factory.CreateDbContextAsync();

        var ids = users.Select(u => u.Id).ToList();
        var entities = await context.ChannelMembers.Where(u => ids.Contains(u.Id)).ToListAsync();

        if (entities.Any())
        {
            context.ChannelMembers.RemoveRange(entities);
            await context.SaveChangesAsync();
        }
    }

    public override async Task<long> CountAsync()
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.ChannelMembers
            .AsNoTracking()
            .LongCountAsync();
    }

    public override async Task<long> CountByLastActionAsync(string lastAction)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.ChannelMembers
            .AsNoTracking()
            .Where(x => x.LastAction == lastAction)
            .LongCountAsync();
    }

    public override async Task<List<long>> Keys()
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.ChannelMembers.Select(u => u.Id).ToListAsync();
    }

    public override async Task<ChannelMember?> FindById(long identity)
    {
        await using var context = await factory.CreateDbContextAsync();

        var entity = await context.ChannelMembers
            .AsNoTracking()
            .Where(u => u.Id == identity)
            .FirstOrDefaultAsync();

        return entity?.ToDomain();
    }

    public override async Task<List<ChannelMember>> FindByIdsAsync(IEnumerable<long> identities)
    {
        await using var context = await factory.CreateDbContextAsync();

        var entities = await context.ChannelMembers
            .AsNoTracking()
            .Where(u => identities.Contains(u.Id))
            .Select(u => u.ToDomain())
            .ToListAsync();

        return entities;
    }

    public override async Task<List<ChannelMember>> TakeLast(int count = 5)
    {
        await using var context = await factory.CreateDbContextAsync();

        var ordered = context.ChannelMembers
            .AsNoTracking()
            .OrderByDescending(m => m.TimeStamp);

        var taken = ordered.Count() >= count ? ordered.Take(count) : ordered;
        return await taken.Select(t => t.ToDomain()).ToListAsync();
    }

    public override async Task<List<ChannelMember>> TakeLastByAction(string lastAction, int count = 5)
    {
        await using var context = await factory.CreateDbContextAsync();

        var filtered = context.ChannelMembers
            .AsNoTracking()
            .Where(cm => cm.LastAction == lastAction);

        var ordered = filtered.OrderByDescending(m => m.TimeStamp);
        var taken = ordered.Count() >= count ? ordered.Take(count) : ordered;
        return await taken.Select(t => t.ToDomain()).ToListAsync();
    }

    public override async Task<List<ChannelMember>> All()
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.ChannelMembers
            .AsNoTracking()
            .Select(u => u.ToDomain()).ToListAsync();
    }

    public override async Task<List<ChannelMember>> AllSubscribed()
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.ChannelMembers
            .AsNoTracking()
            .Where(cm => cm.LastAction == nameof(SubscriberJoinedEvent))
            .Select(u => u.ToDomain()).ToListAsync();
    }

    public override async Task<List<long>> AllSubscribedIdentities()
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.ChannelMembers
            .AsNoTracking()
            .Where(cm => cm.LastAction == nameof(SubscriberJoinedEvent))
            .Select(u => u.Id).ToListAsync();
    }
}
