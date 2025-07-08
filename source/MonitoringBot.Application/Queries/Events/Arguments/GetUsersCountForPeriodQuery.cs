namespace MonitoringBot.Application.Queries.Events.Arguments;

public sealed record GetUsersCountForPeriodQuery
{
    public required string AggregateName { get; init; }
    public DateTime FromNonInclusive { get; init; }
    public DateTime ToInclusive { get; init; }
    public TimeSpan Step { get; init; }
}
