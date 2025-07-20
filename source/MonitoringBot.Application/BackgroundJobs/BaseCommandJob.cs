using MonitoringBot.Application.Commands;
using MonitoringBot.Infrastructure;

using Quartz;
using Quartz.Util;

using Serilog;

using System.Text.Json;

namespace MonitoringBot.Application.BackgroundJobs;

public class BaseCommandJob<TCommand, TArguments>(TCommand command) : IJob
    where TCommand : BaseCommand<TArguments>
    where TArguments : class
{

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var arguments = GetCommandArguments(context);
            if (arguments is null)
                Log.Error("Unable to deserialize job arguments.");

            var serviceContext = GetServiceContext(context);
            if(serviceContext is null)
                Log.Error("Unable to deserialize service context.");

            await command.ExecuteAsync(arguments!, serviceContext!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Job failed with exception.");
        }
    }

    protected TArguments? GetCommandArguments(IJobExecutionContext context)
    {
        try
        {
            var contextArgumentsSucceed = context.JobDetail.JobDataMap.TryGetString(nameof(JobData.OperationArguments), out string? contextArguments);
            if (!contextArgumentsSucceed || contextArguments.IsNullOrWhiteSpace())
            {
                Log.Error("Unable to parse job arguments.");
            }

            var arguments = JsonSerializer.Deserialize<TArguments>(contextArguments!);
            return arguments;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception while extracting job arguments.");
            return null;
        }
    }

    protected ServiceContext? GetServiceContext(IJobExecutionContext context)
    {
        try
        {
            var contextArgumentsSucceed = context.JobDetail.JobDataMap.TryGetString(nameof(JobData.ServiceContext), out string? contextArguments);
            if (!contextArgumentsSucceed || contextArguments.IsNullOrWhiteSpace())
            {
                Log.Error($"Unable to parse job {nameof(ServiceContext)}.");
            }

            var arguments = JsonSerializer.Deserialize<ServiceContext>(contextArguments!);
            return arguments;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Exception while extracting job {nameof(ServiceContext)}.");
            return null;
        }
    }
}
