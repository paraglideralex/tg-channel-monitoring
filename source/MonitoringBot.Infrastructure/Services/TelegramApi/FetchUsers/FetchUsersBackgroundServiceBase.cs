using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;

using Serilog;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public abstract class FetchUsersBackgroundServiceBase
{
    protected volatile List<ChannelMember> currentUsers = [];
    protected readonly TimeSpan updateInterval;
    protected readonly TelegramApiServiceBase? telegramService;
    protected CancellationTokenSource cancellationTokenSource = new();

    public FetchUsersBackgroundServiceBase() { }
    public FetchUsersBackgroundServiceBase(TelegramChannelService telegramService, TimeSpan updateInterval)
    {
        this.telegramService = telegramService;
        this.updateInterval = updateInterval;
    }

    public abstract Task<bool> InitializeServiceAsync();

    public abstract void Start();

    public abstract void Stop();

    public abstract List<ChannelMember> GetSnapshot();
    public abstract TelegramServiceState GetState();

    public async Task LoginAsync() => await telegramService.LoginAsync();

    protected virtual async Task RunFetchLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var users = await telegramService.GetChannelMembersAsync();

                if (users is null)
                    Log.Error("Импорт подписчиков канала не выполнен, подписчики в этот раз не получены из телеграм-канала.");
                else
                {
                    currentUsers = users;
                    Log.Debug($"Успешно загружен новый снапшот из {users.Count} участников канала.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка в сервисе фонового мониторинга подписчиков при получении пользователей из Telegram.");
            }

            await Task.Delay(updateInterval, cancellationToken);
        }
    }

    public void Dispose() => telegramService?.Dispose();
}
