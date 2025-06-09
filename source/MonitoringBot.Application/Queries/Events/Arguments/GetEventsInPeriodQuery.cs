namespace MonitoringBot.Application.Queries.Events.Arguments;

public sealed record GetEventsInPeriodQuery
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
}
