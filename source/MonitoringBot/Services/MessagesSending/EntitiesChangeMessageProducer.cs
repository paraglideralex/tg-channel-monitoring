using MonitoringBot.Application.Events;

namespace MonitoringBot.Services.MessagesSending;

public abstract class EntitiesChangeMessageProducer<TEntity>
{
    public event Func<object?, MessageCreatedEventArgs, Task>? MessageProduced;

    protected abstract Task<string> CreateLeftMessageAsync(EntitiesCollectionChangedEventArgs<TEntity> e);
    protected abstract Task<string> CreateJoinedMessageAsync(EntitiesCollectionChangedEventArgs<TEntity> e);

    public async Task OnEntitiesJoined(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        var message = await CreateJoinedMessageAsync(e);
        var @event = new MessageCreatedEventArgs(message);
        await RaisePublicMessageEventAsync(MessageProduced, @event);
    }

    public async Task OnEntitiesLeft(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        var message = await CreateLeftMessageAsync(e);
        var @event = new MessageCreatedEventArgs(message);
        await RaisePublicMessageEventAsync(MessageProduced, @event);
    }

    private async Task RaisePublicMessageEventAsync(
        Func<object?, MessageCreatedEventArgs, Task>? @event,
        MessageCreatedEventArgs messageCreatedEventArgs)
    {
        if (@event != null)
            await @event(this, messageCreatedEventArgs);
    }
}
