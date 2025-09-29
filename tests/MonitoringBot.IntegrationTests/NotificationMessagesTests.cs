using Microsoft.Extensions.Caching.Memory;

using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.Application.Queries.BotUsers;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Snapshots;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Caching;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;

public class NotificationMessagesTests : IntegrationTestsBase
{
    private EntitiesChangeMessageProducer<ChannelMember> entitiesChangeMessageProducer;
    private MessageConsumer messageConsumer;
    private GetTimeSpanBetweenLastEventsQueryExecution<ChannelMember> getTimeSpanBetweenLastEventsQueryExecution;
    private GetUsersCountForPeriodQueryExecution getUsersCountForPeriodQueryExecution;
    private ClosestSnapshotByTimeQueryExecution closestSnapshotByTimeQueryExecution;
    private SnapshotRepository snapshotRepository;
    private BotUsersService botUsersService;
    private BotUserRepository botUserRepository;
    private AddBotUserCommand addBotUserCommand;
    private IMemoryCache memoryCache;
    private GetActiveBotUsersQuery getActiveBotUsersQuery;
    private GetUserByNickNameQueryExecution getUserByNickNameQueryExecution;

    [SetUp]
    public override async Task SetUpAsync()
    {
        await base.SetUpAsync();

        snapshotRepository = new SnapshotRepositoryImplementation(dbContextFactory);
        botUserRepository = new BotUserRepositoryImplementation(dbContextFactory);

        getTimeSpanBetweenLastEventsQueryExecution = new(eventsRepository, timeProviderMock.Object);
        getUsersCountForPeriodQueryExecution = new(usersRepository, eventsRepository, new UsersCountForPeriodCore());
        closestSnapshotByTimeQueryExecution = new(snapshotRepository);
        memoryCache = new MemoryCache(new MemoryCacheOptions());
        addBotUserCommand = new(botUserRepository, timeProviderMock.Object);
        getActiveBotUsersQuery = new(botUserRepository);
        botUsersService = new(addBotUserCommand, memoryCache, new CacheKeysFactory(), getActiveBotUsersQuery);
        getUserByNickNameQueryExecution = new(usersRepository);

        entitiesChangeMessageProducer = new SubscribersChangeMessageProducer(
            new MonitoringPresentation(
                new MessageBuilder(usersRepository, getTimeSpanBetweenLastEventsQueryExecution, getUsersCountForPeriodQueryExecution, 
                closestSnapshotByTimeQueryExecution, timeProviderMock.Object, botUsersService, getUserByNickNameQueryExecution)
                ));

        messageConsumer = new();
        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersQuantityChanged;

        eventsMonitoringProcessor.EntitiesJoined += entitiesChangeMessageProducer.OnEntitiesJoined;
        eventsMonitoringProcessor.EntitiesLeft += entitiesChangeMessageProducer.OnEntitiesLeft;

        entitiesChangeMessageProducer.MessageProduced += messageConsumer.OnMessageProduced;
    }

    [TearDown]
    public override async Task TearDownAsync()
    {
        await base.TearDownAsync();
        messageConsumer?.Messages?.Clear();
        timeProviderMock.Reset();
    }

    [Test]
    public async Task JoinedOneNew_Success()
    {
        // Arrange
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 9, 1));
        var fromApi = new List<ChannelMember> { allChannelMembers![0]};
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();
        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);

        fetchUsersBackgroundServiceMock.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([fromApi[0]]);

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(1));

        var message = messageConsumer.Messages[0];
        Assert.That(message, Contains.Substring("Ура, новые подпищщики!"));
        Assert.That(message, Contains.Substring("Последнее действие: Подписка"));
        Assert.That(message, Contains.Substring("Информация от: 01.01.2025 03:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2024 03:03"));
    }

    [Test]
    public async Task JoinedOneOld_Success()
    {
        // Arrange & Act

        // First joins at first time
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        var fromApi = new List<ChannelMember> { allChannelMembers![1] };
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();
        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);

        fetchUsersBackgroundServiceMock.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([fromApi[0]]);

        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // First leaves
        timeProviderMock.Reset();
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 2, 1, 3, 3, 5));
        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // First joins again
        timeProviderMock.Reset();
        var newJoinDateTime = new DateTime(2025, 3, 25, 5, 3, 7);
        timeProviderMock.Setup(x => x.UtcNow).Returns(newJoinDateTime);
        var updatedUser = new ChannelMember(77, "test2", false, "test2", "test2", "79999998889", newJoinDateTime, newJoinDateTime, "test-channel", null);

        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([updatedUser.Id]);

        fetchUsersBackgroundServiceMock.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([updatedUser]);

        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(3));

        var message = messageConsumer.Messages[2];
        Assert.That(message, Contains.Substring("Ура, новые подпищщики!"));
        Assert.That(message, Contains.Substring("Он снова вернулся к нам после перерыва: 52d, 2h, 2s"));
        Assert.That(message, Contains.Substring("Последнее действие: Подписка"));
        Assert.That(message, Contains.Substring("Информация от: 25.03.2025 05:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2024 03:03"));
    }

    [Test]
    public async Task TwoLeft_Success()
    {
        // Arrange & Act

        // Two Join
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        var fromApi = new List<ChannelMember> { allChannelMembers![1], allChannelMembers![2] };
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();
        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);

        fetchUsersBackgroundServiceMock.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromApi);

        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);


        // Two leave
        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        timeProviderMock.Reset();
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 2, 2, 9, 1, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(2));

        var message = messageConsumer.Messages[1];
        Assert.That(message, Contains.Substring("Неееет! От нас свалили!"));
        Assert.That(message, Contains.Substring("Пользователь #1:"));
        Assert.That(message, Contains.Substring("Пользователь #2:"));
        Assert.That(message, Contains.Substring("Человека хватило на 32d, 5h, 58m..."));
        Assert.That(message, Contains.Substring("ID: 77"));
        Assert.That(message, Contains.Substring("ID: 88"));
        Assert.That(message, Contains.Substring("Последнее действие: Отписка"));
        Assert.That(message, Contains.Substring("Информация от: 01.01.2025 03:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2024 03:05"));
    }
}

internal class MessageConsumer
{
    public List<string> Messages { get; private set; } = [];

    public Task OnMessageProduced(object? sender, MessageCreatedEventArgs args, ServiceContext serviceContext)
    {
        Messages.Add(args.Message);
        return Task.CompletedTask;
    }
}
