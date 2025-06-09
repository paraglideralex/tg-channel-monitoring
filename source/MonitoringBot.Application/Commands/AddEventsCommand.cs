using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.RepositoriesAbstarctions;

namespace MonitoringBot.Application.Commands;

public class AddEventsCommand<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>(
    EventRepository<TEntity> eventRepository) :
    BaseCommand<IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>>>

{
    protected override async Task ExecuteCoreAsync(
        IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> arguments)
    {
        await eventRepository.AddRangeAsync(arguments);
    }

}
