using MonitoringBot.Application.Queries.Snapshots.Arguments;
using MonitoringBot.Domain.Projections;
using MonitoringBot.Domain.RepositoriesAbstarctions;

namespace MonitoringBot.Application.Queries.Snapshots;

public sealed class ClosestSnapshotByTimeQueryExecution(
    SnapshotRepository snapshotRepository)
{
    public async Task<AggregateSnapshot?> ExecuteAsync(ClosestSnapshotByTimeQuery query)
    {
        return await snapshotRepository.GetClosestPreviousAsync(query.AggregateName, query.TimeStamp);
    }
}
