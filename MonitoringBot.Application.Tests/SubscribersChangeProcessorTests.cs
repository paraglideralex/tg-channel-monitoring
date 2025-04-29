using Microsoft.EntityFrameworkCore;

using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure.Persistence;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests;

public class SubscribersChangeProcessorTests
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

        // TODO: репозиторий выводить в интеграционные тесты, здесь просто считать invocations с моими параметрами
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

        var eventArgs = new EntitiesChangedEventArgs<ChannelMember>(
            differenceCount: 1,
            entitiesDifference:[ allChannelMembers![1] ]);

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

        var eventArgs = new EntitiesChangedEventArgs<ChannelMember>(
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

    //[Test]
    //public async Task RangeAddition_Success()
    //{
    //    // Arrange
    //    var existingUsers = new List<ChannelMember>()
    //    { 
    //        new ChannelMember(55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new ChannelMember(77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //    };

    //    await usersRepository!.AddRange(existingUsers);

    //    var newUsers = new List<ChannelMember>()
    //    {
    //        new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
    //        new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
    //    };

    //    var incommingUsers = new List<ChannelMember>();
    //    incommingUsers.AddRange(existingUsers);
    //    incommingUsers.AddRange(newUsers);

    //    await monitoringEngine!.MonitoringStep(incommingUsers);

    //    Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(88));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Last().Id, Is.EqualTo(99));

    //    Assert.That(dbContext!.ChannelMembers.Count(), Is.EqualTo(4));
    //}

    //[Test]
    //public async Task BasicDeletion_Success()
    //{
    //    // Arrange
    //    var existingUsers = new List<ChannelMember>()
    //    {
    //        new ChannelMember(55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new ChannelMember(77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //    };

    //    await usersRepository!.AddRange(existingUsers);

    //    var incommingUsers = new List<ChannelMember>()
    //    {
    //        existingUsers[0]
    //    };

    //    await monitoringEngine!.MonitoringStep(incommingUsers);

    //    Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(-1));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(1));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(77));

    //    Assert.That(dbContext!.ChannelMembers.Count(), Is.EqualTo(1));
    //}

    //[Test]
    //public async Task RangeDeletion_Success()
    //{
    //    // Arrange
    //    var existingUsers = new List<ChannelMember>()
    //    {
    //        new (55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new (77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new (88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
    //        new (99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
    //    };

    //    await usersRepository!.AddRange(existingUsers);

    //    var incommingUsers = new List<ChannelMember>()
    //    {
    //        existingUsers[2],
    //        existingUsers[3],
    //    };

    //    await monitoringEngine!.MonitoringStep(incommingUsers);

    //    Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(-2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(55));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Last().Id, Is.EqualTo(77));

    //    Assert.That(dbContext!.ChannelMembers.Count(), Is.EqualTo(2));
    //}
}
