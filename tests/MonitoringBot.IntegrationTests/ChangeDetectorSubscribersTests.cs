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

public class ChangeDetectorSubscribersTests : IntegrationTestsBase
{

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 1, 1));

        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
    }

    [TearDown]
    public override async Task TearDown()
    {
        await base.TearDown();
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
        await usersRepository!.AddRangeAsync([allChannelMembers![0], allChannelMembers[1]], nameof(SubscriberJoinedEvent));

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
        await usersRepository!.AddRangeAsync([allChannelMembers![0], allChannelMembers[1]], nameof(SubscriberJoinedEvent));

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
        await usersRepository!.AddRangeAsync([allChannelMembers![1], allChannelMembers[2]], nameof(SubscriberJoinedEvent));

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        var databaseMembers = dbContext!.ChannelMembers.Select(x => x.ToDomain()).ToList();
        var events = dbContext!.Events.ToList();

        Assert.That(databaseMembers, Is.EquivalentTo(fromApi));
        Assert.That(events.Count, Is.EqualTo(0));
    }
}
