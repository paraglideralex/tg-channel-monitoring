using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;

using System.Text;

namespace MonitoringBot.Presentation;

public class MessageBuilder(UserRepository userRepository)
{
    public async Task<string> CheckDiagnostics(int dataUpdatePeriodSeconds, 
        int botSubscribersCount,
        DateTime startWorkingTimeStamp,
        TelegramServiceState state)
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
        sb.AppendLine($"Инфа от: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}");
        return sb.ToString();
    }

    public async Task<string> Last(int count = 5)
    {
        if (count <= 0)
            return "⚠️ число не может быть меньше или равно нулю";
        if (count > 50)
            return "⚠️ нельзя вернуть больше 50 юзеров";

        var members = await userRepository.TakeLast(count);

        return FormatMembers(members.Count >= count ? members.Take(count) : members);
    }

    public string Info(string channelReference) =>
        $"Данный бот предоставляет для канала {channelReference} информацию о хороших новых подписчиках ❤️ и плохих отписавшихся 💩";

    public string FormatMember(ChannelMemberEntity member)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"  ID: {member.Id}");
        sb.AppendLine($"  Присоединился: {member.Created?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
        sb.AppendLine($"  Никнейм: @{(string.IsNullOrEmpty(member.NickName) ? "не указан" : member.NickName)}");
        sb.AppendLine($"  Это бот: {(member.IsBot ? "да" : "нет")}");
        sb.AppendLine($"  Имя: {member.FirstName}");
        sb.AppendLine($"  Фамилия: {member.LastName}");
        sb.AppendLine($"  Телефон: {(string.IsNullOrEmpty(member.Phone) ? "не указан" : member.Phone)}");
        sb.AppendLine($"  Инфа от: {member.TimeStamp?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
        return sb.ToString();
    }

    public string FormatMembers(IEnumerable<ChannelMemberEntity> members)
    {
        var sb = new StringBuilder();

        int index = 1;
        foreach (var member in members)
        {
            sb.AppendLine($"Пользователь #{index}:");
            sb.AppendLine(FormatMember(member));
            sb.AppendLine();
            index++;
        }
        if (sb.Length == 0)
            sb.AppendLine("Нет пользователей для отображения.");

        return sb.ToString();
    }

    public string FormatMember(ChannelMember member)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"  ID: {member.Id}");
        sb.AppendLine($"  Информация от: {member.TimeStamp?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
        sb.AppendLine($"  Никнейм: @{(string.IsNullOrEmpty(member.NickName) ? "не указан" : member.NickName)}");
        sb.AppendLine($"  Это бот: {(member.IsBot ? "да" : "нет")}");
        sb.AppendLine($"  Имя: {member.FirstName}");
        sb.AppendLine($"  Фамилия: {member.LastName}");
        sb.AppendLine($"  Телефон: {(string.IsNullOrEmpty(member.Phone) ? "не указан" : member.Phone)}");
        sb.AppendLine($"  Присоединился: {member.Created?.ToString("dd.MM.yyyy HH:mm") ?? ""}");
        return sb.ToString();
    }

    public string FormatMembers(IEnumerable<ChannelMember> members)
    {
        var sb = new StringBuilder();

        int index = 1;
        foreach (var member in members)
        {
            sb.AppendLine($"Пользователь #{index}:");
            sb.AppendLine(FormatMember(member));
            sb.AppendLine();
            index++;
        }
        if (sb.Length == 0)
            sb.AppendLine("Нет пользователей для отображения.");

        return sb.ToString();
    }

    public string Last(List<ChannelMember> members, int count = 5)
    {
        if (count <= 0)
            return "число не может быть меньше или равно нулю";
        if (count > 50)
            return "нельзя вернуть больше 50 юзеров";

        _ = members.OrderByDescending(m => m.TimeStamp);

        return FormatMembers(members.Count >= count ? members.Take(count) : members);
    }

}
