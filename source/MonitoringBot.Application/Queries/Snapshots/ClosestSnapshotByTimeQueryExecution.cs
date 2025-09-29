using MonitoringBot.Application.Queries.Snapshots.Arguments;
using MonitoringBot.Domain.Projections;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Queries.Snapshots;

public sealed class ClosestSnapshotByTimeQueryExecution(
    SnapshotRepository snapshotRepository)
{
    public async Task<AggregateSnapshot?> ExecuteAsync(ClosestSnapshotByTimeQuery query, ServiceContext context)
    {
        return await snapshotRepository.GetClosestPreviousAsync(query.AggregateName, query.TimeStamp, context.CancellationToken);
    }
}
