using Microsoft.IdentityModel.Tokens;

using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersMonitoringService(
    FetchUsersBackgroundServiceBase fetchUsersBackgroundService,
    GetAllCurrentSubscribersIdentitiesQuery getAllCurrentSubscribersIdentitiesQuery,
    IEntitiesChangeDetector<long> entityChangeDetector,
    AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand,
    IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor,
    TelegramApiSettings telegramApiSettings,
    GetSubscribersByIdentitiesQuery getSubscribersByIdentitiesQuery,
    IEntitiesChangeEventsCreator<ChannelMember> entitiesChangeEventsCreator
    ) : IMonitoringService<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>
{
    public async Task ProcessMonitoringAsync(CancellationToken cancellationToken)
    {
        var currentMonitoringContext = new ServiceContext { CancellationToken = cancellationToken, CorrelationId = Guid.NewGuid() };

        var idsFromApi = await fetchUsersBackgroundService.GetExistingIdentitiesAsync(cancellationToken);
        if (idsFromApi is null)
        {
            Log.Error("Telegram API service fetching users process terminated with errors," +
                "this time subscribers list was not updated and received correctly.");
            return;
        }

        var idsFromRepository = await getAllCurrentSubscribersIdentitiesQuery.ExecuteAsync(currentMonitoringContext);

        var changes = entityChangeDetector.FindChanges(idsFromApi, idsFromRepository, telegramApiSettings.ChannelReferenceLink);

        var joinedEntities = changes.EntitiesJoined.IsNullOrEmpty()
            ? []
            : await fetchUsersBackgroundService.GetByIdsAsync(changes.EntitiesJoined, cancellationToken);

        var leftEntities = changes.EntitiesLeft.IsNullOrEmpty()
            ? []
            : await getSubscribersByIdentitiesQuery.ExecuteAsync(changes.EntitiesLeft, currentMonitoringContext);

        var eventsToBeProduced = entitiesChangeEventsCreator.ProduceEvents(joinedEntities, leftEntities, 
            telegramApiSettings.ChannelReferenceLink);

        if (eventsToBeProduced.Count != 0)
        {
            var addResult = await addEventsCommand.ExecuteAsync(eventsToBeProduced, currentMonitoringContext);
            if (addResult is not true)
            {
                Log.Error($"Incoming events were not added to events store due to internal errors and " +
                    $"will not be processed during current monitoring step. Events: {string.Join(';', eventsToBeProduced)}.");

                return;
            }
        }

        await eventsMonitoringProcessor.ExecuteMonitoringAsync(currentMonitoringContext);
    }
}
