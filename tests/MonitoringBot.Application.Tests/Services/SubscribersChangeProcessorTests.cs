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
    private Mock<EventsMonitoringProcessor<ChannelMember>>? subscribersChangeDetectorMock;
    private AddSubscribersCommand? addSubscribersCommand;
    private DeleteSubscribersCommand? deleteSubscribersCommand;

    [SetUp]
    public void SetUp()
    {
        allChannelMembers =
        [
            new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new(2, "2", false, "2", "2", "2", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(3, "3", false, "3", "3", "3", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(4, "4", false, "4", "4", "4", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        ];

        usersRepositoryMock = new Mock<UserRepository>();
        addSubscribersCommand = new AddSubscribersCommand(usersRepositoryMock.Object);
        deleteSubscribersCommand = new DeleteSubscribersCommand(usersRepositoryMock.Object);

        monitoringEngine = new SubscribersChangeProcessor(addSubscribersCommand, deleteSubscribersCommand);

        subscribersChangeDetectorMock = new Mock<EventsMonitoringProcessor<ChannelMember>>();
        subscribersChangeDetectorMock.Object.EntitiesJoined += monitoringEngine!.OnSubscribersJoined;
        subscribersChangeDetectorMock.Object.EntitiesLeft += monitoringEngine!.OnSubscribersLeft;
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        var eventArgs = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            differenceCount: 4,
            entitiesDifference: allChannelMembers!);

        // Act
        await subscribersChangeDetectorMock!.RaiseAsync(
            d => d.EntitiesJoined += null!,
            subscribersChangeDetectorMock.Object,
            eventArgs);

        // Assert
        usersRepositoryMock!.Verify(
            repo => repo.AddRange(It.Is<IEnumerable<ChannelMember>>(actual =>
                actual.SequenceEqual(allChannelMembers!))),
            Times.Once()
        );
    }

    [Test]
    public async Task RangeDeletion_Success()
    {
        // Arrange
        var eventArgs = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            differenceCount: -4,
            entitiesDifference: allChannelMembers!);

        // Act
        await subscribersChangeDetectorMock!.RaiseAsync(
            d => d.EntitiesLeft += null!,
            subscribersChangeDetectorMock.Object,
            eventArgs);

        // Assert
        usersRepositoryMock!.Verify(
            repo => repo.DeleteRange(It.Is<IEnumerable<ChannelMember>>(actual =>
                actual.SequenceEqual(allChannelMembers!))),
            Times.Once()
        );
    }
}
