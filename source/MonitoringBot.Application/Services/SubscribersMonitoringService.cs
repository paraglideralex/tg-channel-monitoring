using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersMonitoringService(FetchUsersBackgroundServiceBase fetchUsersBackgroundService,
    GetAllCurrentSubscribersIdentitiesQuery getAllCurrentSubscribersIdentitiesQuery,
    IEntitiesChangeDetector<long> entityChangeDetector,
    AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand,
    IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor,
    TelegramApiSettings telegramApiSettings,
    GetSubscribersByIdentitiesQuery getSubscribersByIdentitiesQuery,
    IEntitiesChangeEventsCreator<ChannelMember> entitiesChangeEventsCreator
    ) : IMonitoringService<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>
{
    public async Task ProcessMonitoringAsync()
    {
        var idsFromApi = await fetchUsersBackgroundService.GetExistingIdentitiesAsync();
        if (idsFromApi is null)
        {
            Log.Warning("Неполадки на стороне сервиса Tg API, подписчики не получены из телеграм-канала.");
            return;
        }

        var idsFromRepository = await getAllCurrentSubscribersIdentitiesQuery.ExecuteAsync();

        var changes = entityChangeDetector.FindChanges(idsFromApi, idsFromRepository, telegramApiSettings.ChannelReferenceLink);

        var joinedEntities = await fetchUsersBackgroundService.GetByIdsAsync(changes.EntitiesJoined);
        var leftEntities = await getSubscribersByIdentitiesQuery.ExecuteAsync(changes.EntitiesLeft);

        var eventsToBeProduced = entitiesChangeEventsCreator.ProduceEvents(joinedEntities, leftEntities, 
            telegramApiSettings.ChannelReferenceLink);

        var addResult = await addEventsCommand.ExecuteAsync(eventsToBeProduced);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();
    }
}
