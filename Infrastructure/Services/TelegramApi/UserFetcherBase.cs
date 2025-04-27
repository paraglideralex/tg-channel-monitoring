namespace MonitoringBot.Infrastructure.Services.TelegramApi;

public abstract class UserFetcherBase: IDisposable
{
    public double LastSearchParicipantsDurationSeconds { get; protected set; } = 0;
    public abstract Task<List<ChannelMember>?> GetChannelMembersAsync();
    public abstract Task LoginAsync();

    public abstract void Dispose();
}
