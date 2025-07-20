using MonitoringBot.Application.Events;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Services.MessagesSending;

public abstract class EntitiesChangeMessageProducer<TEntity>
{
    public event Func<object?, MessageCreatedEventArgs, ServiceContext, Task>? MessageProduced;

    protected abstract Task<string> CreateLeftMessageAsync(EntitiesCollectionChangedEventArgs<TEntity> e);
    protected abstract Task<string> CreateJoinedMessageAsync(EntitiesCollectionChangedEventArgs<TEntity> e);

    public async Task OnEntitiesJoined(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e, ServiceContext serviceContext)
    {
        var message = await CreateJoinedMessageAsync(e);
        var @event = new MessageCreatedEventArgs(message);
        await RaisePublicMessageEventAsync(MessageProduced, @event, serviceContext);
    }

    public async Task OnEntitiesLeft(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e, ServiceContext serviceContext)
    {
        var message = await CreateLeftMessageAsync(e);
        var @event = new MessageCreatedEventArgs(message);
        await RaisePublicMessageEventAsync(MessageProduced, @event, serviceContext);
    }

    private async Task RaisePublicMessageEventAsync(
        Func<object?, MessageCreatedEventArgs, ServiceContext, Task>? @event,
        MessageCreatedEventArgs messageCreatedEventArgs,
        ServiceContext serviceContext)
    {
        if (@event != null)
            await @event(this, messageCreatedEventArgs, serviceContext);
    }
}
