using Microsoft.EntityFrameworkCore;

using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Persistence;

public class UsersRepositoryInMemoryImplementation : UserRepository
{
    private readonly MonitoringBotDbContextBase context;

    public UsersRepositoryInMemoryImplementation(MonitoringBotDbContextBase context)
    {
        this.context = context;
    }

    public override async Task Add(ChannelMember user)
    {
        var exists = await context.ChannelMembers.AnyAsync(u => u.Id == user.Id);
        if (!exists)
        {
            context.ChannelMembers.Add(user.ToEntity());
            await context.SaveChangesAsync();
        }
    }

    public override async Task AddRange(IEnumerable<ChannelMember> users)
    {
        var existingIds = await context.ChannelMembers.Select(u => u.Id).ToListAsync();
        var newUsers = users.Where(u => !existingIds.Contains(u.Id)).ToList();

        if (newUsers.Count != 0)
        {
            var entities = newUsers.Select(u => u.ToEntity());
            context.ChannelMembers.AddRange(entities);
            await context.SaveChangesAsync();
        }
    }

    public override async Task Delete(ChannelMember user)
    {
        var entity = await context.ChannelMembers.FindAsync(user.Id);
        if (entity != null)
        {
            context.ChannelMembers.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public override async Task DeleteRange(IEnumerable<ChannelMember> users)
    {
        var ids = users.Select(u => u.Id).ToList();
        var entities = await context.ChannelMembers.Where(u => ids.Contains(u.Id)).ToListAsync();

        if (entities.Any())
        {
            context.ChannelMembers.RemoveRange(entities);
            await context.SaveChangesAsync();
        }
    }

    public override async Task<long> Count()
    {
        return await context.ChannelMembers.LongCountAsync();
    }

    public override async Task<List<long>> Keys()
    {
        return await context.ChannelMembers.Select(u => u.Id).ToListAsync();
    }

    public override async Task<List<ChannelMember>> FindByIds(IEnumerable<long> identities)
    {
        var entities = await context.ChannelMembers
            .Where(u => identities.Contains(u.Id))
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public override Task<List<ChannelMember>> FindByIds(IEnumerable<ChannelMember> usersCollection, IEnumerable<long> identities)
    {
        var filtered = usersCollection.Where(u => identities.Contains(u.Id)).ToList();
        return Task.FromResult(filtered);
    }

    public override async Task<List<ChannelMember>> Difference(IEnumerable<ChannelMember> users)
    {
        var currentUsers = context.ChannelMembers.AsEnumerable().Select(u => u.ToDomain()).ToList();
        var difference = currentUsers.SymmetricDifference(users);
        return difference.ToList();
    }

    public override async Task<List<ChannelMember>> TakeLast(int count = 5)
    {
        var ordered = context.ChannelMembers.OrderByDescending(m => m.JoinedAt);
        var taken = ordered.Count() >= count ? ordered.Take(count) : ordered;
        return await taken.Select(t => t.ToDomain()).ToListAsync();
    }
}
