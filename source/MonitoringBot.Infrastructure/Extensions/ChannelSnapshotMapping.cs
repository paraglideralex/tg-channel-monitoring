using MonitoringBot.Domain.Projections;
using MonitoringBot.Infrastructure.Persistence.Entities.Snapshots;

namespace MonitoringBot.Infrastructure.Extensions;

public static class ChannelSnapshotMapping
{
    public static AggregateSnapshot ToDomain(this AggregateSnapshotEntity entity) => new()
    {
        Id = entity.Id,
        AggregateName = entity.AggregateName,
        LastEventSequenceNumberForTimeStamp = entity.LastEventSequenceNumberForTimeStamp,
        LastEventTimeStamp = entity.LastEventTimeStamp,
        LastProcessedEventId = entity.LastProcessedEventId,
        TotalEntities = entity.TotalEntities,
        TimeStamp = entity.TimeStamp
    };

    public static AggregateSnapshotEntity ToEntity(this AggregateSnapshot domain) => new()
    {
        Id = domain.Id,
        AggregateName = domain.AggregateName,
        LastEventSequenceNumberForTimeStamp = domain.LastEventSequenceNumberForTimeStamp,
        LastEventTimeStamp = domain.LastEventTimeStamp,
        LastProcessedEventId = domain.LastProcessedEventId,
        TotalEntities = domain.TotalEntities,
        TimeStamp = domain.TimeStamp
    };
}
