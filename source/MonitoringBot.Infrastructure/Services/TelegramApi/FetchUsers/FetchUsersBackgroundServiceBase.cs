using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public abstract class FetchUsersBackgroundServiceBase
{
    //protected volatile List<ChannelMember> currentUsers = [];
    protected readonly TimeSpan updateInterval;
    protected readonly TelegramApiServiceBase? telegramService;
    protected CancellationTokenSource cancellationTokenSource = new();
    protected readonly ApiUsersRepository apiUsersRepository;
    protected readonly ITimeProvider timeProvider;

    public FetchUsersBackgroundServiceBase(
        TelegramChannelService telegramService,
        TelegramBotSettings settings,
        ApiUsersRepository apiUsersRepository,
        ITimeProvider timeProvider)
    {
        this.telegramService = telegramService;
        updateInterval = TimeSpan.FromSeconds(settings.CheckPeriodSeconds);
        this.apiUsersRepository = apiUsersRepository;
        this.timeProvider = timeProvider;
    }

    public abstract Task<bool> InitializeServiceAsync();

    public abstract void Start();

    public abstract void Stop();
    public abstract Task<List<long>> GetExistingIdentitiesAsync();
    public abstract Task<List<ChannelMember>> GetSnapshot();
    public abstract Task<List<ChannelMember>> GetByIdsAsync(IReadOnlyCollection<long> idsCollection);
    public abstract TelegramServiceState GetState();

    public async Task LoginAsync() => await telegramService.LoginAsync();

    protected virtual async Task RunFetchLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var users = await telegramService.GetChannelMembersAsync(cancellationToken);

                if (users is null)
                    Log.Error("Импорт подписчиков канала не выполнен, подписчики в этот раз не получены из телеграм-канала.");
                else
                {
                    await apiUsersRepository.RefreshUsersAsync(users, timeProvider.UtcNow);
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
