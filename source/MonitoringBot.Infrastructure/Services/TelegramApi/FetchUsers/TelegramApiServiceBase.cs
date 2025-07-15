using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure.Persistence.Entities;
using MonitoringBot.Infrastructure.Services.FaultSafety;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Settings;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public abstract class TelegramApiServiceBase : ConfigurableTelegramApiService, IDisposable
{
    protected RetryServiceBase retryService;
    protected ITimeProvider timeProvider;
    public TelegramApiServiceBase(
        TelegramConfig config,
        TelegramApiSettings settings,
        RetryServiceBase retryService,
        ITimeProvider timeProvider)
        : base(config, settings)
    {
        this.retryService = retryService;
        this.timeProvider = timeProvider;
    }

    public TelegramServiceState State { get; protected set; } = new()
    {
        IsInitialized = false,
        LastSearchParicipantsDurationSeconds = 0,
        LastSearchParticipantsTimeStamp = DateTime.UtcNow
    };

    public double LastSearchParicipantsDurationSeconds { get; protected set; } = 0;
    public abstract Task<bool> InitializeChannelAsync();
    public abstract Task<List<TLUser>?> GetChannelMembersAsync();

    public abstract void Dispose();
}
