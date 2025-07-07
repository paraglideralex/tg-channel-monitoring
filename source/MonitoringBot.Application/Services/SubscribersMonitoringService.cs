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
    GetAllCurrentSubscribersQuery getAllCurrentSubscribersQuery,
    IEntitiesChangeDetector<ChannelMember> entityChangeDetector,
    AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand,
    IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor,
    TelegramApiSettings telegramApiSettings
    ) : IMonitoringService<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>
{
    public async Task ProcessMonitoringAsync()
    {
        var apiUsers = fetchUsersBackgroundService.GetSnapshot();
        if (apiUsers is null)
        {
            Log.Warning("Неполадки на стороне сервиса Tg API, подписчики не получены из телеграм-канала.");
            return;
        }

        var databaseUsers = await getAllCurrentSubscribersQuery.ExecuteAsync();

        var result = entityChangeDetector.ProduceEvents(apiUsers, databaseUsers, telegramApiSettings.ChannelReferenceLink);
        var addResult = await addEventsCommand.ExecuteAsync(result);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();
    }
}
