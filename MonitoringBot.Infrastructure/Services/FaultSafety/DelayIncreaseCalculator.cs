namespace MonitoringBot.Infrastructure.Services.FaultSafety;

public class DelayIncreaseCalculator
{
    public int Calculate(int initial, int sequenceNumber, DelayIncreaseType delayIncreaseType) =>
    delayIncreaseType switch
    {
        DelayIncreaseType.Constant => initial,
        DelayIncreaseType.Linear => Linear(initial, sequenceNumber),
        DelayIncreaseType.Exponential => Exponent(initial, sequenceNumber),
        _ => initial
    };

    private int Linear(int initial, int sequenceNumber) => sequenceNumber * initial;

    private int Exponent(int initial, int sequenceNumber) => Convert.ToInt32(initial * Math.Pow(2, sequenceNumber - 1));
}
