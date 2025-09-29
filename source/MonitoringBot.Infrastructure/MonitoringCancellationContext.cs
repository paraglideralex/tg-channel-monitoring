namespace MonitoringBot.Infrastructure;

public class MonitoringCancellationContext
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private CancellationTokenSource cancellationTokenSource = new();

    public CancellationToken Token => cancellationTokenSource.Token;

    public async Task CancelAsync()
    {
        await semaphore.WaitAsync();
        try
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task RestartAsync()
    {
        await semaphore.WaitAsync();
        try
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }

            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await semaphore.WaitAsync();
        try
        {
            cancellationTokenSource.Dispose();
        }
        finally
        {
            semaphore.Release();
            semaphore.Dispose();
        }
    }
}
