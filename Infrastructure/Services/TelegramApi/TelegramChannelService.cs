using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Diagnostics;
using MonitoringBot.Infrastructure.Services.TelegramApi;

using Serilog;

using TgChannelApi;

using TL;

using WTelegram;

using Channel = TL.Channel;

public class TelegramChannelService : UserFetcherBase
{
    private readonly TelegramConfig config;
    private readonly Client client;
    private readonly string channelReference;

    private const int delayBetweenParticipantsRequestsMilliseconds = 1050;

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

    private string SessionPath()
    {
        // works both on Windows and Linux
        var exeDir = AppContext.BaseDirectory;
        var sessionDir = exeDir;

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

    private async Task<Messages_Dialogs?> TryGetAllDialogsAsync()
    {
        try
        {
            return await client.Messages_GetAllDialogs();
        }
        catch (RpcException ex)
        {
            Log.Error($"Исключение на уровне RPC Telegram API во время получения диалогов: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Неопознанное исключение Telegram API во время получения диалогов: {ex.Message}");
            return null;
        }
    }

    private async Task<Channels_ChannelParticipants?> TryGetChannelMembersAsynchh(Channel channel)
    {
        try
        {
            return await Safe_GetAllParticipants(
                channel,
                delayBetweenRequestsMilliseconds: delayBetweenParticipantsRequestsMilliseconds);
        }
        catch (RpcException ex)
        {
            Log.Error($"Исключение на уровне RPC Telegram API во время получения всех участников: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Неопознанное исключение Telegram API во время получения всех участников: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Copied from nuget-package as legacy. Implemented Task.Delay to avoid 420 Flood Wait error.
    /// </summary>
    /// <returns></returns>
    private async Task<Channels_ChannelParticipants> Safe_GetAllParticipants(InputChannelBase channel,
        bool includeKickBan = false,
        string alphabet1 = "АБCДЕЄЖФГHИІJКЛМНОПQРСТУВWХЦЧШЩЫЮЯЗ",
        string alphabet2 = "АCЕHИJЛМНОРСТУВWЫ",
        CancellationToken cancellationToken = default,
        int delayBetweenRequestsMilliseconds = 1050)
    {
        alphabet2 ??= alphabet1;
        var result = new Channels_ChannelParticipants { chats = [], users = [] };
        var user_ids = new HashSet<long>();
        var participants = new List<ChannelParticipantBase>();

        var mcf = await client.Channels_GetFullChannel(channel);
        result.count = mcf.full_chat.ParticipantsCount;
        if (result.count > 2000 && ((Channel)mcf.chats[channel.ChannelId]).IsChannel)
            Helpers.Log(2, "Fetching all participants on a big channel can take several minutes...");
        await GetWithFilter(new ChannelParticipantsAdmins());
        await GetWithFilter(new ChannelParticipantsBots());
        await GetWithFilter(new ChannelParticipantsSearch { q = "" }, (f, c) => new ChannelParticipantsSearch { q = f.q + c }, alphabet1);
        if (includeKickBan)
        {
            await GetWithFilter(new ChannelParticipantsKicked { q = "" }, (f, c) => new ChannelParticipantsKicked { q = f.q + c }, alphabet1);
            await GetWithFilter(new ChannelParticipantsBanned { q = "" }, (f, c) => new ChannelParticipantsBanned { q = f.q + c }, alphabet1);
        }
        result.participants = [.. participants];
        return result;

        async Task GetWithFilter<T>(T filter, Func<T, char, T> recurse = null, string? alphabet = null) where T : ChannelParticipantsFilter
        {
            Channels_ChannelParticipants ccp;
            int maxCount = 0;
            for (int offset = 0; ;)
            {
                await Task.Delay(delayBetweenParticipantsRequestsMilliseconds);
                cancellationToken.ThrowIfCancellationRequested();
                ccp = await client.Channels_GetParticipants(channel, filter, offset, 1024, 0);
                if (ccp.count > maxCount) maxCount = ccp.count;
                foreach (var kvp in ccp.chats) result.chats[kvp.Key] = kvp.Value;
                foreach (var kvp in ccp.users) result.users[kvp.Key] = kvp.Value;
                lock (participants)
                    foreach (var participant in ccp.participants)
                        if (user_ids.Add(participant.UserId))
                            participants.Add(participant);
                offset += ccp.participants.Length;
                if (offset >= ccp.count || ccp.participants.Length == 0) break;
            }
            Helpers.Log(0, $"GetParticipants({(filter as ChannelParticipantsSearch)?.q}) returned {ccp.count}/{maxCount}.\tAccumulated count: {participants.Count}");
            if (recurse != null && (ccp.count < maxCount - 100 || ccp.count == 200 || ccp.count == 1000))
            {
                foreach (var c in alphabet)
                {
                    await GetWithFilter(recurse(filter, c), recurse, c == 'А' ? alphabet : alphabet2);
                }
            }
        }
    }

    public override async Task LoginAsync()
    {
        var user = await client.LoginUserIfNeeded();
        Log.Information($"✅ Logged in as: {user.username ?? user.first_name}");
    }

    public override async Task<List<ChannelMember>?> GetChannelMembersAsync()
    {
        List<ChannelMember> members = new();

        Messages_Dialogs? dialogs = await TryGetAllDialogsAsync();

        if (dialogs is null)
            return null;

        foreach (var peer in dialogs.chats.Values)
        {
            if (peer is Channel channel && channel.IsChannel && channel.MainUsername == channelReference)
            {
                using var timer = new ExecutionTimer(
                    "Безопасный поиск всех подписчиков",
                    t => LastSearchParicipantsDurationSeconds = t.TotalSeconds);

                var participants = await TryGetChannelMembersAsynchh(channel);

                if (participants is null)
                    return null;

                var result = participants.users.Values;

                foreach (var user in result)
                    members.Add(ToChannelMember(user));

                Log.Debug($"Найдено {result.Count} участников канала");
            }
        }
        return members;
    }

    public override void Dispose() => client.Dispose();

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
