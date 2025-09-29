using Serilog;

namespace MonitoringBot.Infrastructure.Services.FaultSafety;

public abstract class RetryServiceBase
{
    public abstract Task<bool> ExecuteRetryAsync(
        Func<CancellationToken, Task<bool>> action,
        string callerName,
        CancellationToken cancellationToken,
        int maxRetries = 5,
        int secondsInitialWait = 20,
        DelayIncreaseType delayIncreaseType = DelayIncreaseType.Linear);
}