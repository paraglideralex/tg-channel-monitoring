namespace MonitoringBot.Infrastructure.Extensions;

public static class LongExtensions
{
    public static DateTime ToUtcDateTime(this long ticks) => new(ticks, DateTimeKind.Utc);
}
