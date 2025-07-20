using Microsoft.EntityFrameworkCore;

using MonitoringBot.Domain.Projections;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

namespace MonitoringBot.Infrastructure.RepositoriesImplementations;

public sealed class SnapshotRepositoryImplementation(IDbContextFactory<MonitoringBotDbContextBase> factory) : SnapshotRepository
{
    public override async Task Add(AggregateSnapshot snapshot, CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        dbContext.Snapshots.Add(snapshot.ToEntity());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public override async Task<AggregateSnapshot?> GetClosestPreviousAsync(string aggregateName, DateTime timeStamp, CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync();

        var closest = await dbContext.Snapshots
            .Where(s => s.AggregateName == aggregateName && s.LastEventTimeStamp <= timeStamp)
            .OrderByDescending(s => s.LastEventTimeStamp)
            .FirstOrDefaultAsync(cancellationToken);

        return closest?.ToDomain();
    }
}
