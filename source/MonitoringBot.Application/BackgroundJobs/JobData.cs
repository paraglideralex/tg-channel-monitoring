namespace MonitoringBot.Application.BackgroundJobs;

public sealed class JobData
{
    public string? OperationArguments { get; set; }
    public long? TimeStamp { get; set; }
}
