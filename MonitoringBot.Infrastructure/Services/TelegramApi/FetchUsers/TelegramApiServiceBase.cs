using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure.Services.FaultSafety;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public abstract class TelegramApiServiceBase : ConfigurableTelegramApiService, IDisposable
{
    protected RetryServiceBase retryService;
    public TelegramApiServiceBase(TelegramConfig config, string? channelReference, RetryServiceBase retryService)
        : base(config, channelReference)
    {
        this.retryService = retryService;
    }

    public TelegramServiceState State { get; protected set; } = new()
    {
        IsInitialized = false,
        LastSearchParicipantsDurationSeconds = 0,
        LastSearchParticipantsTimeStamp = DateTime.Now
    };

    public double LastSearchParicipantsDurationSeconds { get; protected set; } = 0;
    public abstract Task<bool> InitializeChannelAsync();
    public abstract Task<List<ChannelMember>?> GetChannelMembersAsync();

    public abstract void Dispose();
}
