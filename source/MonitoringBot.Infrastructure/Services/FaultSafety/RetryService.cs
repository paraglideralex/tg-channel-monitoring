using Serilog;

namespace MonitoringBot.Infrastructure.Services.FaultSafety;

public class RetryService : RetryServiceBase
{
    private readonly DelayIncreaseCalculator delayIncreaseCalculator = new();

    public override async Task<bool> ExecuteRetryAsync(
        Func<Task<bool>> action,
        string callerName,
        int maxRetries = 5,
        int secondsInitialWait = 20,
        DelayIncreaseType delayIncreaseType = DelayIncreaseType.Linear)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            bool result = await action();
            if (result)
            {
                Log.Information($"Задача '{callerName}' успешно выполнена.");
                return true;
            }
            else
            {
                var delay = delayIncreaseCalculator.Calculate(secondsInitialWait, i, delayIncreaseType);
                Log.Warning($"Ожидание {delay} секунд и попытка #{i} выполнить задачу '{callerName}'");
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }
        Log.Error($"Исчерпано количество попыток выполнить задачу '{callerName}'.");
        return false;
    }
}
