using MonitoringBot.Domain.Abstractions;

namespace MonitoringBot.Infrastructure.Services;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcDateTimeOffset => DateTimeOffset.UtcNow;
}
