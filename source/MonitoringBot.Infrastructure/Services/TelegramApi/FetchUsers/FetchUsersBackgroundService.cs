using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public class FetchUsersBackgroundService : FetchUsersBackgroundServiceBase
{
    public FetchUsersBackgroundService(TelegramChannelService telegramService, TelegramBotSettings settings) : base(telegramService, settings)
    {
    }

    public override async Task<bool> InitializeServiceAsync() => await telegramService.InitializeChannelAsync();

    public override void Start() => _ = Task.Run(() => RunFetchLoopAsync(cancellationTokenSource.Token));

    public override void Stop() => cancellationTokenSource.Cancel();

    public override List<ChannelMember> GetSnapshot() => currentUsers;
    public override TelegramServiceState GetState() => telegramService.State;
}
