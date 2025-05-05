using MonitoringBot.Infrastructure.Services.FaultSafety;

using NUnit.Framework;

namespace MonitoringBot.Infrastructure.Tests;

internal class RetryServiceTests
{
    private RetryService retryService = new();

    [Test]
    public async Task BasicSuccess()
    {
        TestRetries testRetries = new();

        bool result = await retryService.ExecuteRetryAsync(
            testRetries.DoStuff,
            nameof(testRetries.DoStuff),
            maxRetries: 3,
            secondsInitialWait: 1,
            DelayIncreaseType.Linear);

        Assert.That(result, Is.True);
        Assert.That(testRetries.Flag, Is.True);
        Assert.That(testRetries.Counter, Is.EqualTo(3));
    }

    [Test]
    public async Task BasicFail()
    {
        TestRetries testRetries = new();

        bool result = await retryService.ExecuteRetryAsync(
            testRetries.DoAnotherStuff,
            nameof(testRetries.DoAnotherStuff),
            maxRetries: 3,
            secondsInitialWait: 1,
            DelayIncreaseType.Exponential);

        Assert.That(result, Is.False);
        Assert.That(testRetries.Flag, Is.False);
        Assert.That(testRetries.Counter, Is.EqualTo(3));
    }
}

internal class TestRetries()
{
    public bool Flag = false;
    public int Counter = 0;

    public async Task<bool> DoStuff()
    {
        Counter++;
        if (Counter == 3)
        {
            Flag = true;
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }

    public async Task<bool> DoAnotherStuff()
    {
        Counter++;
        return await Task.FromResult(false);
    }
}