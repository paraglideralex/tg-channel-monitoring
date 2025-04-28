using Serilog;

using System.Diagnostics;

namespace MonitoringBot.Infrastructure.Diagnostics;

public sealed class ExecutionTimer : IDisposable
{
    private readonly Stopwatch stopwatch;
    private readonly Action<TimeSpan> onCompleted;
    private readonly string operationName;

    public ExecutionTimer(string operationName, Action<TimeSpan>? onCompleted = null)
    {
        this.operationName = operationName;
        this.onCompleted = onCompleted ?? (_ => { });
        stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        Log.Information($"Операция '{operationName}' заняла {elapsed.TotalSeconds:0.###} сек.");
        onCompleted(elapsed);
    }
}
