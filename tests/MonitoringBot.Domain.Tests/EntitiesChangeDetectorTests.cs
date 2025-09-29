using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;

using Moq;

using NUnit.Framework;

using System.Text.Json;

namespace MonitoringBot.Domain.Tests;

public class EntitiesChangeDetectorTests
{
    private EntitiesChangeDetector<ChannelMember>? entitiesChangeDetector;
    private string baseType = "base-type";
    private List<ChannelMember>? allChannelMembers;

    [SetUp]
    public void SetUp()
    {
        allChannelMembers =
        [
            new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel",baseType),
            new(2, "2", false, "2", "2", "2", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel",baseType),
            new(3, "3", false, "3", "3", "3", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel",baseType),
            new(4, "4", false, "4", "4", "4", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel",baseType)
        ];

        entitiesChangeDetector = new EntitiesChangeDetector<ChannelMember>();
    }

    [Test]
    public void NothingHappens()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[0], allChannelMembers[1] };

        // Act
        var result = entitiesChangeDetector!.FindChanges(fromApi, fromRepository, "test");

        // Assert
        Assert.That(result!.EntitiesJoined, Is.Empty);
        Assert.That(result!.EntitiesLeft, Is.Empty);
    }

    [Test]
    public void FirstExists_SecondJoins()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[0], allChannelMembers[1] };

        // Act
        var result = entitiesChangeDetector!.FindChanges(fromApi, fromRepository, "test");

        // Assert
        Assert.That(result!.EntitiesJoined, Is.Not.Empty);
        Assert.That(result!.EntitiesJoined.Count, Is.EqualTo(1));
        Assert.That(result!.EntitiesJoined.First().Id, Is.EqualTo(allChannelMembers[1].Id));

        Assert.That(result!.EntitiesLeft, Is.Empty);
    }

    [Test]
    public void FirstSecondExist_ThirdForthJoined()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = allChannelMembers;

        // Act
        var result = entitiesChangeDetector!.FindChanges(fromApi, fromRepository, "test");

        // Assert
        Assert.That(result!.EntitiesJoined, Is.Not.Empty);
        Assert.That(result!.EntitiesJoined.Count, Is.EqualTo(2));
        Assert.That(result!.EntitiesJoined.First().Id, Is.EqualTo(allChannelMembers[2].Id));
        Assert.That(result!.EntitiesJoined.Last().Id, Is.EqualTo(allChannelMembers[3].Id));

        Assert.That(result!.EntitiesLeft, Is.Empty);
    }

    [Test]
    public void FirstSecondExists_FirstLeft()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers![0] };

        // Act
        var result = entitiesChangeDetector!.FindChanges(fromApi, fromRepository, "test");

        // Assert
        Assert.That(result!.EntitiesLeft, Is.Not.Empty);
        Assert.That(result!.EntitiesLeft.Count, Is.EqualTo(1));
        Assert.That(result!.EntitiesLeft.First().Id, Is.EqualTo(allChannelMembers[1].Id));

        Assert.That(result!.EntitiesJoined, Is.Empty);
    }

    [Test]
    public void AllExist_ThirdFourthLeave()
    {
        // Arrange
        var fromRepository = allChannelMembers!;
        var fromApi = new List<ChannelMember>() { allChannelMembers![2], allChannelMembers[3] };

        // Act
        var result = entitiesChangeDetector!.FindChanges(fromApi, fromRepository, "test");

        // Assert
        Assert.That(result!.EntitiesLeft, Is.Not.Empty);
        Assert.That(result!.EntitiesLeft.Count, Is.EqualTo(2));
        Assert.That(result!.EntitiesLeft.First().Id, Is.EqualTo(allChannelMembers[0].Id));
        Assert.That(result!.EntitiesLeft.Last().Id, Is.EqualTo(allChannelMembers[1].Id));

        Assert.That(result!.EntitiesJoined, Is.Empty);
    }

    [Test]
    public void SecondExists_FirstLeft_ThirdForthJoined()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[1], allChannelMembers[2], allChannelMembers[3] };

        // Act
        var result = entitiesChangeDetector!.FindChanges(fromApi, fromRepository, "test");

        // Assert
        Assert.That(result!.EntitiesLeft, Is.Not.Empty);
        Assert.That(result!.EntitiesLeft.Count, Is.EqualTo(1));
        Assert.That(result!.EntitiesLeft.First().Id, Is.EqualTo(allChannelMembers[0].Id));

        Assert.That(result!.EntitiesJoined, Is.Not.Empty);
        Assert.That(result!.EntitiesJoined.Count, Is.EqualTo(2));
        Assert.That(result!.EntitiesJoined.First().Id, Is.EqualTo(allChannelMembers[2].Id));
        Assert.That(result!.EntitiesJoined.Last().Id, Is.EqualTo(allChannelMembers[3].Id));
    }
}
