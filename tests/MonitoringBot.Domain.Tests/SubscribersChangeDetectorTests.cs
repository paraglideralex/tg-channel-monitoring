using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;


using NUnit.Framework;

using System.Text.Json;

namespace MonitoringBot.Domain.Tests;

public class SubscribersChangeDetectorTests
{
    private EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>? subscribersChangeDetector;
    //private ProcessorSubscriber? subscriber;
    private List<ChannelMember>? allChannelMembers;

    [SetUp]
    public void SetUp()
    {
        allChannelMembers =
        [
            new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel"),
            new(2, "2", false, "2", "2", "2", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel"),
            new(3, "3", false, "3", "3", "3", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel"),
            new(4, "4", false, "4", "4", "4", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel")
        ];

        subscribersChangeDetector = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        //subscriber = new ProcessorSubscriber();

        //subscribersChangeDetector.EntitiesJoined += subscriber.OnJoined;
        //subscribersChangeDetector.EntitiesLeft += subscriber.OnLeft;
    }

    [Test]
    public async Task NothingHappens()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[0], allChannelMembers[1] };

        // Act
        var result = subscribersChangeDetector!.ProduceEvents(fromApi, fromRepository, "test");

        // Assert
        //Assert.That(subscriber!.Joined, Is.Null);
        //Assert.That(subscriber!.Left, Is.Null);
    }

    [Test]
    public async Task FirstExists_SecondJoins()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[0], allChannelMembers[1] };

        // Act
        var result = subscribersChangeDetector!.ProduceEvents(fromApi, fromRepository, "test");


        var dese = JsonSerializer.Serialize(result.First());

        //var kk = 

        // Assert
        //Assert.That(subscriber!.Joined, Is.Not.Null);
        //Assert.That(subscriber!.Joined!.DifferenceCount, Is.EqualTo(1));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.Count(), Is.EqualTo(1));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.First().Id, Is.EqualTo(allChannelMembers[1].Id));

        //Assert.That(subscriber!.Left, Is.Null);
    }

    [Test]
    public async Task FirstSecondExist_ThirdForthJoined()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = allChannelMembers;

        // Act
        subscribersChangeDetector!.ProduceEvents(fromApi, fromRepository, "test");

        // Assert
        //Assert.That(subscriber!.Joined, Is.Not.Null);
        //Assert.That(subscriber!.Joined!.DifferenceCount, Is.EqualTo(2));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.Count(), Is.EqualTo(2));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.First().Id, Is.EqualTo(allChannelMembers[2].Id));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.Last().Id, Is.EqualTo(allChannelMembers[3].Id));

        //Assert.That(subscriber!.Left, Is.Null);
    }

    [Test]
    public async Task FirstSecondExists_FirstLeft()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers![0] };

        // Act
        subscribersChangeDetector!.ProduceEvents(fromApi, fromRepository, "test");

        // Assert
        //Assert.That(subscriber!.Left, Is.Not.Null);
        //Assert.That(subscriber!.Left!.DifferenceCount, Is.EqualTo(1));
        //Assert.That(subscriber!.Left!.EntitiesDifference.Count(), Is.EqualTo(1));
        //Assert.That(subscriber!.Left!.EntitiesDifference.First().Id, Is.EqualTo(allChannelMembers[1].Id));

        //Assert.That(subscriber!.Joined, Is.Null);
    }

    [Test]
    public async Task AllExist_ThirdFourthLeave()
    {
        // Arrange
        var fromRepository = allChannelMembers!;
        var fromApi = new List<ChannelMember>() { allChannelMembers![2], allChannelMembers[3] };

        // Act
        subscribersChangeDetector!.ProduceEvents(fromApi, fromRepository, "test");

        // Assert
        //Assert.That(subscriber!.Left, Is.Not.Null);
        //Assert.That(subscriber!.Left!.DifferenceCount, Is.EqualTo(2));
        //Assert.That(subscriber!.Left!.EntitiesDifference.Count(), Is.EqualTo(2));
        //Assert.That(subscriber!.Left!.EntitiesDifference.First().Id, Is.EqualTo(allChannelMembers[0].Id));
        //Assert.That(subscriber!.Left!.EntitiesDifference.Last().Id, Is.EqualTo(allChannelMembers[1].Id));

        //Assert.That(subscriber!.Joined, Is.Null);
    }

    [Test]
    public async Task SecondExist_FirstLeft_ThirdForthJoined()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>() { allChannelMembers![0], allChannelMembers[1] };
        var fromApi = new List<ChannelMember>() { allChannelMembers[1], allChannelMembers[2], allChannelMembers[3] };

        // Act
        subscribersChangeDetector!.ProduceEvents(fromApi, fromRepository, "test");

        // Assert
        //Assert.That(subscriber!.Left, Is.Not.Null);
        //Assert.That(subscriber!.Left!.DifferenceCount, Is.EqualTo(1));
        //Assert.That(subscriber!.Left!.EntitiesDifference.Count(), Is.EqualTo(1));
        //Assert.That(subscriber!.Left!.EntitiesDifference.First().Id, Is.EqualTo(allChannelMembers[0].Id));

        //Assert.That(subscriber!.Joined, Is.Not.Null);
        //Assert.That(subscriber!.Joined!.DifferenceCount, Is.EqualTo(2));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.Count(), Is.EqualTo(2));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.First().Id, Is.EqualTo(allChannelMembers[2].Id));
        //Assert.That(subscriber!.Joined!.EntitiesDifference.Last().Id, Is.EqualTo(allChannelMembers[3].Id));
    }
}

//internal sealed class ProcessorSubscriber
//{
//    public EntitiesCollectionChangedEventArgs<ChannelMember>? Joined { get; private set; }
//    public EntitiesCollectionChangedEventArgs<ChannelMember>? Left { get; private set; }

//    public async Task OnJoined(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e) => 
//        await Task.Run(() => Joined = e);

//    public async Task OnLeft(object? sender, EntitiesCollectionChangedEventArgs<ChannelMember> e) =>
//        await Task.Run(() => Left = e);
//}