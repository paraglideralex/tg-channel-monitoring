using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;

using Serilog;

namespace MonitoringBot.Application.Commands;

public sealed class CreateSnapshotCommand<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs>(
    SnapshotRepository snapshotRepository,
    AggregateSnapshotCreator<TEntity, TOnJoinedEventArgs, TOnLeftEventArgs> snapshotCreator) : BaseCommand<CreateSnapshotArguments>
        where TEntity : ISearchableEntity
        where TOnJoinedEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEventArgs : EntitiesChangedDomainEventBase<TEntity>, new()
{
    protected override async Task ExecuteCoreAsync(CreateSnapshotArguments arguments, ServiceContext serviceContext)
    {
        var snapshot = await snapshotCreator.ExecuteAsync(arguments.AggregateName, serviceContext.CancellationToken);
        await snapshotRepository.Add(snapshot, serviceContext.CancellationToken);
        Log.Information($"{GetType().Name} finished successfully.");
    }
}
