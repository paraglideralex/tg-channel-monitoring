using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
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
    private AddOrUpdateSubscribersCommand? addSubscribersCommand;
    private DeleteSubscribersCommand? deleteSubscribersCommand;
    private const string eventName = "test-event";

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
        addSubscribersCommand = new AddOrUpdateSubscribersCommand(usersRepositoryMock.Object);
        deleteSubscribersCommand = new DeleteSubscribersCommand(usersRepositoryMock.Object);

        monitoringEngine = new SubscribersChangeProcessor(addSubscribersCommand, deleteSubscribersCommand);

        eventsMonitoringProcessorMock = new Mock<IEventsMonitoringProcessor<ChannelMember>>();
        eventsMonitoringProcessorMock.Object.EntitiesJoined += monitoringEngine!.OnSubscribersJoined;
        eventsMonitoringProcessorMock.Object.EntitiesLeft += monitoringEngine!.OnSubscribersLeft;
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        var eventArgs = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            differenceCount: 4,
            entitiesDifference: allChannelMembers!,
            "test-event");

        // Act
        await eventsMonitoringProcessorMock!.RaiseAsync(
            d => d.EntitiesJoined += null!,
            eventsMonitoringProcessorMock.Object,
            eventArgs);

        // Assert
        usersRepositoryMock!.Verify(
            repo => repo.AddRange(It.Is<IEnumerable<ChannelMember>>(actual =>
                actual.SequenceEqual(allChannelMembers!)), eventName),
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

        // Act
        await eventsMonitoringProcessorMock!.RaiseAsync(
            d => d.EntitiesLeft += null!,
            eventsMonitoringProcessorMock.Object,
            eventArgs);

        // Assert
        usersRepositoryMock!.Verify(
            repo => repo.DeleteRange(It.Is<IEnumerable<ChannelMember>>(actual =>
                actual.SequenceEqual(allChannelMembers!))),
            Times.Once()
        );
    }
}
