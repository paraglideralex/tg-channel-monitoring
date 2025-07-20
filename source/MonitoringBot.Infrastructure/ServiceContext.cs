namespace MonitoringBot.Infrastructure;

public class ServiceContext
{
    public CancellationToken CancellationToken { get; init; }
    public Guid CorrelationId { get; init; } = Guid.Empty;
}
