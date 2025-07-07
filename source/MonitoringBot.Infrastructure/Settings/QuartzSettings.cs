namespace MonitoringBot.Infrastructure.Settings;

public class QuartzSettings
{
    public string? SerializerType { get; init; }

    public string? InstanceName { get; init; }

    public string? SchedulerId { get; init; }

    public string? TablePrefix { get; init; }

    public string? ThreadCount { get; init; }
    public string? DebuggerEndpoint { get; init; }
}
