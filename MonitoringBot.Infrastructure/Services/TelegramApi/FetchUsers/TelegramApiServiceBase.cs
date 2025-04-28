using MonitoringBot.Domain.Entities;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public abstract class TelegramApiServiceBase : ConfigurableTelegramApiService, IDisposable
{
    public TelegramApiServiceBase(TelegramConfig config, string? channelReference) : base(config, channelReference) { }
    public double LastSearchParicipantsDurationSeconds { get; protected set; } = 0;
    public abstract Task<List<ChannelMember>?> GetChannelMembersAsync();

    public abstract void Dispose();
}
