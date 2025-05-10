using Microsoft.EntityFrameworkCore;

using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure.Persistence;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;

public class ChangeDetectorSubscribersTests
{
    private UsersRepositoryInMemoryImplementation? usersRepository;
    private SubscribersChangeProcessor? monitoringEngine;
    private MonitoringBotDbContextBase? dbContext;
    private List<ChannelMember>? allChannelMembers;
    private Mock<IEntitiesChangeDetector<ChannelMember>>? subscribersChangeDetectorMock;

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
        monitoringEngine = new SubscribersChangeProcessor(usersRepository);

        subscribersChangeDetectorMock = new Mock<IEntitiesChangeDetector<ChannelMember>>();
        subscribersChangeDetectorMock.Object.EntitiesChanged += monitoringEngine!.OnSubscribersChanged;
    }

    [Test]
    public async Task BasicAddition_Success()
    {
        // Arrange
        await usersRepository!.Add(allChannelMembers![0]);

        var eventArgs = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            differenceCount: 1,
            entitiesDifference: [allChannelMembers![1]]);

        // Act
        await subscribersChangeDetectorMock!.RaiseAsync(
            d => d.EntitiesChanged += null!,
            subscribersChangeDetectorMock.Object,
            eventArgs);

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.ToList();

        Assert.That(databaseMembers.Count, Is.EqualTo(2));
        Assert.That(databaseMembers[0].Id, Is.EqualTo(allChannelMembers![0].Id));
        Assert.That(databaseMembers[1].Id, Is.EqualTo(allChannelMembers![1].Id));
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        await usersRepository!.AddRange([allChannelMembers![0], allChannelMembers[1]]);

        var eventArgs = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            differenceCount: 2,
            entitiesDifference: [allChannelMembers![2], allChannelMembers[3]]);

        // Act
        await subscribersChangeDetectorMock!.RaiseAsync(
            d => d.EntitiesChanged += null!,
            subscribersChangeDetectorMock.Object,
            eventArgs);

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.ToList();

        Assert.That(databaseMembers.Count, Is.EqualTo(4));
        for (int i = 0; i < databaseMembers.Count; i++)
            Assert.That(databaseMembers[i].Id, Is.EqualTo(allChannelMembers![i].Id));
    }
}
