using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure.Persistence;

using System.Text.Json;

namespace MonitoringBot.CommonTestUtilities;

public class EventsSequencesExamples
{
    private ChannelMemberEntity CreateChannelMemberEntity(int id, string channel, string? lastAction) =>
    new()
    {
        Id = 1,
        FirstName = $"{id}FirstName",
        LastName = $"{id}LastName",
        IsBot = false,
        Created = new DateTime(2025, 10, 15),
        NickName = $"{id}NickName",
        Phone = "79995553311",
        ChannelReference = $"{channel}",
        TimeStamp = new DateTime(2025, 10, 15),
        LastAction = lastAction
    };

    private ChannelMember CreateChannelMember(int id, string channel, string? lastAction) =>
    new()
    {
        Id = 1,
        FirstName = $"{id}FirstName",
        LastName = $"{id}LastName",
        IsBot = false,
        Created = new DateTime(2025, 10, 15),
        NickName = $"{id}NickName",
        Phone = "79995553311",
        ChannelReference = $"{channel}",
        TimeStamp = new DateTime(2025, 10, 15),
        LastAction = lastAction
    };

    public SubscriberJoinedEvent CreateJoinedDomainEvent(int id, string channel, string lastAction, DateTime timeStamp)
    {
        var userEntity = CreateChannelMember(id, channel, lastAction);
        return new SubscriberJoinedEvent
        {
            Entity = userEntity,
            ChannelName = userEntity.ChannelReference!,
            TimeStamp = timeStamp,
            EntityIdProjection = userEntity.IdProjection(),
            EntityNameProjection = userEntity.NameProjection(),
            Id = Guid.NewGuid()
        };
    }

    public SubscriberLeftEvent CreateLeftDomainEvent(int id, string channel, string lastAction, DateTime timeStamp)
    {
        var userEntity = CreateChannelMember(id, channel, lastAction);
        return new SubscriberLeftEvent
        {
            Entity = userEntity,
            ChannelName = userEntity.ChannelReference!,
            TimeStamp = timeStamp,
            EntityIdProjection = userEntity.IdProjection(),
            EntityNameProjection = userEntity.NameProjection(),
            Id = Guid.NewGuid()
        };
    }

    public EventEntity CreateJoinEvent(int id, string channel, string lastAction, DateTime timeStamp)
    {
        var domainEvent = CreateJoinedDomainEvent(id, channel, lastAction, timeStamp);
        var eventEntity = new EventEntity
        {
            EntityIdProjection = domainEvent.EntityIdProjection,
            AggregateNameProjection = domainEvent.ChannelName,
            EntityNameProjection = domainEvent.EntityNameProjection,
            EntityType = domainEvent.Entity.GetType().Name,
            EventType = domainEvent.GetType().Name,
            Id = Guid.NewGuid(),
            TimeStamp = timeStamp,
            Data = JsonSerializer.Serialize(domainEvent)
        };
        return eventEntity;
    }

    public EventEntity CreateLeftEvent(int id, string channel, string lastAction, DateTime timeStamp)
    {
        var domainEvent = CreateLeftDomainEvent(id, channel, lastAction, timeStamp);
        var eventEntity = new EventEntity
        {
            EntityIdProjection = domainEvent.EntityIdProjection,
            AggregateNameProjection = domainEvent.ChannelName,
            EntityNameProjection = domainEvent.EntityNameProjection,
            EntityType = domainEvent.Entity.GetType().Name,
            EventType = domainEvent.GetType().Name,
            Id = Guid.NewGuid(),
            TimeStamp = timeStamp,
            Data = JsonSerializer.Serialize(domainEvent)
        };
        return eventEntity;
    }
}
