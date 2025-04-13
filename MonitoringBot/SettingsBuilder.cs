using Microsoft.Extensions.Configuration;

using MonitoringBot.Infrastructure.Settings;

namespace MonitoringBot;
public class SettingsBuilder
{
    public TelegramBotSettings? TelegramBotSettings { get; private set; }
    public TelegramApiSettings? TelegramApiSettings { get; private set; }

    public void BuildBotSettings(IConfiguration appsettings)
    {
        TelegramBotSettings = appsettings.GetSection("TelegramBot").Get<TelegramBotSettings>()
            ?? throw new InvalidDataException($"{nameof(TelegramBotSettings)} section doesn't exist.");
    }

    public void BuildApiSettings(IConfiguration appsettings)
    {
        TelegramApiSettings = appsettings.GetSection("TelegramApi").Get<TelegramApiSettings>()
            ?? throw new InvalidDataException($"{nameof(TelegramApiSettings)} section doesn't exist.");
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

        BuildBotSettings(appsettings);
        BuildApiSettings(appsettings);
    }
}
