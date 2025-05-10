using MonitoringBot.Application.Commands;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersChangeProcessor(
    AddSubscribersCommand addSubscribersCommand,
    DeleteSubscribersCommand deleteSubscribersCommand)
{
    public async Task OnSubscribersJoined(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e)
    {

        var result = await addSubscribersCommand.ExecuteAsync(e.EntitiesDifference);

    }

    public async Task OnSubscribersLeft(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e)
    {
        if (e.DifferenceCount != 0)
        {
            await usersRepository.DeleteRange(e.EntitiesDifference);
            Log.Information($"Пользователи удалены из базы: {string.Join(";", e.EntitiesDifference.Select(u => u.NickName))}");
        }
    }

    public async Task OnSubscribersChanged(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e)
    {
        if(e.DifferenceCount > 0)
        {
            await usersRepository.AddRange(e.EntitiesDifference);
            Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", e.EntitiesDifference.Select(u => u.NickName))}");
        }

        if(e.DifferenceCount < 0)
        {
            await usersRepository.DeleteRange(e.EntitiesDifference);
            Log.Information($"Пользователи удалены из базы: {string.Join(";", e.EntitiesDifference.Select(u => u.NickName))}");
        }
    }
}
