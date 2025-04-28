using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure;
using MonitoringBot.Domain.Entities;

using Serilog;

namespace MonitoringBot.Application.Services;

public class SubscribersChangeProcessor(
    UserRepository usersRepository)
{
    //public SubscribersChangeProcessor(UserRepository usersRepository, SubscribersChangeDetector subscribersChangeDetector)
    //{
    //    this.usersRepository = usersRepository;
    //    CurrentStepDifferenceCount = 0;

    //}

    //private readonly UserRepository usersRepository;
    //private readonly SubscribersChangeDetector subscribersChangeDetector;
    //public long CurrentStepDifferenceCount { get; private set;}
    //public IEnumerable<ChannelMember> CurrentStepMemberDifference { get; private set; }

    public async Task OnSubscribersChanged(object? sender, SubscribersChangedEventArgs e)
    {
        if (e.DifferenceCount > 0)
        {
            await usersRepository.AddRange(e.MemberDifference);
            Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", e.MemberDifference.Select(u => u.NickName))}");
        }

        if (e.DifferenceCount < 0)
        {
            await usersRepository.DeleteRange(e.MemberDifference);
            Log.Information($"Пользователи удалены из базы: {string.Join(";", e.MemberDifference.Select(u => u.NickName))}");
        }
    }


    //private async Task OnSubscribersChanged(object? sender, SubscribersChangedEventArgs e)
    //{
    //    if (e.DifferenceCount > 0)
    //    {
    //        await _telegramBotService.SendMessageAsync($"Новые подписчики: {e.DifferenceCount}");
    //    }
    //    else if (e.DifferenceCount < 0)
    //    {
    //        await _telegramBotService.SendMessageAsync($"Отписались: {Math.Abs(e.DifferenceCount)}");
    //    }

    //    foreach (var member in e.MemberDifference)
    //    {
    //        await _telegramBotService.SendMessageAsync($"Изменение: {member.Name}");
    //    }
    //}

    //private async Task<long> CalculateCurrentStepDifferenceCount(IEnumerable<ChannelMember> usersCollectionFromApi) => 
    //    usersCollectionFromApi.Count() - await usersRepository.CountAsync();

    //private async Task<List<ChannelMember>> FindUsersDifference(IEnumerable<ChannelMember> usersCollectionFromApi, long currentStepDifferenceCount)
    //{
    //    var usersDifference = await usersRepository.Difference(usersCollectionFromApi);
    //    if(currentStepDifferenceCount > 0)
    //    {
    //        await usersRepository.AddRange(usersDifference);
    //        Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", usersDifference.Select(u => u.NickName))}");
    //    }

    //    if (currentStepDifferenceCount < 0)
    //    {
    //        await usersRepository.DeleteRange(usersDifference);
    //        Log.Information($"Пользователи удалены из базы: {string.Join(";", usersDifference.Select(u => u.NickName))}");
    //    }

    //    return usersDifference;
    //}

    //public async Task MonitoringStep(IEnumerable<ChannelMember> usersCollectionFromApi)
    //{
    //    var currentStepDifferenceCount = await CalculateCurrentStepDifferenceCount(usersCollectionFromApi);
    //    CurrentStepDifferenceCount = currentStepDifferenceCount;
    //    var usersDifference = await FindUsersDifference(usersCollectionFromApi, currentStepDifferenceCount);
    //    CurrentStepMemberDifference = usersDifference;
    //}
}
