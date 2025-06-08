using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;

namespace MonitoringBot.Domain.Services;

public class EntitiesChangeDetector<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>
    : IEntitiesChangeDetector<TEntity>
    where TEntity : ISearchableEntity
    where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
    where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
{
    private bool HasValidItems(List<TEntity> list) =>
        list is not null && list.Count > 0;

    private List<TEntity> ExistInFirstAbsentInSecond(
        IEnumerable<TEntity> firstCollection,
        IEnumerable<TEntity> secondCollection) =>

        firstCollection.Except(secondCollection).ToList();

    public IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> ProduceEvents(
        IEnumerable<TEntity> usersCollectionFromApi,
        IEnumerable<TEntity> usersCollectionFromRepository,
        string aggregateName)
    {
            var left = ExistInFirstAbsentInSecond(usersCollectionFromRepository, usersCollectionFromApi);
            var joined = ExistInFirstAbsentInSecond(usersCollectionFromApi, usersCollectionFromRepository);

            var events = new List<EntitiesChangedDomainEventBase<TEntity>>();

            events.AddRange(joined.Select(j => new TOnJoinedEventArgs
            {
                Entity = j,
                EntityIdProjection = j.IdProjection(),
                EntityNameProjection = j.NameProjection(),
                ChannelName = aggregateName
            }));

            events.AddRange(left.Select(l => new TOnLeftEventArgs
            {
                Entity = l,
                EntityIdProjection = l.IdProjection(),
                EntityNameProjection = l.NameProjection(),
                ChannelName = aggregateName
            }));

            return events;
    }
}
