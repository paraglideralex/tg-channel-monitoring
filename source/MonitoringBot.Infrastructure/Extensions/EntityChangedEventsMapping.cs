using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonitoringBot.Infrastructure.Extensions;
public static class EntityChangedEventsMapping
{
    public static EventEntity ToEntity<TEvent, TEntity>(TEvent domainEvent)
    where TEvent : EntitiesChangedDomainEventBase<TEntity>
    {
        return new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateType = typeof(TEntity).Name,
            EventType = domainEvent.GetType().Name,
            Data = JsonSerializer.Serialize(domainEvent),
            TimeStamp = domainEvent.TimeStamp,
            ChannelName = domainEvent.ChannelName,
            EntityIdProjection = domainEvent.EntityIdProjection,
            EntityNameProjection = domainEvent.EntityNameProjection
        };
    }

    public static EntitiesChangedDomainEventBase<TEntity> MapToDomainEvent<TEntity>(EventEntity entity)
    {
        return entity.EventType switch
        {
            nameof(SubscriberJoinedEvent) => JsonSerializer.Deserialize<SubscriberJoinedEvent>(entity.Data)
                as EntitiesChangedDomainEventBase<TEntity>
                ?? throw new InvalidOperationException("Cannot deserialize event data"),

            nameof(SubscriberLeftEvent) => JsonSerializer.Deserialize<SubscriberLeftEvent>(entity.Data)
                as EntitiesChangedDomainEventBase<TEntity>
                ?? throw new InvalidOperationException("Cannot deserialize event data"),

            _ => throw new NotSupportedException($"Unknown event type: {entity.EventType}")
        };
    }
}
