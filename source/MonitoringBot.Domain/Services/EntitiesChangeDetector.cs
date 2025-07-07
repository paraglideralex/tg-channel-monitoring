using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;

namespace MonitoringBot.Domain.Services;

public class EntitiesChangeDetector<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>(
    ITimeProvider timeProvider)
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
            int currentTimeSequenceNumber = 0;

            events.AddRange(joined.Select(j => new TOnJoinedEventArgs
            {
                Entity = j,
                EntityIdProjection = j.IdProjection(),
                EntityNameProjection = j.NameProjection(),
                ChannelName = aggregateName,
                TimeStamp = timeProvider.UtcNow,
                CurrentTimeSequenceNumber = currentTimeSequenceNumber++
            }));

            events.AddRange(left.Select(l => new TOnLeftEventArgs
            {
                Entity = l,
                EntityIdProjection = l.IdProjection(),
                EntityNameProjection = l.NameProjection(),
                ChannelName = aggregateName,
                TimeStamp = timeProvider.UtcNow,
                CurrentTimeSequenceNumber = currentTimeSequenceNumber++
            }));

        // TODO: для данного набора заполнять последовательно поле типа CurrentTimeStampSequenceNumber
        // искать по двойной сортировке - сначала таймштамп, потом CurrentTimeStampSequenceNumber
        // так мы не будем завязываться на инфраструктуру и соблюдём уникальность, потому что всё равно
        // это будет создаваться для одного конкретного агрегата

        return events;
    }
}
