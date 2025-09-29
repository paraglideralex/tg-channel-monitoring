namespace MonitoringBot.Application.Events;

public sealed class MessageCreatedEventArgs : EventArgs
{
    public string Message { get; }

    public MessageCreatedEventArgs(string message)
    {
        Message = message;
    }
}
