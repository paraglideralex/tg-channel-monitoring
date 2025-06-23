namespace MonitoringBot.Domain.Abstractions;

public interface ITimeProvider
{
    public DateTime Now
    {
        get;
    }
}
