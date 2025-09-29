using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Infrastructure;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests.Services;

public class SubscribersChangeProcessorTests
{
    private Mock<UserRepository>? usersRepositoryMock;
    private SubscribersChangeProcessor? monitoringEngine;
    private List<ChannelMember>? allChannelMembers;
    private Mock<IEventsMonitoringProcessor<ChannelMember>>? eventsMonitoringProcessorMock;
    private AddOrUpdateSubscribersCommand? addOrUpdateSubscribersCommand;
    private const string eventName = nameof(SubscriberJoinedEvent);
    private Mock<ITimeProvider> timeProviderMock;
    private ServiceContext serviceContext;

    [SetUp]
    public void SetUp()
    {
        allChannelMembers =
        [
            new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel",eventName),
            new(2, "2", false, "2", "2", "2", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel",eventName),
            new(3, "3", false, "3", "3", "3", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel",eventName),
            new(4, "4", false, "4", "4", "4", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5),"test-channel",eventName)
        ];

        usersRepositoryMock = new Mock<UserRepository>();
        timeProviderMock = new Mock<ITimeProvider>();
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 1, 1));

        addOrUpdateSubscribersCommand = new AddOrUpdateSubscribersCommand(usersRepositoryMock.Object, timeProviderMock.Object);

        monitoringEngine = new SubscribersChangeProcessor(addOrUpdateSubscribersCommand);

        eventsMonitoringProcessorMock = new Mock<IEventsMonitoringProcessor<ChannelMember>>();
        eventsMonitoringProcessorMock.Object.EntitiesJoined += monitoringEngine!.OnSubscribersQuantityChanged;
        eventsMonitoringProcessorMock.Object.EntitiesLeft += monitoringEngine!.OnSubscribersQuantityChanged;

        serviceContext = new() { CancellationToken = CancellationToken.None };
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        var eventArgs = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            differenceCount: 4,
            entitiesDifference: allChannelMembers!,
            eventName);

        // Act
        await eventsMonitoringProcessorMock!.RaiseAsync(
            d => d.EntitiesJoined += null!,
            eventsMonitoringProcessorMock.Object,
            eventArgs,
            serviceContext);

        // Assert
        usersRepositoryMock!.Verify(
            repo => repo.AddRangeAsync(It.Is<IEnumerable<ChannelMember>>(actual =>
                actual.SequenceEqual(allChannelMembers!)), eventName, It.IsAny<CancellationToken>()),
            Times.Once()
        );
    }

    [Test]
    public async Task RangeDeletion_Success()
    {
        // Arrange
        var eventArgs = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            differenceCount: -4,
            entitiesDifference: allChannelMembers!,
            eventName);

        usersRepositoryMock.Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allChannelMembers);

        // Act
        await eventsMonitoringProcessorMock!.RaiseAsync(
            d => d.EntitiesLeft += null!,
            eventsMonitoringProcessorMock.Object,
            eventArgs,
            serviceContext);

        // Assert
        usersRepositoryMock!.Verify(
            repo => repo.UpdateRangeAsync(It.Is<IEnumerable<ChannelMember>>(actual =>
                actual.SequenceEqual(allChannelMembers!)), eventName, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once()
        );
    }
}
