using MonitoringBot.Infrastructure.Extensions;

using NUnit.Framework;

namespace MonitoringBot.Infrastructure.Tests;

public class DateConversionTests
{
    [Test]
    public void LongConversionTest()
    {
        long input = 638874999980000000;
        var result = input.ToUtcDateTime();
        Assert.That(result, Is.EqualTo(new DateTime(2025, 7, 7, 15, 46, 38)));
    }
}
