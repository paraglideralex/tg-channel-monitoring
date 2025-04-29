using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersChangeProcessor(
    UserRepository usersRepository)
{
    public async Task OnSubscribersChanged(object? sender, EntitiesChangedEventArgs<ChannelMember> e)
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
