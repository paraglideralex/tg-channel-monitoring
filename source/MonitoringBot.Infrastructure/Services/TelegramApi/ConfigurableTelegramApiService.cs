using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

using WTelegram;

namespace MonitoringBot.Infrastructure.Services.TelegramApi;

public class ConfigurableTelegramApiService
{
    protected readonly TelegramConfig config;
    protected readonly Client client;
    protected readonly string channelReference;
    protected bool loggedIn = false;

    protected string GetVerificationCode()
    {
        Console.Write("verification: ");
        return Console.ReadLine() ?? "";
    }

    protected string SessionPath()
    {
        // works both on Windows and Linux
        var exeDir = AppContext.BaseDirectory;
        var sessionDir = exeDir;

        if (!Directory.Exists(sessionDir))
            Directory.CreateDirectory(sessionDir);

        return Path.Combine(sessionDir, $"session_{channelReference}");
    }

    protected string? ResolveConfig(string what) => what switch
    {
        "api_id" => config.ApiId ?? throw new InvalidOperationException("конфиг юзера не задан!!"),
        "api_hash" => config.ApiHash,
        "phone_number" => config.PhoneNumber,
        //"session_key" => Guid.NewGuid().ToString(),
        "session_pathname" => SessionPath(),
        "verification_code" => GetVerificationCode(),
        _ => null
    };

    public virtual async Task LoginAsync()
    {
        var user = await client.LoginUserIfNeeded();
        loggedIn = true;
        Log.Information($"✅ Logged in as: {user.username ?? user.first_name}");
    }

    public ConfigurableTelegramApiService(TelegramConfig config, TelegramApiSettings settings)
    {
        this.config = config;
        this.channelReference = settings.ChannelReferenceLink ?? throw new InvalidOperationException("конфиг юзера не задан!!");
        client = new Client(ResolveConfig);
    }
}
