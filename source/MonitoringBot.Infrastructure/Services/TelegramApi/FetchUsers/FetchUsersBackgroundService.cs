using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

using System.Threading;
using System.Threading.Tasks;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public class FetchUsersBackgroundService : FetchUsersBackgroundServiceBase
{
    public FetchUsersBackgroundService(
        TelegramChannelService telegramService,
        TelegramBotSettings settings,
        ApiUsersRepository apiUsersRepository,
        ITimeProvider timeProvider,
        MonitoringCancellationContext cancellationContext) : base(telegramService, settings, apiUsersRepository, timeProvider, cancellationContext)
    {
    }

    public override async Task<bool> InitializeServiceAsync() => await telegramService.InitializeChannelAsync(cancellationContext.Token);

    public override void Start() => _ = Task.Run(() => RunFetchLoopAsync(cancellationContext.Token));

    public override async Task StopAsync() => await cancellationContext.CancelAsync();

    public override async Task<List<ChannelMember>> GetSnapshot(CancellationToken cancellationToken)
    {
        var current = await apiUsersRepository.GetCurrentUsersAsync(cancellationToken);
        return current.Select(u => u.ToDomain()).ToList();
    }
    public override async Task<List<long>> GetExistingIdentitiesAsync(CancellationToken cancellationToken)
    {
        return await apiUsersRepository.GetCurrentUsersIdentitiesAsync(cancellationToken);
    }
    public override TelegramServiceState GetState() => telegramService.State;

    public override async Task<List<ChannelMember>> GetByIdsAsync(IReadOnlyCollection<long> idsCollection, CancellationToken cancellationToken)
    {
        var users = await apiUsersRepository.GetUsersByIdsAsync(idsCollection, cancellationToken);
        return users.Select(u => u.ToDomain()).ToList();
    }
}
