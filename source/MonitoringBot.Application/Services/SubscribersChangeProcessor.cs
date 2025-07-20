using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

using Serilog;

using System.ComponentModel.Design;

namespace MonitoringBot.Application.Services;

public class SubscribersChangeProcessor(
    AddOrUpdateSubscribersCommand addSubscribersCommand)
{
    public async Task OnSubscribersQuantityChanged(
        object? sender, 
        EntitiesCollectionChangedEventArgs<ChannelMember> e, 
        ServiceContext serviceContext)
    {

        var result = await addSubscribersCommand.ExecuteAsync(e, serviceContext);
        if (!result)
            Log.Error($"Ошибка обработки команды {nameof(OnSubscribersQuantityChanged)} сервиса {nameof(SubscribersChangeProcessor)}.");
    }
}
