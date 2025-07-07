using MonitoringBot.Domain.Projections;

namespace MonitoringBot.Domain.RepositoriesAbstarctions;

public abstract class SnapshotRepository
{
    public abstract Task Add(AggregateSnapshot snapshot);

    public abstract Task<AggregateSnapshot?> GetClosestPreviousAsync(string aggregateName, DateTime timeStamp);
}
