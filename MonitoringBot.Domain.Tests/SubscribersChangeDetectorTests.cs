using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Services;

using NUnit.Framework;

namespace MonitoringBot.Domain.Tests;

public class SubscribersChangeDetectorTests
{
    private EntitiesChangeDetector<ChannelMember>? subscribersChangeDetector;
    private ProcessorSubscriber? subscriber;
    private List<ChannelMember>? allChannelMembers;

    [SetUp]
    public void SetUp()
    {
        allChannelMembers =
        [
            new(55, "test1", false, "test1", "test1", "79999998888", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        ];

        subscribersChangeDetector = new EntitiesChangeDetector<ChannelMember>();
        subscriber = new ProcessorSubscriber();
        subscribersChangeDetector.EntitiesChanged += subscriber.OnEvent;
    }

    [Test]
    public async Task NothingHappens_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[0], allChannelMembers[1] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs, Is.Null);
    }

    [Test]
    public async Task BasicAddition_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[0], allChannelMembers[1] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.Count(), Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.First().Id, Is.EqualTo(77));
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = allChannelMembers;

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(2));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.Count(), Is.EqualTo(2));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.First().Id, Is.EqualTo(88));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.Last().Id, Is.EqualTo(99));
    }

    [Test]
    public async Task BasicDeletion_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers![0] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(-1));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.Count(), Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.First().Id, Is.EqualTo(77));
    }

    [Test]
    public async Task RangeDeletion_Success()
    {
        // Arrange
        var fromRepository = allChannelMembers!;
        var fromApi = new List<ChannelMember>() { allChannelMembers![2], allChannelMembers[3] };

        // Act
        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        // Assert
        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(-2));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.Count(), Is.EqualTo(2));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.First().Id, Is.EqualTo(55));
        Assert.That(subscriber!.EventArgs?.EntitiesDifference.Last().Id, Is.EqualTo(77));
    }
}

internal sealed class ProcessorSubscriber
{
    public EntitiesChangedEventArgs<ChannelMember>? EventArgs { get; private set; }
    public async Task OnEvent(object? sender, EntitiesChangedEventArgs<ChannelMember> e) => 
        await Task.Run(() => EventArgs = e);
}