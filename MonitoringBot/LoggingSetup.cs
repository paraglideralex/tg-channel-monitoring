using Serilog;

namespace MonitoringBot;
public class LoggingSetup
{
    public static void SetupLogging()
    {
        // Получаем абсолютный путь к директории логов
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        string logFilePath = Path.Combine(logDirectory, "log.txt");

        // Создаём директорию, если её нет
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 50 * 1024 * 1024, // 10 МБ
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 7 // Хранить максимум 7 файлов, каждый заданное количество мегабайт
            )
        .CreateLogger();

         Log.Warning($"Логи будут записываться в файл: {logFilePath}");
    }
}
