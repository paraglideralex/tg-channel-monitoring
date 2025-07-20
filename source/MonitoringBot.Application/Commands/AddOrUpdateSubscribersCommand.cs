using MonitoringBot.Application.Events;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;

using Serilog;

namespace MonitoringBot.Application.Commands;

public sealed class AddOrUpdateSubscribersCommand(
    UserRepository userRepository,
    ITimeProvider timeProvider) 
    : BaseCommand<EntitiesCollectionChangedEventArgs<ChannelMember>>
{
    protected override bool Validate(EntitiesCollectionChangedEventArgs<ChannelMember> eventArgs)
    {
        var basic = base.Validate(eventArgs);
        bool eachNotNull = eventArgs.EntitiesDifference.AllNotNull();
        bool notEmpty = eventArgs.EntitiesDifference.Any();
        return basic && eachNotNull && notEmpty;
    }

    protected override async Task ExecuteCoreAsync(EntitiesCollectionChangedEventArgs<ChannelMember> arguments, ServiceContext serviceContext)
    {
        var existingMembers = await userRepository.FindByIdsAsync(arguments.EntitiesDifference.Select(x => x.Id), serviceContext.CancellationToken);

        if(existingMembers is not null && existingMembers.Count != 0)
        {
            await userRepository.UpdateRangeAsync(existingMembers, arguments.EventType, timeProvider.UtcNow, serviceContext.CancellationToken);
            Log.Information($"Обновлены пользователи: {string.Join(";", existingMembers.Select(x => x.NickName))}");
        }

        var newMembers = arguments.EntitiesDifference.Except(existingMembers ?? []);
        await userRepository.AddRangeAsync(newMembers, arguments.EventType, serviceContext.CancellationToken);
        Log.Information($"Новые пользователи добавлены в базу: {string.Join(";", newMembers.Select(x => x.NickName))}");
    }
}
