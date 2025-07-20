using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Infrastructure.Diagnostics;
using MonitoringBot.Infrastructure.Persistence.Entities;
using MonitoringBot.Infrastructure.Services.FaultSafety;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Infrastructure.Settings;

using Serilog;

using TL;

using WTelegram;

using Channel = TL.Channel;

public class TelegramChannelService : TelegramApiServiceBase
{
    private const int delayBetweenParticipantsRequestsMilliseconds = 1050;
    private Channel? channel;

    public TelegramChannelService(TelegramConfig config, TelegramApiSettings settings, RetryServiceBase retryService, ITimeProvider timeProvider)
        : base(config, settings, retryService, timeProvider)
    {
    }

    private async Task<bool> TryInitializeChannelAsync(CancellationToken cancellationToken)
    {
        var channel = await TryGetAllDialogsAsync(channelReference, cancellationToken);
        if (channel is null)
        {
            Log.Fatal($"Не удалось найти доступный канал со ссылкой '{channelReference}'");
            return false;
        }

        else
        {
            this.channel = channel;
            State.IsInitialized = true;
            return true;
        }
    }

    private async Task<Channel?> TryGetAllDialogsAsync(string channelReference, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await client.Contacts_ResolveUsername(channelReference);
            var channel = resolved.chats.Values.OfType<Channel>().FirstOrDefault();
            return channel;
        }
        catch (RpcException ex)
        {
            Log.Error($"Исключение на уровне RPC Telegram API во время получения нужного диалога: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Неопознанное исключение Telegram API во время получения нужного диалога: {ex.Message}");
            return null;
        }
    }

    private async Task<Channels_ChannelParticipants?> TryGetChannelMembersAsync(Channel channel, CancellationToken cancellationToken)
    {
        try
        {
            return await Safe_GetAllParticipants(
                channel,
                delayBetweenRequestsMilliseconds: delayBetweenParticipantsRequestsMilliseconds,
                cancellationToken: cancellationToken);
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

        async Task GetWithFilter<T>(T filter, Func<T, char, T>? recurse = null, string? alphabet = null) where T : ChannelParticipantsFilter
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
                // Hack: this should be uploaded to database too each recursive step as API users snapshot.
                foreach (var kvp in ccp.users) result.users[kvp.Key] = kvp.Value;
                lock (participants)
                    foreach (var participant in ccp.participants)
                        if (user_ids.Add(participant.UserId))
                        {
                            // Hack: Ideally, at this moment we should load this batch of users into the database instead of sequentially accumulating them in RAM.
                            // With channels exceeding 1M subscribers, current implementation could easily lead to OutOfMemoryException.
                            // One channel entity weights about 400 bytes. So, 10M subscribers leads to ~3.7 Gb RAM bloat
                            // However for channel quantity about 1000 users it works with no problems
                            participants.Add(participant);
                        }

                offset += ccp.participants.Length;
                if (offset >= ccp.count || ccp.participants.Length == 0) break;
            }
            Helpers.Log(0, $"GetParticipants({(filter as ChannelParticipantsSearch)?.q}) returned {ccp.count}/{maxCount}.\tAccumulated count: {participants.Count}");
            if (recurse != null && (ccp.count < maxCount - 100 || ccp.count == 200 || ccp.count == 1000))
            {
                if (alphabet is not null)
                {
                    foreach (var c in alphabet)
                    {
                        await GetWithFilter(recurse(filter, c), recurse, c == 'А' ? alphabet : alphabet2);
                    }
                }
            }
        }
    }

    public override async Task<bool> InitializeChannelAsync(CancellationToken cancellationToken)
    {
        var result = await retryService.ExecuteRetryAsync(
            action: TryInitializeChannelAsync,
            callerName: nameof(TryInitializeChannelAsync),
            cancellationToken,
            maxRetries: 5,
            secondsInitialWait: 20,
            delayIncreaseType: DelayIncreaseType.Exponential);

        return result;
    }

    public override async Task<List<TLUser>?> GetChannelMembersAsync(CancellationToken cancellationToken)
    {
        if (channel is null)
        {
            Log.Error($"Не найдена ссылка на запрашиваемый канал '{channelReference}', " +
                $"пользователи не будут получены. Возможно, стоит инициализировать сервис '{GetType().Name}'");

            return null;
        }

        using var timer = new ExecutionTimer(
            "Safe search of all participants",
            t => State.LastSearchParicipantsDurationSeconds = t.TotalSeconds);

        var participants = await TryGetChannelMembersAsync(channel, cancellationToken);

        if (participants is null)
            return null;

        var result = participants.users.Values;

        var members = participants.participants
            .Select(m => ToTLUser(m, participants.users, channelReference))
            .ToList();

        State.LastSearchParticipantsTimeStamp = timeProvider.UtcNow;
        Log.Debug($"Найдено {result.Count} участников канала");

        return members;
    }

    public TLUser ToTLUser(ChannelParticipantBase participantBase, Dictionary<long, User> usersDictionary, string channelReference)
    {
        _ = usersDictionary.TryGetValue(participantBase.UserId, out var user);
        if (user == null)
        {
            Log.Error($"User with identity {participantBase.UserId} was not found in users collection!");
            return new()
            {
                Id = participantBase.UserId
            };
        }

        return new()
        {
            Id = user.ID,
            ChannelReference = channelReference,
            FirstName = user.first_name,
            LastName = user.last_name,
            IsBot = user.IsBot,
            NickName = user.MainUsername,
            Phone = user.phone,
            TimeStampTicks = timeProvider.UtcNow.Ticks,
            JoinedTicks = participantBase.IsAdmin ? null : (participantBase as ChannelParticipant)?.date.Ticks
        };
    }

    public override void Dispose() => client.Dispose();
}
