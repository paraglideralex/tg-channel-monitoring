using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

namespace MonitoringBot.Application.Commands;

public sealed class AddBotUserCommand(
    BotUserRepository botUserRepository,
    ITimeProvider timeProvider) : BaseCommand<BotUser>
{
    protected override async Task ExecuteCoreAsync(BotUser user, ServiceContext serviceContext)
    {
        await botUserRepository.AddBotUserAsync(user, timeProvider.UtcNow, serviceContext.CancellationToken);
    }
}
