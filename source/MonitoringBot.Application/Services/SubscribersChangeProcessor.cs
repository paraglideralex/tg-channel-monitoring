using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersChangeProcessor(
    AddOrUpdateSubscribersCommand addSubscribersCommand,
    DeleteSubscribersCommand deleteSubscribersCommand)
{
    public async Task OnSubscribersJoined(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e)
    {

        var result = await addSubscribersCommand.ExecuteAsync(e);
        if (!result)
            Log.Error($"Ошибка обработки команды {nameof(OnSubscribersJoined)} сервиса {nameof(SubscribersChangeProcessor)}.");
    }

    public async Task OnSubscribersLeft(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e)
    {
        var result = await deleteSubscribersCommand.ExecuteAsync(e.EntitiesDifference);
        if (!result)
            Log.Error($"Ошибка обработки команды {nameof(OnSubscribersLeft)} сервиса {nameof(SubscribersChangeProcessor)}.");
    }
}
