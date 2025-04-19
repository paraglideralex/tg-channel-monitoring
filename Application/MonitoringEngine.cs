using MonitoringBot.Infrastructure;

using Serilog;

namespace MonitoringBot.Application;

public class MonitoringEngine
{
    public MonitoringEngine(UserRepository usersRepository)
    {
        this.usersRepository = usersRepository;
        CurrentStepDifferenceCount = 0;
        CurrentStepMemberDifference = [];
    }

    private readonly UserRepository usersRepository;
    public long CurrentStepDifferenceCount { get; private set;}
    public IEnumerable<ChannelMember> CurrentStepMemberDifference { get; private set; }

    private async Task<long> CalculateCurrentStepDifferenceCount(IEnumerable<ChannelMember> usersCollectionFromApi) => 
        usersCollectionFromApi.Count() - await usersRepository.CountAsync();

    private async Task<List<ChannelMember>> FindUsersDifference(IEnumerable<ChannelMember> usersCollectionFromApi, long currentStepDifferenceCount)
    {
        var usersDifference = await usersRepository.Difference(usersCollectionFromApi);
        if(currentStepDifferenceCount > 0)
        {
            await usersRepository.AddRange(usersDifference);
            Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", usersDifference.Select(u => u.NickName))}");
        }
            
        if (currentStepDifferenceCount < 0)
        {
            await usersRepository.DeleteRange(usersDifference);
            Log.Information($"Пользователи удалены из базы: {string.Join(";", usersDifference.Select(u => u.NickName))}");
        }
            
        return usersDifference;
    }

    public async Task MonitoringStep(IEnumerable<ChannelMember> usersCollectionFromApi)
    {
        var currentStepDifferenceCount = await CalculateCurrentStepDifferenceCount(usersCollectionFromApi);
        CurrentStepDifferenceCount = currentStepDifferenceCount;
        var usersDifference = await FindUsersDifference(usersCollectionFromApi, currentStepDifferenceCount);
        CurrentStepMemberDifference = usersDifference;
    }
}
