namespace MonitoringBot.Domain.Events;

public interface IDomainEvent
{
    public DateTime TimeStamp { get; }
}