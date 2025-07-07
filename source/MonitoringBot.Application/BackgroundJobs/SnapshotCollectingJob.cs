using MonitoringBot.Application.Commands;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure.RepositoriesImplementations;

using Quartz;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot.Application.BackgroundJobs;
public class SnapshotCollectingJob<TEntity, TOnJoinedEvent, TOnLeftEvent> : IJob
        where TEntity : ISearchableEntity
        where TOnJoinedEvent : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEvent : EntitiesChangedDomainEventBase<TEntity>, new()
{
    //private readonly CreateSnapshotCommand<TEntity, TOnJoinedEvent, TOnLeftEvent> createSnapshotCommand;
    //private readonly BaseCommandJob<CreateSnapshotCommand<TEntity, TOnJoinedEvent, TOnLeftEvent>, CreateSnapshotArguments> baseJob;

    //public async Task Execute(IJobExecutionContext context)
    //{
    //    await baseJob.Execute(context);
    //}

    //public SnapshotCollectingJob() { }

    //public SnapshotCollectingJob()

    //public void Init()
    //{

    //}
    public Task Execute(IJobExecutionContext context)
    {
        throw new NotImplementedException();
    }
}
