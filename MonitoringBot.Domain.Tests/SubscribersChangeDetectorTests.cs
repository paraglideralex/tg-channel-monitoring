using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Services;

using NUnit.Framework;

namespace MonitoringBot.Domain.Tests;

public class SubscribersChangeDetectorTests
{
    private SubscribersChangeDetector? subscribersChangeDetector;
    private ProcessorSubscriber? subscriber;
    private List<ChannelMember>? members;

    [SetUp]
    public void SetUp()
    {
        members =
        [
            new (55, "test1", false, "test1", "test1", "79999998888", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new (77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        ];

        subscribersChangeDetector = new SubscribersChangeDetector();
        subscriber = new ProcessorSubscriber();
        subscribersChangeDetector.SubscribersChanged += subscriber.OnEvent;
    }

    [Test]
    public async Task NothingHappens_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { members![0], members[1] };
        var fromApi = new List<ChannelMember>() { members[0], members[1] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs, Is.Null);
    }

    [Test]
    public async Task BasicAddition_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { members![0] };
        var fromApi = new List<ChannelMember>() { members[0], members[1] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.MemberDifference.Count(), Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.MemberDifference.First().Id, Is.EqualTo(77));
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { members![0], members[1] };
        var fromApi = members;

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(2));
        Assert.That(subscriber!.EventArgs?.MemberDifference.Count(), Is.EqualTo(2));
        Assert.That(subscriber!.EventArgs?.MemberDifference.First().Id, Is.EqualTo(88));
        Assert.That(subscriber!.EventArgs?.MemberDifference.Last().Id, Is.EqualTo(99));
    }

    [Test]
    public async Task BasicDeletion_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { members![0], members[1] };
        var fromApi = new List<ChannelMember>() { members![0] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(-1));
        Assert.That(subscriber!.EventArgs?.MemberDifference.Count(), Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.MemberDifference.First().Id, Is.EqualTo(77));
    }

    [Test]
    public async Task RangeDeletion_Success()
    {
        // Arrange
        var fromRepository = members!;
        var fromApi = new List<ChannelMember>() { members![2], members[3] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(-2));
        Assert.That(subscriber!.EventArgs?.MemberDifference.Count(), Is.EqualTo(2));
        Assert.That(subscriber!.EventArgs?.MemberDifference.First().Id, Is.EqualTo(55));
        Assert.That(subscriber!.EventArgs?.MemberDifference.Last().Id, Is.EqualTo(77));
    }
}

internal sealed class ProcessorSubscriber
{
    public SubscribersChangedEventArgs? EventArgs { get; private set; }
    public async Task OnEvent(object? sender, SubscribersChangedEventArgs e) => await Task.Run(() => EventArgs = e);
}