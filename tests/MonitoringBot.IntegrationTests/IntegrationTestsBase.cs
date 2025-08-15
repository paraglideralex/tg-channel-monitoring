using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Infrastructure.Settings;

using Moq;

using NUnit.Framework;

using System.Threading;

namespace MonitoringBot.IntegrationTests;

public class IntegrationTestsBase
{
    protected SubscribersMonitoringService subscribersMonitoringService;
    protected Mock<FetchUsersBackgroundServiceBase> fetchUsersBackgroundServiceMock;
    protected GetAllCurrentSubscribersIdentitiesQuery getAllCurrentSubscribersIdentitiesQuery;
    protected IEntitiesChangeDetector<long>? entitiesChangeDetector;
    protected AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand;
    protected TelegramApiSettings telegramApiSettings;
    protected GetSubscribersByIdentitiesQuery getSubscribersByIdentitiesQuery;

    protected UsersRepositoryImplementation? usersRepository;
    protected EventsRepositoryImplementation<ChannelMember>? eventsRepository;
    protected SubscribersChangeProcessor? subscribersChangeProcessor;
    protected List<ChannelMember>? allChannelMembers;
    
    protected AddOrUpdateSubscribersCommand? addSubscribersCommand;
    protected GetEventsInPeriodQueryExecution<ChannelMember>? getEventsInPeriodQueryExecution;
    
    protected EventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor;
    protected Mock<ITimeProvider> timeProviderMock;

    protected IEntitiesChangeEventsCreator<ChannelMember> entitiesChangeEventsCreator;

    protected CancellationToken cancellationToken;
    protected ServiceProvider serviceProvider;
    protected IDbContextFactory<MonitoringBotDbContextBase> dbContextFactory;
    protected MonitoringBotDbContextBase dbContext;

    [SetUp]
    public virtual async Task SetUpAsync()
    {
        allChannelMembers =
        [
            new(55, "test1", false, "test1", "test1", "79999998888", new DateTime(2025,1,1,3,3,3), new DateTime(2024,1,1,3,3,3),"test-channel", nameof(SubscriberJoinedEvent)),
            new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025,1,1,3,3,5), new DateTime(2024,1,1,3,3,5),"test-channel", nameof(SubscriberJoinedEvent)),
            new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025,1,1,3,5,5), new DateTime(2024,1,1,3,5,5),"test-channel", nameof(SubscriberJoinedEvent)),
            new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025,1,1,3,3,5), new DateTime(2025,1,1,3,3,5),"test-channel", nameof(SubscriberJoinedEvent))
        ];

        var services = new ServiceCollection();
        services.AddDbContextFactory<MonitoringBotDbContextBase>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
        });

        serviceProvider = services.BuildServiceProvider();
        dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<MonitoringBotDbContextBase>>();

        usersRepository = new UsersRepositoryImplementation(dbContextFactory);
        eventsRepository = new EventsRepositoryImplementation<ChannelMember> (dbContextFactory);

        getEventsInPeriodQueryExecution = new GetEventsInPeriodQueryExecution<ChannelMember>(eventsRepository);
        addEventsCommand = new AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(eventsRepository);

        timeProviderMock = new Mock<ITimeProvider>();

        addSubscribersCommand = new AddOrUpdateSubscribersCommand(usersRepository, timeProviderMock.Object);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand);

        entitiesChangeDetector = new EntitiesChangeDetector<long>();
        eventsMonitoringProcessor = new EventsMonitoringProcessor<ChannelMember>(getEventsInPeriodQueryExecution, timeProviderMock.Object);

        fetchUsersBackgroundServiceMock = new Mock<FetchUsersBackgroundServiceBase>();
        getAllCurrentSubscribersIdentitiesQuery = new(usersRepository);

        telegramApiSettings = new() { ChannelReferenceLink = "test-channel" };
        getSubscribersByIdentitiesQuery = new(usersRepository);

        entitiesChangeEventsCreator = new EntitiesChangeEventsCreator<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(timeProviderMock.Object);

        cancellationToken = new();

        subscribersMonitoringService = new(
            fetchUsersBackgroundServiceMock.Object,
            getAllCurrentSubscribersIdentitiesQuery,
            entitiesChangeDetector,
            addEventsCommand,
            eventsMonitoringProcessor,
            telegramApiSettings,
            getSubscribersByIdentitiesQuery,
            entitiesChangeEventsCreator
            );

        dbContext = await dbContextFactory.CreateDbContextAsync();
    }

    [TearDown]
    public virtual async Task TearDownAsync()
    {
        fetchUsersBackgroundServiceMock.Reset();
        dbContext.Database.EnsureDeleted();
        await dbContext.DisposeAsync();
    }
}
