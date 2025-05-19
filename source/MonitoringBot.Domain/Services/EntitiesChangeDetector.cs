using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;

namespace MonitoringBot.Domain.Services;

public class EntitiesChangeDetector<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>
    : IEntitiesChangeDetector<TEntity>
    where TEntity : class
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
        IEnumerable<TEntity> usersCollectionFromRepository)
    {
            var left = ExistInFirstAbsentInSecond(usersCollectionFromRepository, usersCollectionFromApi);
            var joined = ExistInFirstAbsentInSecond(usersCollectionFromApi, usersCollectionFromRepository);

            var events = new List<EntitiesChangedDomainEventBase<TEntity>>();

            events.AddRange(joined.Select(j => new TOnJoinedEventArgs
            {
                Entity = j
            }));

            events.AddRange(left.Select(l => new TOnLeftEventArgs
            {
                Entity = l
            }));

            return events;
    }
}
