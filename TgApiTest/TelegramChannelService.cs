using MonitoringBot.Infrastructure;

using Serilog;

using TgChannelApi;

using TL;

using WTelegram;

public class TelegramChannelService : IDisposable
{
    private readonly TelegramConfig config;
    private readonly Client client;
    private readonly string channelReference;

    public TelegramChannelService(TelegramConfig config, string? channelReference)
    {
        this.config = config;
        this.channelReference = channelReference ?? throw new InvalidOperationException("конфиг юзера не задан!!");
        client = new Client(ResolveConfig);
    }

    private string GetVerificationCode()
    {
        Console.Write("verification: ");
        return Console.ReadLine() ?? "";
    }

    public string SessionPath()
    {
        // Получаем путь к директории с исполняемым файлом
        var exeDir = AppContext.BaseDirectory;

        // Для Linux: если путь содержит "bin/Debug" или "bin/Release", оставляем как есть
        // Для Windows: тоже будет работать корректно
        var sessionDir = exeDir;

        // Создаем директорию, если не существует
        if (!Directory.Exists(sessionDir))
            Directory.CreateDirectory(sessionDir);

        return Path.Combine(sessionDir, $"session_{channelReference}");
    }

    private string? ResolveConfig(string what) => what switch
    {
        "api_id" => config.ApiId ?? throw new InvalidOperationException("конфиг юзера не задан!!"),
        "api_hash" => config.ApiHash,
        "phone_number" => config.PhoneNumber,
        //"session_key" => Guid.NewGuid().ToString(),
        "session_pathname" => SessionPath(),
        "verification_code" => GetVerificationCode(),
        _ => null
    };


    public async Task LoginAsync()
    {
        var user = await client.LoginUserIfNeeded();
        Log.Information($"✅ Logged in as: {user.username ?? user.first_name}");
    }

    public async Task<List<ChannelMember>> GetChannelMembersAsync()
    {
        var members = new List<ChannelMember>();

        var dialogs = await client.Messages_GetAllDialogs();
        foreach (var peer in dialogs.chats.Values)
        {
            if (peer is Channel channel && channel.IsChannel && channel.MainUsername == channelReference)
            {
                Console.WriteLine($"📢 Channel: {channel.MainUsername}");
                try
                {
                    var result = await client.Channels_GetParticipants(
                        channel,
                        filter: new ChannelParticipantsRecent(),
                        offset: 0,
                        limit: 1000,
                        hash: 0);

                    foreach (var user in result.users.Values)
                    {
                        Console.WriteLine($" - {user.username ?? user.first_name} ({user.id})");
                        members.Add(ToChannelMember(user));
                    }
                    Log.Debug("Успешный импорт участников канала");
                }
                catch (RpcException ex)
                {
                    Log.Error($"Нету прав на просмотр юзеров, текст: {ex.Message}");
                }
            }
        }

        return members;
    }

    public void Dispose() => client.Dispose();

    public ChannelMember ToChannelMember(User tgUser) => new
    (
        tgUser.ID,
        tgUser.MainUsername,
        tgUser.IsBot,
        tgUser.first_name,
        tgUser.last_name,
        tgUser.phone,
        DateTime.Now,
        DateTime.Now
    );
}
