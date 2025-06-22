using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;

using System.Text;

namespace MonitoringBot.Presentation;

public class MessageBuilder(UserRepository userRepository)
{
    public async Task<string> CheckDiagnostics(int dataUpdatePeriodSeconds, 
        int botSubscribersCount,
        DateTime startWorkingTimeStamp,
        TelegramServiceState state,
        string channelReference)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Количество подписчиков: {await userRepository.CountAsync()}");
        sb.AppendLine($"Период обновления данных, секунд: {dataUpdatePeriodSeconds}");
        sb.AppendLine($"Время подсчета подписчиков, секунд: {state.LastSearchParicipantsDurationSeconds}");
        sb.AppendLine($"Количество текущих подписчиков на бота: {botSubscribersCount}");
        sb.AppendLine($"Запущен: {startWorkingTimeStamp.ToString("dd.MM.yyyy HH:mm")}");
        sb.AppendLine($"Статус инициализации: {state.IsInitialized}");
        sb.AppendLine($"Продолжительность работы: '{(DateTime.Now - startWorkingTimeStamp).FormattedDuration()}'");
        sb.AppendLine($"Последний успешный поиск подписчиков: {state.LastSearchParticipantsTimeStamp.ToString("dd.MM.yyyy HH:mm")}");
        sb.AppendLine($"Канал: {channelReference}");
        sb.AppendLine($"Среда: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");
        sb.AppendLine($"Инфа от: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}");
        return sb.ToString();
    }

    private async Task<string> GetLastCoreAsync(List<ChannelMember> subscribers, string? eventType = null, int count = 5)
    {
        if (count <= 0)
            return "⚠️ число не может быть меньше или равно нулю";
        if (count > 50)
            return "⚠️ нельзя вернуть больше 50 юзеров";

        return await FormatMembersAsync(subscribers.Count >= count ? subscribers.Take(count) : subscribers, eventType);
    }

    public async Task<string> Last(string? eventType = null, int count = 5)
    {
        var members = await userRepository.TakeLast(count);
        return await GetLastCoreAsync(members, eventType, count);
    }

    public async Task<string> LastSubscribed(string? eventType = null, int count = 5)
    {
        var members = await userRepository.TakeLastByAction(nameof(SubscriberJoinedEvent), count);
        return await GetLastCoreAsync(members, eventType, count);
    }

    public async Task<string> LastUnsubscribed(string? eventType = null, int count = 5)
    {
        var members = await userRepository.TakeLastByAction(nameof(SubscriberLeftEvent), count);
        return await GetLastCoreAsync(members, eventType, count);
    }

    public string Info(string channelReference) =>
        $"Данный бот предоставляет для канала {channelReference} информацию о хороших новых подписчиках ❤️ и плохих отписавшихся 💩";

    //public string FormatMember(ChannelMemberEntity member, string eventType)
    //{
    //    var sb = new StringBuilder();

    //    sb.AppendLine($"  ID: {member.Id}");
    //    sb.AppendLine($"  Присоединился: {member.Created?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
    //    sb.AppendLine($"  Никнейм: @{(string.IsNullOrEmpty(member.NickName) ? "не указан" : member.NickName)}");
    //    sb.AppendLine($"  Это бот: {(member.IsBot ? "да" : "нет")}");
    //    sb.AppendLine($"  Имя: {member.FirstName}");
    //    sb.AppendLine($"  Фамилия: {member.LastName}");
    //    sb.AppendLine($"  Телефон: {(string.IsNullOrEmpty(member.Phone) ? "не указан" : member.Phone)}");
    //    sb.AppendLine($"  Последнее действие: {MapActions(member.LastAction ?? "null")}");
    //    sb.AppendLine($"  Инфа от: {member.TimeStamp?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
    //    return sb.ToString();
    //}

    //public string FormatMembers(IEnumerable<ChannelMemberEntity> members, string eventType)
    //{
    //    var sb = new StringBuilder();

    //    int index = 1;
    //    foreach (var member in members)
    //    {
    //        sb.AppendLine($"Пользователь #{index}:");
    //        sb.AppendLine(FormatMember(member, eventType));
    //        sb.AppendLine();
    //        index++;
    //    }
    //    if (sb.Length == 0)
    //        sb.AppendLine("Нет пользователей для отображения.");

    //    return sb.ToString();
    //}

    public async Task<string> FormatMemberAsync(ChannelMember member, string? eventType = null)
    {
        var sb = new StringBuilder();
        var lastAction = eventType is null
            ? member.LastAction is null
                ? null
                : member.LastAction
            : eventType;

        var existing = await userRepository.FindById(member.Id);
        var created = existing?.Created ?? member.Created;

        sb.AppendLine($"  ID: {member.Id}");
        sb.AppendLine($"  Никнейм: @{(string.IsNullOrEmpty(member.NickName) ? "не указан" : member.NickName)}");
        sb.AppendLine($"  Это бот: {(member.IsBot ? "да" : "нет")}");
        sb.AppendLine($"  Имя: {member.FirstName}");
        sb.AppendLine($"  Фамилия: {member.LastName}");
        sb.AppendLine($"  Телефон: {(string.IsNullOrEmpty(member.Phone) ? "не указан" : member.Phone)}");
        sb.AppendLine($"  Последнее действие:  {MapActions(lastAction ?? "null")}");
        sb.AppendLine($"  Информация от: {member.TimeStamp?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
        sb.AppendLine($"  Впервые зарегистрирован: {created?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
        return sb.ToString();
    }

    public async Task<string> FormatMembersAsync(IEnumerable<ChannelMember> members, string? eventType = null)
    {
        var sb = new StringBuilder();

        int index = 1;
        foreach (var member in members)
        {
            sb.AppendLine($"Пользователь #{index}:");
            sb.AppendLine(await FormatMemberAsync(member, eventType));
            sb.AppendLine();
            index++;
        }
        if (sb.Length == 0)
            sb.AppendLine("Нет пользователей для отображения.");

        return sb.ToString();
    }

    //public string Last(List<ChannelMember> members, string eventType, int count = 5)
    //{
    //    if (count <= 0)
    //        return "число не может быть меньше или равно нулю";
    //    if (count > 50)
    //        return "нельзя вернуть больше 50 юзеров";

    //    _ = members.OrderByDescending(m => m.TimeStamp);

    //    return FormatMembers(members.Count >= count ? members.Take(count) : members, eventType);
    //}

    public string MapActions(string action) => action switch
    {
        nameof(SubscriberJoinedEvent) => "Подписка",
        nameof(SubscriberLeftEvent) => "Отписка",
        _ => $"Неизвестное действие {action}"
    };
}
