using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersChangeProcessor(
    AddOrUpdateSubscribersCommand addSubscribersCommand)
{
    public async Task OnSubscribersQuantityChanged(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e)
    {

        var result = await addSubscribersCommand.ExecuteAsync(e);
        if (!result)
            Log.Error($"Ошибка обработки команды {nameof(OnSubscribersQuantityChanged)} сервиса {nameof(SubscribersChangeProcessor)}.");
    }
}
