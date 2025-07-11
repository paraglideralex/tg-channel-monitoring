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
    TelegramApiSettings telegramApiSettings
    ) : IMonitoringService<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>
{
    public async Task ProcessMonitoringAsync()
    {
        var currentIdentities = fetchUsersBackgroundService.GetExistingIdentities();
        if (currentIdentities is null)
        {
            Log.Warning("Неполадки на стороне сервиса Tg API, подписчики не получены из телеграм-канала.");
            return;
        }

        var repositoryIdentities = await getAllCurrentSubscribersIdentitiesQuery.ExecuteAsync();

        var result = entityChangeDetector.FindChanges(currentIdentities, repositoryIdentities, telegramApiSettings.ChannelReferenceLink);

        var joinedEntities = 

            // TODO: тут будет работать уже процессор

        var addResult = await addEventsCommand.ExecuteAsync(result);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();
    }
}
