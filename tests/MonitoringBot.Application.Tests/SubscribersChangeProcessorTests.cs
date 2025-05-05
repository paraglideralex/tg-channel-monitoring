using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests;

public class SubscribersChangeProcessorTests
{
    private Mock<UserRepository>? usersRepositoryMock;
    private SubscribersChangeProcessor? monitoringEngine;
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

        usersRepositoryMock = new Mock<UserRepository>();
        monitoringEngine = new SubscribersChangeProcessor(usersRepositoryMock.Object);

        subscribersChangeDetectorMock = new Mock<IEntitiesChangeDetector<ChannelMember>>();
        subscribersChangeDetectorMock.Object.EntitiesChanged += monitoringEngine!.OnSubscribersChanged;
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        var eventArgs = new EntitiesChangedEventArgs<ChannelMember>(
            differenceCount: 4,
            entitiesDifference: allChannelMembers!);

        // Act
        await subscribersChangeDetectorMock!.RaiseAsync(
            d => d.EntitiesChanged += null!,
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
        var eventArgs = new EntitiesChangedEventArgs<ChannelMember>(
            differenceCount: -4,
            entitiesDifference: allChannelMembers!);

        // Act
        await subscribersChangeDetectorMock!.RaiseAsync(
            d => d.EntitiesChanged += null!,
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
