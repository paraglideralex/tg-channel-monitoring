using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Events.Arguments;
using MonitoringBot.Application.Queries.Snapshots;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;

using System.Text;

namespace MonitoringBot.Presentation;

public class MessageBuilder(
    UserRepository userRepository,
    GetTimeSpanBetweenLastEventsQueryExecution<ChannelMember> getLastPreviousEventQueryExecution,
    GetUsersCountForPeriodQueryExecution getUsersCountForPeriodQueryExecution,
    ClosestSnapshotByTimeQueryExecution closestSnapshotByTimeQueryExecution,
    ITimeProvider timeProvider)
{
    public async Task<string> CheckDiagnosticsAsync(int dataUpdatePeriodSeconds, 
        int botSubscribersCount,
        DateTime startWorkingTimeStamp,
        TelegramServiceState state,
        string channelReference,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Количество подписчиков: {await userRepository.CountByLastActionAsync(nameof(SubscriberJoinedEvent), cancellationToken)}");
        sb.AppendLine($"Период обновления данных, секунд: {dataUpdatePeriodSeconds}");
        sb.AppendLine($"Время подсчета подписчиков, секунд: {state.LastSearchParicipantsDurationSeconds}");
        sb.AppendLine($"Количество текущих подписчиков на бота: {botSubscribersCount}");
        sb.AppendLine($"Запущен: {startWorkingTimeStamp.ToString("dd.MM.yyyy HH:mm")}");
        sb.AppendLine($"Статус инициализации: {state.IsInitialized}");
        sb.AppendLine($"Продолжительность работы: '{(timeProvider.UtcNow - startWorkingTimeStamp).FormattedDuration()}'");
        sb.AppendLine($"Последний успешный поиск подписчиков: {state.LastSearchParticipantsTimeStamp.ToString("dd.MM.yyyy HH:mm")}");
        sb.AppendLine($"Канал: {channelReference}");
        sb.AppendLine($"Среда: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");
        sb.AppendLine($"Мониторинг активен: {(isActive ? "да" : "нет")}");
        sb.AppendLine($"Инфа от: {timeProvider.UtcNow.ToString("dd.MM.yyyy HH:mm")}");
        return sb.ToString();
    }

    public async Task<string> LastAsync(string? eventType = null, int count = 5, CancellationToken cancellationToken = default)
    {
        var members = await userRepository.TakeLast(count);
        return await GetLastCoreAsync(members, eventType, count);
    }

    public async Task<string> LastSubscribedAsync(string? eventType = null, int count = 5, CancellationToken cancellationToken = default)
    {
        var members = await userRepository.TakeLastByActionAsync(nameof(SubscriberJoinedEvent), cancellationToken, count);
        return await GetLastCoreAsync(members, eventType, count);
    }

    public async Task<string> LastUnsubscribedAsync(string? eventType = null, int count = 5, CancellationToken cancellationToken = default)
    {
        var members = await userRepository.TakeLastByActionAsync(nameof(SubscriberLeftEvent), cancellationToken, count);
        return await GetLastCoreAsync(members, eventType, count);
    }

    public string Info(string channelReference) =>
        $"Данный бот предоставляет для канала {channelReference} информацию о хороших новых подписчиках ❤️ и плохих отписавшихся 💩";

    public async Task<string> NotificationMessageForOneAsync(
        ChannelMember member,
        ServiceContext serviceContext,
        string? eventType = null)
    {
        var lastAction = DefineLastAction(member, eventType);

        var lastTimeSpan = await getLastPreviousEventQueryExecution.ExecuteAsync(new()
        {
            UserIdentity = member.Id,
            LastAction = lastAction,
            LastTimeStamp = member.TimeStamp,
        },
        serviceContext);

        if (lastTimeSpan is null)
            return await FormatMemberAsync(member, eventType);

        var specialMessage = lastAction is nameof(SubscriberJoinedEvent)
            ? $"Он снова вернулся к нам после перерыва: {((TimeSpan)lastTimeSpan).FormattedDuration()}😃!\r\n"
            : $"Человека хватило на {((TimeSpan)lastTimeSpan).FormattedDuration()}...🙄\r\n";

        var result = specialMessage + await FormatMemberAsync(member, eventType);

        return result;
    }

    public async Task<string> NotificationMessageForManyAsync(IEnumerable<ChannelMember> members, ServiceContext serviceContext, string? eventType = null)
    {
        var sb = new StringBuilder();

        int index = 1;
        foreach (var member in members)
        {
            sb.AppendLine($"Пользователь #{index}:");
            sb.AppendLine(await NotificationMessageForOneAsync(member, serviceContext, eventType));
            //sb.AppendLine();
            index++;
        }
        if (sb.Length == 0)
            sb.AppendLine("Нет пользователей для отображения.");

        return sb.ToString();
    }

    public async Task<string> FormatMemberAsync(
        ChannelMember member,
        string? eventType = null, 
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        var lastAction = DefineLastAction(member, eventType) ;

        var existing = await userRepository.FindByIdAsync(member.Id, cancellationToken);
        var created = existing?.Created ?? member.Created;

        sb.AppendLine($"  ID: {member.Id}");
        sb.AppendLine($"  Никнейм: @{(string.IsNullOrEmpty(member.NickName) ? "не указан" : member.NickName)}");
        sb.AppendLine($"  Это бот: {(member.IsBot ? "да" : "нет")}");
        sb.AppendLine($"  Имя: {member.FirstName}");
        sb.AppendLine($"  Фамилия: {member.LastName}");
        sb.AppendLine($"  Телефон: {(string.IsNullOrEmpty(member.Phone) ? "не указан" : member.Phone)}");
        sb.AppendLine($"  Последнее действие: {MapActions(lastAction ?? "null")}");
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

    public async Task<string> CountHistoryAsync(
        string aggregateName,
        ServiceContext serviceContext,
        DateTime? toDateTimeInclusive = null,
        DateTime? fromDateTimeNonInclusive = null,
        TimeSpan? step = null)
    {
        var actualToDateTimeInclusive = toDateTimeInclusive ?? timeProvider.UtcNow;
        var actualFromDateTimeNonInclusive = fromDateTimeNonInclusive ?? actualToDateTimeInclusive.AddDays(-30);
        var actualStep = step ?? TimeSpan.FromDays(1);

        var queryResult = await getUsersCountForPeriodQueryExecution.ExecuteAsync(new()
        {
            AggregateName = aggregateName,
            Step = actualStep,
            FromNonInclusive = actualFromDateTimeNonInclusive,
            ToInclusive = actualToDateTimeInclusive
        },
        serviceContext);

        var period = toDateTimeInclusive is null && fromDateTimeNonInclusive is null
            ? "месяц"
            : toDateTimeInclusive is not null && fromDateTimeNonInclusive is not null
                ?((DateTime)toDateTimeInclusive - (DateTime)fromDateTimeNonInclusive).FormattedDuration()
                : (toDateTimeInclusive - fromDateTimeNonInclusive).ToString();

        var sb = new StringBuilder();
        sb.AppendLine($"📈 Вот так менялось количество подписчиков за {period}: 📊");
        sb.AppendLine();
        foreach (var item in queryResult)
        {
            sb.AppendLine($"{item.Date.ToString("dd.MM.yyyy HH:mm")}:\t{item.UsersCount}");
        }

        return sb.ToString();

    }

    public async Task<string> HistorySnapshotAsync(string aggregateName, ServiceContext serviceContext, DateTime? timeStamp = null)
    {
        (TimeSpan timeSpan, string name) = timeStamp is null
            ? (TimeSpan.FromDays(7), "Неделю")
            : (timeProvider.UtcNow - timeStamp.Value, (timeProvider.UtcNow - timeStamp.Value).FormattedDuration());

        var realTimeStamp = timeStamp ?? timeProvider.UtcNow - timeSpan;

        long currentCount = await userRepository.CountByLastActionAsync(nameof(SubscriberJoinedEvent), serviceContext.CancellationToken);
        var snapshot = await closestSnapshotByTimeQueryExecution.ExecuteAsync(
            new()
            {
                AggregateName = aggregateName,
                TimeStamp = realTimeStamp
            },
            serviceContext);

        long? previousCount = snapshot?.TotalEntities;

        if (previousCount is null)
            return $"Для вывода такой статистики пока недостаточно истории 🧐. Главное, что сейчас нас уже {currentCount}!💅🏻";

        if (currentCount > previousCount)
            return $"{name} назад нас было {previousCount}, а сейчас уже {currentCount}! Так держать ✊✊";

        if (currentCount < previousCount)
            return $"В силу деградации человеческих потребностей и оскуднения мышления среднестатистического потребителя" +
                $"соцсетевого контента наше количество за {name} уменьшилось с {previousCount} до {currentCount} 🤡.";
        else return $"Вот уже {name} мы стабильно держим отметку в {currentCount} подписчиков👍🏼.";
    }

    private string MapActions(string action) => action switch
    {
        nameof(SubscriberJoinedEvent) => "Подписка",
        nameof(SubscriberLeftEvent) => "Отписка",
        _ => $"Неизвестное действие {action}"
    };

    private async Task<string> GetLastCoreAsync(List<ChannelMember> subscribers, string? eventType = null, int count = 5)
    {
        if (count <= 0)
            return "⚠️ число не может быть меньше или равно нулю";
        if (count > 50)
            return "⚠️ нельзя вернуть больше 50 юзеров";

        return await FormatMembersAsync(subscribers.Count >= count ? subscribers.Take(count) : subscribers, eventType);
    }

    private string? DefineLastAction(ChannelMember member, string? eventType = null) =>

    eventType is null
        ? member.LastAction is null
            ? null
            : member.LastAction
        : eventType;
}
