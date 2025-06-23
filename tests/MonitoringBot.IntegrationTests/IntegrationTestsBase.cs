using Microsoft.EntityFrameworkCore;

using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;

public class IntegrationTestsBase
{
    protected DbContextOptions<MonitoringBotDbContextInMemory> options;
    protected UsersRepositoryInMemoryImplementation? usersRepository;
    protected EventsRepositoryImplementation<ChannelMember>? eventsRepository;
    protected SubscribersChangeProcessor? subscribersChangeProcessor;
    protected MonitoringBotDbContextBase? dbContext;
    protected List<ChannelMember>? allChannelMembers;
    protected EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>? subscribersChangeDetector;
    protected AddOrUpdateSubscribersCommand? addSubscribersCommand;
    protected GetEventsInPeriodQueryExecution<ChannelMember>? getEventsInPeriodQueryExecution;
    protected AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand;
    protected EventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor;
    protected const string lastActionBase = "base-action";
    protected SubscribersMonitoringService subscribersMonitoringService;
    protected Mock<FetchUsersBackgroundServiceBase> fetchUsersBackgroundServiceMock;
    protected GetAllCurrentSubscribersQuery getAllCurrentSubscribersQuery;
    protected Mock<ITimeProvider> timeProviderMock;

    [SetUp]
    public virtual void SetUp()
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

        timeProviderMock = new Mock<ITimeProvider>();

        addSubscribersCommand = new AddOrUpdateSubscribersCommand(usersRepository, timeProviderMock.Object);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand);

        subscribersChangeDetector = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(timeProviderMock.Object);
        eventsMonitoringProcessor = new EventsMonitoringProcessor<ChannelMember>(getEventsInPeriodQueryExecution, timeProviderMock.Object);

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
    public virtual async Task TearDown()
    {
        var members = dbContext!.ChannelMembers.ToList();
        dbContext.ChannelMembers.RemoveRange(members);
        await dbContext.SaveChangesAsync();

        var events = dbContext!.Events.ToList();
        dbContext.Events.RemoveRange(events);
        await dbContext.SaveChangesAsync();
    }
}
