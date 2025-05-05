using MonitoringBot.Infrastructure.Extensions;

using NUnit.Framework;

namespace MonitoringBot.Infrastructure.Tests;

public class DurationFormattingTests
{
    [Test]
    public void AllElements()
    {
        var time1 = new DateTime(2024, 1, 1, 15, 25, 15);
        var time2 = new DateTime(2024, 1, 3, 13, 27, 33);
        var duration = time2 - time1;

        var result = duration.FormattedDuration();
        Assert.That(result, Is.EqualTo("1d, 22h, 2m, 18s"));
    }

    [Test]
    public void SecondsOnly()
    {
        var time1 = new DateTime(2024, 1, 1, 15, 25, 15);
        var time2 = new DateTime(2024, 1, 1, 15, 25, 53);
        var duration = time2 - time1;

        var result = duration.FormattedDuration();
        Assert.That(result, Is.EqualTo("38s"));
    }

}
