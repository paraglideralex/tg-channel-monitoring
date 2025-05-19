using Microsoft.EntityFrameworkCore;

using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.RepositoriesImplementations;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;

public class ChangeDetectorSubscribersTests
{
    private DbContextOptions<MonitoringBotDbContextInMemory> options;
    private UsersRepositoryInMemoryImplementation? usersRepository;
    private EventsRepositoryImplementation<ChannelMember>? eventsRepository;
    private SubscribersChangeProcessor? subscribersChangeProcessor;
    private MonitoringBotDbContextBase? dbContext;
    private List<ChannelMember>? allChannelMembers;
    private EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>? subscribersChangeDetector;
    private AddSubscribersCommand? addSubscribersCommand;
    private DeleteSubscribersCommand? deleteSubscribersCommand;
    private GetEventsInPeriodQueryExecution<ChannelMember>? getEventsInPeriodQueryExecution;
    private AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent> addEventsCommand;
    private EventsMonitoringProcessor<ChannelMember> eventsMonitoringProcessor;

    [SetUp]
    public void SetUp()
    {
        allChannelMembers =
        [
            new(55, "test1", false, "test1", "test1", "79999998888", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        ];

        options = new DbContextOptionsBuilder<MonitoringBotDbContextInMemory>()
            .UseInMemoryDatabase("InMemoryDb")
            .Options;
        dbContext = new MonitoringBotDbContextInMemory(options);

        usersRepository = new UsersRepositoryInMemoryImplementation(dbContext);
        eventsRepository = new EventsRepositoryImplementation<ChannelMember> (dbContext);

        getEventsInPeriodQueryExecution = new GetEventsInPeriodQueryExecution<ChannelMember>(eventsRepository);
        addEventsCommand = new AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(eventsRepository);

        addSubscribersCommand = new AddSubscribersCommand(usersRepository);
        deleteSubscribersCommand = new DeleteSubscribersCommand(usersRepository);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand, deleteSubscribersCommand);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand, deleteSubscribersCommand);

        subscribersChangeDetector = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        eventsMonitoringProcessor = new EventsMonitoringProcessor<ChannelMember>(getEventsInPeriodQueryExecution);
        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersJoined;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersLeft;
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
    public async Task BasicAddition_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![0], allChannelMembers[1] };
        await usersRepository!.Add(allChannelMembers![0]);

        // Act
        var result = subscribersChangeDetector!.ProduceEvents(fromApi, await usersRepository.All());
        var addResult = await addEventsCommand.ExecuteAsync(result);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
    }

    [Test]
    public async Task Deletion_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![0] };
        await usersRepository!.AddRange([allChannelMembers![0], allChannelMembers[1]]);

        // Act
        var result = subscribersChangeDetector!.ProduceEvents(fromApi, await usersRepository.All());
        var addResult = await addEventsCommand.ExecuteAsync(result);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.ToList();

        Assert.That(databaseMembers.Count, Is.EqualTo(1));
        Assert.That(databaseMembers[0].Id, Is.EqualTo(allChannelMembers![0].Id));
    }

    [Test]
    public async Task Combined_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![1], allChannelMembers![2] };
        await usersRepository!.AddRange([allChannelMembers![0], allChannelMembers[1]]);

        // Act
        var result = subscribersChangeDetector!.ProduceEvents(fromApi, await usersRepository.All());
        var addResult = await addEventsCommand.ExecuteAsync(result);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
    }

    [Test]
    public async Task NothingHappens()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![1], allChannelMembers![2] };
        await usersRepository!.AddRange([allChannelMembers![1], allChannelMembers[2]]);

        // Act
        var result = subscribersChangeDetector!.ProduceEvents(fromApi, await usersRepository.All());

        var addResult = await addEventsCommand.ExecuteAsync(result);

        await eventsMonitoringProcessor.ExecuteMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
    }
}
