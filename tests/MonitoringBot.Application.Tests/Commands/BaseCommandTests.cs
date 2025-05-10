using MonitoringBot.Application.Commands;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests.Commands;

public class BaseCommandTests
{
    [Test]
    public async Task NullArgument_ReturnsFalse()
    {
        var command = new BaseCommandChild();
        TestArguments? arguments = null;
        var result = await command.ExecuteAsync(arguments);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CorrectArgument_ReturnsTrue()
    {
        var command = new BaseCommandChild();
        TestArguments? arguments = new();
        var result = await command.ExecuteAsync(arguments);
        Assert.That(result, Is.True);
    }
}

internal class BaseCommandChild : BaseCommand<TestArguments>
{
    protected override Task ExecuteCoreAsync(TestArguments arguments)
    {
        return Task.CompletedTask;
    }
}

internal class TestArguments
{
}
