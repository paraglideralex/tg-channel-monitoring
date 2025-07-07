using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.RepositoriesAbstarctions;

using Serilog;

namespace MonitoringBot.Application.Commands;

public sealed class CreateSnapshotCommand<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>(
    SnapshotRepository snapshotRepository,
    AggregateSnapshotCreator<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs> snapshotCreator) : BaseCommand<CreateSnapshotArguments>
        where TEntity : ISearchableEntity
        where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
{
    protected override async Task ExecuteCoreAsync(CreateSnapshotArguments arguments)
    {
        var snapshot = await snapshotCreator.ExecuteAsync(arguments.AggregateName);
        await snapshotRepository.Add(snapshot);
        Log.Information($"======================{GetType().Name} штатно отработала.===============================");
    }
}
