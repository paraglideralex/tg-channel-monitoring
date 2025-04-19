using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Diagnostics;

using Serilog;

using System.Collections.Generic;
using System.Reflection.Metadata;

using TgChannelApi;

using TL;

using WTelegram;

public class TelegramChannelService : IDisposable
{
    private readonly TelegramConfig config;
    private readonly Client client;
    private readonly string channelReference;

    private const int delayBetweenParticipantsRequestsMilliseconds = 1050;
    
    public double LastsearchParicipantsDurationSeconds { get; private set; } = 0;

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
                    using var timer = new ExecutionTimer(
                        "Безопасный поиск всех подписчиков",
                        t => LastsearchParicipantsDurationSeconds = t.TotalSeconds);

                    var participants = await Safe_GetAllParticipants(
                        channel,
                        delayBetweenRequestsMilliseconds: delayBetweenParticipantsRequestsMilliseconds);

                    var result = participants.users.Values;

                    foreach (var user in result)
                    {
                        members.Add(ToChannelMember(user));
                    }
                    Log.Debug($"Успешный импорт {result.Count} участников канала");
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

    /// <summary>
    /// Copied from nuget-package as legacy. Implemented Task.Delay to avoid 420 Flood Wait error.
    /// </summary>
    /// <returns></returns>
    public async Task<Channels_ChannelParticipants> Safe_GetAllParticipants(InputChannelBase channel,
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
}
