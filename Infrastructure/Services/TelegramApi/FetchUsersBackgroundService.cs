using Serilog;

namespace MonitoringBot.Infrastructure.Services.TelegramApi;

public class FetchUsersBackgroundService: IDisposable
{
    private volatile List<ChannelMember> currentUsers = new();
    private readonly TimeSpan updateInterval;
    private readonly UserFetcherBase telegramService;
    private CancellationTokenSource cancellationTokenSource = new();

    public FetchUsersBackgroundService(TelegramChannelService telegramService, TimeSpan updateInterval)
    {
        this.telegramService = telegramService;
        this.updateInterval = updateInterval;
    }

    public void Start() => _ = Task.Run(() => RunFetchLoopAsync(cancellationTokenSource.Token));

    public void Stop() => cancellationTokenSource.Cancel();

    public List<ChannelMember> GetSnapshot() => currentUsers;
    public double GetFetchDuration() => telegramService.LastSearchParicipantsDurationSeconds;

    public async Task LoginAsync() => await telegramService.LoginAsync();

    private async Task RunFetchLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var users = await telegramService.GetChannelMembersAsync();

                if (users is null)
                {
                    Log.Error("Импорт подписчиков канала не выполнен, подписчики в этот раз не получены из телеграм-канала.");
                    return;
                }

                currentUsers = users;

                Log.Debug($"Успешно загружен новый снапшот из {users.Count} участников канала.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка в сервисе фонового мониторинга подписчиков при получении пользователей из Telegram.");
            }

            await Task.Delay(updateInterval, cancellationToken);
        }
    }

    public void Dispose()
    {
        telegramService?.Dispose();
    }
}
