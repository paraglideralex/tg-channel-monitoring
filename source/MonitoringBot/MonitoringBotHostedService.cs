using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;

namespace MonitoringBot;

public class MonitoringBotHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MonitoringBotHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Создаём новый scope — это создаст новый MonitoringBotRunner со всеми его scoped-зависимостями
        using var scope = _scopeFactory.CreateScope();

        var runner = scope.ServiceProvider.GetRequiredService<MonitoringBotRunner<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

        await runner.InitializeAsync();
        await runner.MainLoopAsync(); // фоновый запуск без ожидания завершения
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Если нужно — тут можно сделать graceful shutdown, например вызвать runner.StopAsync()
        return Task.CompletedTask;
    }
}
