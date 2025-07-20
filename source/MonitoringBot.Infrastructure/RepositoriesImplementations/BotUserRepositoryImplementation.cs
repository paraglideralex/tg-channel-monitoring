using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

namespace MonitoringBot.Infrastructure.RepositoriesImplementations;
internal class BotUserRepositoryImplementation(
    IDbContextFactory<MonitoringBotDbContextBase> factory) : BotUserRepository
{
    public override async Task AddBotUserAsync(BotUser user, DateTime timeStamp, CancellationToken token)
    {
        await using var dbContext = await factory.CreateDbContextAsync(token);

        await dbContext.BotUsers.AddAsync(user.ToEntity(true, timeStamp));
    }

    public override async Task UpdateBotUserAsync(BotUser user, bool isCurrent, DateTime timeStamp, CancellationToken token)
    {
        await using var context = await factory.CreateDbContextAsync();

        var existing = await context.BotUsers.FindAsync(user.Id);

        if (existing is null)
            throw new InvalidOperationException($"User with Id: {user.Id} does not exist.");

        existing.UserName = user.UserName;
        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.Id = user.Id;
        existing.IsForum = user.IsForum;
        existing.IsCurrent = isCurrent;
        existing.TimeStamp = timeStamp;

        await context.SaveChangesAsync();
    }

    public override Task<IReadOnlyCollection<BotUser>> GetAllUsers(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public override Task<IReadOnlyCollection<BotUser>> GetCurrentUsers(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
