using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersChangeProcessor(
    UserRepository usersRepository)
{
    public async Task OnSubscribersChanged(object? sender, SubscribersChangedEventArgs e)
    {
        if(e.DifferenceCount > 0)
        {
            await usersRepository.AddRange(e.MemberDifference);
            Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", e.MemberDifference.Select(u => u.NickName))}");
        }

        if(e.DifferenceCount < 0)
        {
            await usersRepository.DeleteRange(e.MemberDifference);
            Log.Information($"Пользователи удалены из базы: {string.Join(";", e.MemberDifference.Select(u => u.NickName))}");
        }
    }
}
