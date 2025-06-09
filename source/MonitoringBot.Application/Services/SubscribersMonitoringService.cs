using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersMonitoringService(FetchUsersBackgroundService fetchUsersBackgroundService,
    GetAllCurrentSubscribersQuery getAllCurrentSubscribersQuery,
    EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> entityChangeDetector,
    AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand,
    IEventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor,
    string channelReference
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

        var result = entityChangeDetector.ProduceEvents(apiUsers, databaseUsers, channelReference);
        var addResult = await addEventsCommand.ExecuteAsync(result);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();
    }
}
