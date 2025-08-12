using MonitoringBot.Infrastructure.Services.FaultSafety;

using NUnit.Framework;

namespace MonitoringBot.Infrastructure.Tests;

internal class RetryServiceTests
{
    private RetryService retryService;
    private CancellationToken cancellationToken;
    TestRetries testRetries;

    [SetUp]
    public void SetUp()
    {
        retryService = new RetryService();
        cancellationToken = new CancellationToken();
        testRetries = new TestRetries();
    }

    [Test]
    public async Task BasicSuccess()
    {
        bool result = await retryService.ExecuteRetryAsync(
            x => testRetries.DoStuff(cancellationToken),
            nameof(testRetries.DoStuff),
            cancellationToken,
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
        bool result = await retryService.ExecuteRetryAsync(
            x => testRetries.DoAnotherStuff(cancellationToken),
            nameof(testRetries.DoAnotherStuff),
            cancellationToken,
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

    public async Task<bool> DoStuff(CancellationToken cancellationToken)
    {
        Counter++;
        if (Counter == 3)
        {
            Flag = true;
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }

    public async Task<bool> DoAnotherStuff(CancellationToken cancellationToken)
    {
        Counter++;
        return await Task.FromResult(false);
    }
}