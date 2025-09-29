using MonitoringBot.CommonTestUtilities;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;

using NUnit.Framework;

namespace MonitoringBot.Domain.Tests;

public class EntitiesQuantityCounterTests
{
    EventsSequencesExamples eventsSequencesExamples = new();

    [Test]
    public void Positive()
    {
        var events = new List<EntitiesChangedDomainEventBase<ChannelMember>>()
        {
            eventsSequencesExamples.CreateJoinDomainEvent(1, "test1", nameof(SubscriberJoinedEvent), new DateTime(2025,2,5,12,0,0)),
            eventsSequencesExamples.CreateJoinDomainEvent(2, "test2", nameof(SubscriberJoinedEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateLeftDomainEvent(3, "test3", nameof(SubscriberLeftEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateJoinDomainEvent(4, "test2", nameof(SubscriberJoinedEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateJoinDomainEvent(5, "test2", nameof(SubscriberJoinedEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateJoinDomainEvent(6, "test2", nameof(SubscriberJoinedEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateLeftDomainEvent(7, "test3", nameof(SubscriberLeftEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
        };

        var service = new EntitiesQuantityCounter<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        var count = service.EntitiesIncrementByEvents(events);
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void Negative()
    {
        var events = new List<EntitiesChangedDomainEventBase<ChannelMember>>()
        {
            eventsSequencesExamples.CreateJoinDomainEvent(1, "test1", nameof(SubscriberJoinedEvent), new DateTime(2025,2,5,12,0,0)),
            eventsSequencesExamples.CreateLeftDomainEvent(2, "test2", nameof(SubscriberLeftEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateLeftDomainEvent(3, "test3", nameof(SubscriberLeftEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateLeftDomainEvent(4, "test2", nameof(SubscriberLeftEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateJoinDomainEvent(5, "test2", nameof(SubscriberLeftEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateLeftDomainEvent(6, "test2", nameof(SubscriberJoinedEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
            eventsSequencesExamples.CreateLeftDomainEvent(7, "test3", nameof(SubscriberLeftEvent), new DateTime(2025, 2, 5, 12, 5, 0)),
        };

        var service = new EntitiesQuantityCounter<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        var count = service.EntitiesIncrementByEvents(events);
        Assert.That(count, Is.EqualTo(-3));
    }

    [Test]
    public void Zero()
    {
        var events = new List<EntitiesChangedDomainEventBase<ChannelMember>>()
        {
        };

        var service = new EntitiesQuantityCounter<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        var count = service.EntitiesIncrementByEvents(events);
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void OtherEvent_Zero()
    {
        var events = new List<EntitiesChangedDomainEventBase<ChannelMember>>()
        {
            new SomeOtherEvent(),
            new SomeOtherEvent(),
            new SomeOtherEvent()
        };

        var service = new EntitiesQuantityCounter<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        var count = service.EntitiesIncrementByEvents(events);
        Assert.That(count, Is.EqualTo(0));
    }

    class SomeOtherEvent : EntitiesChangedDomainEventBase<ChannelMember> { }
}
