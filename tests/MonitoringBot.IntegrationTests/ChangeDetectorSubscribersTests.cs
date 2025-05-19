using Microsoft.EntityFrameworkCore;

using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Extensions;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;

using Moq;

using NUnit.Framework;

using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;

namespace MonitoringBot.IntegrationTests;

public class ChangeDetectorSubscribersTests
{
    private UsersRepositoryInMemoryImplementation? usersRepository;
    private SubscribersChangeProcessor? subscribersChangeProcessor;
    private MonitoringBotDbContextBase? dbContext;
    private List<ChannelMember>? allChannelMembers;
    private EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>? subscribersChangeDetector;
    private AddSubscribersCommand? addSubscribersCommand;
    private DeleteSubscribersCommand? deleteSubscribersCommand;

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

        var options = new DbContextOptionsBuilder<MonitoringBotDbContextInMemory>()
            .UseInMemoryDatabase("InMemoryDb")
            .Options;
        dbContext = new MonitoringBotDbContextInMemory(options);

        usersRepository = new UsersRepositoryInMemoryImplementation(dbContext);

        addSubscribersCommand = new AddSubscribersCommand(usersRepository);
        deleteSubscribersCommand = new DeleteSubscribersCommand(usersRepository);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand, deleteSubscribersCommand);
        subscribersChangeProcessor = new SubscribersChangeProcessor(addSubscribersCommand, deleteSubscribersCommand);

        subscribersChangeDetector = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
        //subscribersChangeDetector.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersJoined;
        //subscribersChangeDetector.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersLeft;
    }

    [Test]
    public async Task BasicAddition_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![0], allChannelMembers[1] };
        await usersRepository!.Add(allChannelMembers![0]);

        // Act
        var result = subscribersChangeDetector!.ExecuteMonitoring(fromApi, await usersRepository.All());
        var dese = EntityChangedEventsMapping.
            ToEntity<EntitiesChangedDomainEventBase<ChannelMember>, ChannelMember>(result.First());

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
        var result = subscribersChangeDetector!.ExecuteMonitoring(fromApi, await usersRepository.All());
        var dese = EntityChangedEventsMapping.
            ToEntity<EntitiesChangedDomainEventBase<ChannelMember>, ChannelMember>(result.First());

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
        subscribersChangeDetector!.ExecuteMonitoring(fromApi, await usersRepository.All());

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
        subscribersChangeDetector!.ExecuteMonitoring(fromApi, await usersRepository.All());

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
    }
}
