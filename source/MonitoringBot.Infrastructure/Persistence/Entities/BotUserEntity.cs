namespace MonitoringBot.Infrastructure.Persistence.Entities;

public sealed record BotUserEntity
{
    public required long Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? UserName { get; set; }
    public bool? IsForum { get; set; }
    public string? Type { get; set; }
    public string? Title { get; set; }
}
