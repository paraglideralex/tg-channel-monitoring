using MonitoringBot.Application.Commands;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure.Settings;

using Quartz;

using System.Text.Json;

namespace MonitoringBot.Application.BackgroundJobs;


public class SnapshotCollectingJobScheduler<TEntity, TOnJoinedEvent, TOnLeftEvent>(
    SnapshotCollectingSettings snapshotCollectingSettings,
    ITimeProvider timeProvider,
    IScheduler scheduler
    )
        where TEntity : ISearchableEntity
        where TOnJoinedEvent : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEvent : EntitiesChangedDomainEventBase<TEntity>, new()
{
    public async Task ScheduleAsync(CreateSnapshotArguments arguments, CancellationToken cancellationToken)
    {
        IJobDetail job = JobBuilder.Create<BaseCommandJob<CreateSnapshotCommand<TEntity, TOnJoinedEvent, TOnLeftEvent>, CreateSnapshotArguments>>()
            .WithIdentity(name: "kek",
                          group: "kek")
            .StoreDurably()
            .UsingJobData(nameof(JobData.TimeStamp), timeProvider.UtcDateTimeOffset.ToUnixTimeMilliseconds())
            .UsingJobData(
                nameof(JobData.OperationArguments),
                JsonSerializer.Serialize(arguments)
            )
            .RequestRecovery()
            .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(name: "JobKek",
                                group: "Jobkek")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(
                    (int)snapshotCollectingSettings.PeriodSeconds)
                .RepeatForever())
                .Build();

        await scheduler!.ScheduleJob(job, trigger, cancellationToken);
    }
}
