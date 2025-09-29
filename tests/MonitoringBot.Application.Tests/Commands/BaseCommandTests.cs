using MonitoringBot.Application.Commands;
using MonitoringBot.Infrastructure;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests.Commands;

public class BaseCommandTests
{
    BaseCommandChild command;
    ServiceContext serviceContext;

    [SetUp]
    public void SetUp()
    {
        serviceContext = new ServiceContext { CancellationToken = new CancellationToken() };
        command = new BaseCommandChild();
    }

    [Test]
    public async Task NullArgument_ReturnsFalse()
    {
        var command = new BaseCommandChild();
        TestArguments? arguments = null;
        var result = await command.ExecuteAsync(arguments, serviceContext);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CorrectArgument_ReturnsTrue()
    {
        var command = new BaseCommandChild();
        TestArguments? arguments = new();
        var result = await command.ExecuteAsync(arguments, serviceContext);
        Assert.That(result, Is.True);
    }
}

internal class BaseCommandChild : BaseCommand<TestArguments>
{
    protected override Task ExecuteCoreAsync(TestArguments arguments, ServiceContext serviceContext)
    {
        return Task.CompletedTask;
    }
}

internal class TestArguments
{
}
