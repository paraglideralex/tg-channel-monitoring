namespace MonitoringBot.Infrastructure.Settings;

public class DatabaseSettings
{
    public string BaseConnectionString { get; set; } = string.Empty;
    public string ReadModelConnectionString { get; set; } = string.Empty;
    public string EventStorageConnectionString { get; set; } = string.Empty;
}
