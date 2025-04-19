using Serilog;

using System.Diagnostics;

namespace MonitoringBot.Infrastructure.Diagnostics;

public sealed class ExecutionTimer : IDisposable
{
    private readonly Stopwatch stopwatch;
    private readonly string operationName;

    public ExecutionTimer(string operationName)
    {
        this.operationName = operationName;
        stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        stopwatch.Stop();
        Log.Information($"Операция '{operationName}' заняла {stopwatch.Elapsed.TotalSeconds:0.###} сек.");
    }
}
