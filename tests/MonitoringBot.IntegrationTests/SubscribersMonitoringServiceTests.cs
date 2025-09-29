using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure.Persistence.Entities;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;

public class SubscribersMonitoringServiceTests : IntegrationTestsBase
{

    [SetUp]
    public override async Task SetUpAsync()
    {
        await base.SetUpAsync();
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 1, 1));

        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
    }

    [TearDown]
    public override async Task TearDownAsync()
    {
        await base.TearDownAsync();
    }

    [Test]
    public async Task JoinedOneNew_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![0], allChannelMembers[1] };
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();
        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);

        fetchUsersBackgroundServiceMock.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([fromApi[1]]);

        await usersRepository!.AddRangeAsync([allChannelMembers![0]], nameof(SubscriberJoinedEvent), cancellationToken);

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        fetchUsersBackgroundServiceMock.Verify(
            x => x.GetByIdsAsync(
                new List<long> { identitiesFromApi[1] }, 
                It.IsAny<CancellationToken>()), 
            Times.Once);

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
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();
        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);

        fetchUsersBackgroundServiceMock.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([fromApi[0]]);

        await usersRepository!.AddRangeAsync([allChannelMembers![1]], nameof(SubscriberLeftEvent), cancellationToken);

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        fetchUsersBackgroundServiceMock.Verify(
            x => x.GetByIdsAsync(
                new List<long> { identitiesFromApi[0] },
                It.IsAny<CancellationToken>()),
            Times.Once);

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
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();

        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);

        await usersRepository!.AddRangeAsync([allChannelMembers![0], allChannelMembers[1]], nameof(SubscriberJoinedEvent), cancellationToken);

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        fetchUsersBackgroundServiceMock.Verify(
            x => x.GetByIdsAsync(
                It.IsAny<List<long>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(allChannelMembers[0..2]));
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].EntityIdProjection, Is.EqualTo(allChannelMembers[1].Id.ToString()));
        Assert.That(events[0].EventType, Is.EqualTo(nameof(SubscriberLeftEvent)));
    }

    ///// <summary>
    ///// Исходно были 0 и 1. За один цикл номер 0 ушёл, а номер 2 пришёл.
    ///// </summary>
    ///// <returns></returns>
    [Test]
    public async Task OneJoinedOneLeft_Success()
    {
        // Arrange
        var fromApi = new List<ChannelMember> { allChannelMembers![1], allChannelMembers[2] };
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();

        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);
        fetchUsersBackgroundServiceMock.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([fromApi[1]]);

        await usersRepository!.AddRangeAsync([allChannelMembers![0], allChannelMembers[1]], nameof(SubscriberJoinedEvent), cancellationToken);

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        fetchUsersBackgroundServiceMock.Verify(
            x => x.GetByIdsAsync(
                new List<long> { identitiesFromApi[1] },
                It.IsAny<CancellationToken>()),
            Times.Once);

        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(allChannelMembers[0..3]));
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
        var fromApi = new List<ChannelMember> { allChannelMembers![1], allChannelMembers[2] };
        var identitiesFromApi = fromApi.Select(i => i.Id).ToList();

        fetchUsersBackgroundServiceMock.Setup(x => x.GetExistingIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(identitiesFromApi);

        await usersRepository!.AddRangeAsync([allChannelMembers![1], allChannelMembers[2]], nameof(SubscriberJoinedEvent), cancellationToken);

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync(cancellationToken);

        // Assert
        fetchUsersBackgroundServiceMock.Verify(
            x => x.GetByIdsAsync(
                It.IsAny<List<long>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
        Assert.That(events.Count, Is.EqualTo(0));
    }
}
