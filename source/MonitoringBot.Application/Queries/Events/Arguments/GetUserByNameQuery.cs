namespace MonitoringBot.Application.Queries.Events.Arguments;

public sealed record GetUserByNameQuery
{
    public string? UserNickName { get; set; }
}
