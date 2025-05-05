namespace MonitoringBot.Infrastructure.Services.TelegramApi.Data;

public class TelegramServiceState
{
    public double LastSearchParicipantsDurationSeconds { get; set; }
    public DateTime LastSearchParticipantsTimeStamp { get; set; }
    public bool IsInitialized { get; set; }
}
