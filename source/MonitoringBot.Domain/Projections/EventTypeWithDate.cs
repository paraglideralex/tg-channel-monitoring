namespace MonitoringBot.Domain.Projections;

public record struct EventTypeWithDate(DateTime TimeStamp, string? EventType);
