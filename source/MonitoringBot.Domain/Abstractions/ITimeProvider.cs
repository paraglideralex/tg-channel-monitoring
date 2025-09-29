namespace MonitoringBot.Domain.Abstractions;

public interface ITimeProvider
{
    public DateTime Now
    {
        get;
    }
    public DateTime UtcNow 
    {
        get;
    }
    public DateTimeOffset UtcDateTimeOffset { get; }
}
