namespace MonitoringBot.Application.Queries.Events.Arguments;

public sealed record GetLastPreviousEventQuery
{
    public string? LastAction { get; init; }
    public DateTime? LastTimeStamp { get; init; }
    public required long UserIdentity { get; init; }
}
