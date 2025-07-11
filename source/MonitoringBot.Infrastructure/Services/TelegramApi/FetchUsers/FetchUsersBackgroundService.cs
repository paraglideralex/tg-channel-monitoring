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

    // TODO: The target scenario is to fetch them from the Telegram API snapshot database via the repository.
    // Currently, users are stored in memory, which makes me feel painful but not painful enough to improve it right now o_O
    public override List<ChannelMember> GetSnapshot() => currentUsers;
    public override List<long> GetExistingIdentities() => currentUsers.Select(u => u.Id).ToList();
    public override TelegramServiceState GetState() => telegramService.State;
}
