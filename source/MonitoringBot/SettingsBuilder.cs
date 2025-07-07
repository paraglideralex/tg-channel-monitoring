using Microsoft.Extensions.Configuration;

using MonitoringBot.Infrastructure.Settings;

namespace MonitoringBot;
public class SettingsBuilder
{
    public TelegramBotSettings? TelegramBotSettingsSection { get; private set; }
    public TelegramApiSettings? TelegramApiSettingsSection { get; private set; }
    public DatabaseSettings? DatabaseSettingsSection { get; private set; }
    public QuartzSettings? QuartzSettingsSection { get; private set; }
    public SnapshotCollectingSettings? SnapshotCollectingSettingsSection { get; private set; }

    public T BuildSettings<T>(IConfiguration appsettings, string sectionName)
    {
        return appsettings.GetSection(sectionName).Get<T>()
            ?? throw new InvalidDataException($"{sectionName} section doesn't exist.");
    }

    public void Build()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        var configPath = Path.Combine(AppContext.BaseDirectory, "Configurations");

        var builder = new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        IConfiguration appsettings = builder.Build();

        TelegramBotSettingsSection = BuildSettings<TelegramBotSettings>(appsettings, "TelegramBot");
        TelegramApiSettingsSection = BuildSettings<TelegramApiSettings>(appsettings, "TelegramApi");
        DatabaseSettingsSection = BuildSettings<DatabaseSettings>(appsettings, "Database");
        QuartzSettingsSection = BuildSettings<QuartzSettings>(appsettings, "Quartz");
        SnapshotCollectingSettingsSection = BuildSettings<SnapshotCollectingSettings>(appsettings, "SnapshotCollecting");
    }
}
