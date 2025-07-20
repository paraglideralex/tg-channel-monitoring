using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

using System.Threading.Tasks;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public class FetchUsersBackgroundService : FetchUsersBackgroundServiceBase
{
    public FetchUsersBackgroundService(
        TelegramChannelService telegramService,
        TelegramBotSettings settings,
        ApiUsersRepository apiUsersRepository,
        ITimeProvider timeProvider) : base(telegramService, settings, apiUsersRepository, timeProvider)
    {
    }

    public override async Task<bool> InitializeServiceAsync() => await telegramService.InitializeChannelAsync(cancellationTokenSource.Token);

    public override void Start() => _ = Task.Run(() => RunFetchLoopAsync(cancellationTokenSource.Token));

    public override void Stop() => cancellationTokenSource.Cancel();

    public override async Task<List<ChannelMember>> GetSnapshot()
    {
        var current = await apiUsersRepository.GetCurrentUsersAsync();
        return current.Select(u => u.ToDomain()).ToList();
    }
    public override async Task<List<long>> GetExistingIdentitiesAsync()
    {
        return await apiUsersRepository.GetCurrentUsersIdentitiesAsync();
    }
    public override TelegramServiceState GetState() => telegramService.State;

    public override async Task<List<ChannelMember>> GetByIdsAsync(IReadOnlyCollection<long> idsCollection)
    {
        var users = await apiUsersRepository.GetUsersByIdsAsync(idsCollection);
        return users.Select(u => u.ToDomain()).ToList();
    }
}
