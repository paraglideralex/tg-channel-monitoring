namespace MonitoringBot.Application.Queries.Events.Arguments;

public sealed record GetEventsInPeriodQuery
{
    public DateTime FromNonInclusive { get; init; }
    public DateTime ToInclusive { get; init; }
}
