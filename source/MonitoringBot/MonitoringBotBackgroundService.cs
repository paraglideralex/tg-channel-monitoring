using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;

namespace MonitoringBot;

public class MonitoringBotBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MonitoringBotBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var runner = scope.ServiceProvider.GetRequiredService<MonitoringBotRunner<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

        await runner.InitializeAsync();

        await runner.MainLoopAsync();
    }
}
