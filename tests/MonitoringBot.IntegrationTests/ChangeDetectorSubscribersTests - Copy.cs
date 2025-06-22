using Microsoft.EntityFrameworkCore;

using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;

public class ChangeDetectorSubscribersTestsMain
{
    private DbContextOptions<MonitoringBotDbContextInMemory> options;
    private UsersRepositoryInMemoryImplementation? usersRepository;
    private EventsRepositoryImplementation<ChannelMember>? eventsRepository;
    private SubscribersChangeProcessor? subscribersChangeProcessor;
    private MonitoringBotDbContextBase? dbContext;
    private List<ChannelMember>? allChannelMembers;
    private EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>? subscribersChangeDetector;
    private AddOrUpdateSubscribersCommand? addSubscribersCommand;
    private GetEventsInPeriodQueryExecution<ChannelMember>? getEventsInPeriodQueryExecution;
    private AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand;
    private EventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor;
    private const string lastActionBase = "base-action";
    private SubscribersMonitoringService subscribersMonitoringService;
    private Mock<FetchUsersBackgroundServiceBase> fetchUsersBackgroundServiceMock;
    private GetAllCurrentSubscribersQuery getAllCurrentSubscribersQuery;

    [SetUp]
    public void SetUp()
    {
        allChannelMembers =
        [
            new(55, "test1", false, "test1", "test1", "79999998888", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel", nameof(SubscriberJoinedEvent)),
            new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel", nameof(SubscriberJoinedEvent)),
            new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel", nameof(SubscriberJoinedEvent)),
            new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel", nameof(SubscriberJoinedEvent))
        ];

        options = new DbContextOptionsBuilder<MonitoringBotDbContextInMemory>()
            .UseInMemoryDatabase("InMemoryDb")
            .Options;
        dbContext = new MonitoringBotDbContextInMemory(options);

        usersRepository = new UsersRepositoryInMemoryImplementation(dbContext);
        eventsRepository = new EventsRepositoryImplementation<ChannelMember> (dbContext);

        getEventsInPeriodQueryExecution = new GetEventsInPeriodQueryExecution<ChannelMember>(eventsRepository);
        addEventsCommand = new AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(eventsRepository);

        addSubscribersCommand = new AddOrUpdateSubscribersCommand(usersRepository);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand);

        subscribersChangeDetector = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        eventsMonitoringProcessor = new EventsMonitoringProcessor<ChannelMember>(getEventsInPeriodQueryExecution);
        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersQuantityChanged;

        fetchUsersBackgroundServiceMock = new Mock<FetchUsersBackgroundServiceBase>();
        getAllCurrentSubscribersQuery = new(usersRepository);

        subscribersMonitoringService = new(
            fetchUsersBackgroundServiceMock.Object,
            getAllCurrentSubscribersQuery,
            subscribersChangeDetector,
            addEventsCommand,
            eventsMonitoringProcessor,
            "test-channel");
    }

    [TearDown]
    public async Task TearDown()
    {
        var members = dbContext!.ChannelMembers.ToList();
        dbContext.ChannelMembers.RemoveRange(members);
        await dbContext.SaveChangesAsync();

        var events = dbContext!.Events.ToList();
        dbContext.Events.RemoveRange(events);
        await dbContext.SaveChangesAsync();
    }

    [Test]
    public async Task JoinedOneNew_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![0], allChannelMembers[1] };
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns(fromApi);
        await usersRepository!.AddAsync(allChannelMembers![0], nameof(SubscriberJoinedEvent));

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));

        var events = dbContext!.Events.ToList();
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].EntityIdProjection, Is.EqualTo(allChannelMembers[1].Id.ToString()));
        Assert.That(events[0].EventType, Is.EqualTo(nameof(SubscriberJoinedEvent)));
    }

    [Test]
    public async Task JoinedOneOld_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![1] };
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns(fromApi);
        await usersRepository!.AddAsync(allChannelMembers![1], nameof(SubscriberLeftEvent));

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].EntityIdProjection, Is.EqualTo(allChannelMembers[1].Id.ToString()));
        Assert.That(events[0].EventType, Is.EqualTo(nameof(SubscriberJoinedEvent)));
    }

    [Test]
    public async Task OneLeft_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![0] };
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns(fromApi);
        await usersRepository!.AddRange([allChannelMembers![0], allChannelMembers[1]], nameof(SubscriberJoinedEvent));

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(allChannelMembers[0..2]));
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].EntityIdProjection, Is.EqualTo(allChannelMembers[1].Id.ToString()));
        Assert.That(events[0].EventType, Is.EqualTo(nameof(SubscriberLeftEvent)));
    }

    /// <summary>
    /// Исходно были 0 и 1. За один цикл номер 0 ушёл, а номер 2 пришёл.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task OneJoinedOneLeft_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![1], allChannelMembers![2] };
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns(fromApi);
        await usersRepository!.AddRange([allChannelMembers![0], allChannelMembers[1]], nameof(SubscriberJoinedEvent));

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(allChannelMembers[0 .. 3]));
        Assert.That(events.Count, Is.EqualTo(2));

        Assert.That(events, Has.Some.Matches<EventEntity>(e => 
            e.EntityIdProjection == allChannelMembers[2].Id.ToString() && 
            e.EventType == nameof(SubscriberJoinedEvent)));

        Assert.That(events, Has.Some.Matches<EventEntity>(e => 
            e.EntityIdProjection == allChannelMembers[0].Id.ToString() && 
            e.EventType == nameof(SubscriberLeftEvent)));
    }

    [Test]
    public async Task NothingHappens()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![1], allChannelMembers![2] };
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns(fromApi);
        await usersRepository!.AddRange([allChannelMembers![1], allChannelMembers[2]], nameof(SubscriberJoinedEvent));

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
        Assert.That(events.Count, Is.EqualTo(0));
    }
}
