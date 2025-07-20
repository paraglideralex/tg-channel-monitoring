using MonitoringBot.Application.Commands;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Settings;

using Quartz;
using Quartz.Impl.Matchers;

using Serilog;

using System.Text.Json;

namespace MonitoringBot.Application.BackgroundJobs;


public class SnapshotCollectingJobScheduler<TEntity, TOnJoinedEvent, TOnLeftEvent>(
    SnapshotCollectingSettings snapshotCollectingSettings,
    ITimeProvider timeProvider,
    IScheduler scheduler)
        where TEntity : ISearchableEntity
        where TOnJoinedEvent : EntitiesChangedDomainEventBase<TEntity>, new()
        where TOnLeftEvent : EntitiesChangedDomainEventBase<TEntity>, new()
{
    private const string groupName = "Snapshots Infrastructure";

    public async Task ScheduleAsync(CreateSnapshotArguments arguments, ServiceContext serviceContext, CancellationToken cancellationToken)
    {
        IJobDetail job = JobBuilder.Create<BaseCommandJob<CreateSnapshotCommand<TEntity, TOnJoinedEvent, TOnLeftEvent>, CreateSnapshotArguments>>()
            .WithIdentity(name: "Create Snapshots Command Job",
                          group: groupName)
            .StoreDurably()
            .UsingJobData(nameof(JobData.TimeStamp), timeProvider.UtcDateTimeOffset.ToUnixTimeMilliseconds())
            .UsingJobData(
                nameof(JobData.OperationArguments),
                JsonSerializer.Serialize(arguments)
            )
            .UsingJobData(
                nameof(JobData.ServiceContext),
                JsonSerializer.Serialize(new ServiceContextDto(serviceContext))
            )
            .RequestRecovery()
            .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(name: "Create Snapshots Command Trigger",
                                group: groupName)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(
                    (int)snapshotCollectingSettings.PeriodSeconds)
                .RepeatForever())
                .Build();

        await scheduler!.ScheduleJob(job, trigger, cancellationToken);

        Log.Information($"Background job for group \" {groupName}\" is scheduled successfully.");
    }

    public async Task PauseAsync()
    {
        var triggerKeyList = await scheduler!
            .GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName));

        foreach (var key in triggerKeyList)
            await scheduler.PauseJob(key);

        Log.Warning($"All background jobs of group \"{groupName}\" are paused.");
    }

    public async Task ResumeAsync()
    {
        var triggerKeyList = await scheduler!
            .GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName));

        foreach (var key in triggerKeyList)
            await scheduler.ResumeJob(key);

        Log.Warning($"All background jobs of group \" {groupName}\" are paused.");
    }
}
