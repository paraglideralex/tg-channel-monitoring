using MonitoringBot.Application.Events;

namespace MonitoringBot.Services.MessagesSending;

public abstract class EntitiesChangeMessageProducer<TEntity>
{
    public event Func<object?, MessageCreatedEventArgs, Task>? MessageProduced;

    protected abstract string CreateLeftMessage(EntitiesCollectionChangedEventArgs<TEntity> e);
    protected abstract string CreateJoinedMessage(EntitiesCollectionChangedEventArgs<TEntity> e);

    public async Task OnEntitiesJoined(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        var message = CreateJoinedMessage(e);
        var @event = new MessageCreatedEventArgs(message);
        await RaisePublicMessageEventAsync(MessageProduced, @event);
    }

    public async Task OnEntitiesLeft(object? sender, EntitiesCollectionChangedEventArgs<TEntity> e)
    {
        var message = CreateLeftMessage(e);
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
