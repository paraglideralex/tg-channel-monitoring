using Serilog;

namespace MonitoringBot;
public class LoggingSetup
{
    public static void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
            .WriteTo.File(
                path: "Logs/log.txt",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 50 * 1024 * 1024, // 10 МБ
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 7 // Хранить максимум 7 файлов, каждый заданное количество мегабайт
            )
        .CreateLogger();
    }
}
