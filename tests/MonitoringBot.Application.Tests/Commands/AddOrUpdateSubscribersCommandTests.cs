using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.CommonTestUtilities;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests.Commands;
public class AddOrUpdateSubscribersCommandTests
{
    private Mock<UserRepository>? usersRepositoryMock;
    private AddOrUpdateSubscribersCommand? addSubscribersCommand;
    private List<ChannelMember?>? allChannelMembers;
    private const string lastAction = "test action";
    private EventsSequencesExamples eventsSequencesExamples = new();
    private const string baseEventType = "test-base";
    private List<ChannelMember> baseChannelMembers;
    private Mock<ITimeProvider> timeProviderMock;
    private ServiceContext serviceContext;

    [SetUp]
    public void SetUp()
    {
        baseChannelMembers = [new(2, "2", false, "2", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel", "old-event")];
        usersRepositoryMock = new Mock<UserRepository>();
        usersRepositoryMock.Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        timeProviderMock = new Mock<ITimeProvider>();
        timeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        addSubscribersCommand = new AddOrUpdateSubscribersCommand(usersRepositoryMock.Object, timeProviderMock.Object);

        serviceContext = new() {CancellationToken = new CancellationToken()};
    }

    [Test]
    public async Task Add_Success()
    {
        var users = new List<ChannelMember> { new(1, "1", false, "1", "1", "1", new DateTime(2025, 1, 1, 3, 3, 3), new DateTime(2025, 1, 1, 3, 3, 3), "test-channel", baseEventType) };
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            1,
            users,
            baseEventType);

        var result = await addSubscribersCommand!.ExecuteAsync(args, serviceContext);

        Assert.That(result, Is.True);
        usersRepositoryMock!.Verify(
            x => x.AddRangeAsync(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(users)), baseEventType, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task Update_Success()
    {
        var addUser = new ChannelMember(1, "1", false, "1", "1", "1", new DateTime(2025, 1, 1, 3, 3, 3), new DateTime(2025, 1, 1, 3, 3, 3), "test-channel", baseEventType);
        var updateUser = new ChannelMember(2, "1", false, "1", "1", "1", new DateTime(2025, 1, 1, 3, 3, 3), new DateTime(2025, 1, 1, 3, 3, 3), "test-channel", baseEventType);
        var users = new List<ChannelMember>
        {
            addUser,
            updateUser 
        };
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            1,
            users,
            baseEventType);

        usersRepositoryMock.Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseChannelMembers);

        var result = await addSubscribersCommand!.ExecuteAsync(args, serviceContext);

        Assert.That(result, Is.True);
        usersRepositoryMock!.Verify(
            x => x.AddRangeAsync(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(new List<ChannelMember> { addUser })), baseEventType, It.IsAny<CancellationToken>()),
            Times.Once());
        usersRepositoryMock!.Verify(
            x => x.UpdateRangeAsync(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(new List<ChannelMember> { updateUser })), baseEventType, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task AddUpdate_Success()
    {
        var users = new List<ChannelMember> { new(1, "1", false, "1", "1", "1", new DateTime(2025, 1, 1, 3, 3, 3), new DateTime(2025, 1, 1, 3, 3, 3), "test-channel", baseEventType) };
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            1,
            users,
            baseEventType);

        usersRepositoryMock.Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseChannelMembers);

        var result = await addSubscribersCommand!.ExecuteAsync(args, serviceContext);

        Assert.That(result, Is.True);
        usersRepositoryMock!.Verify(
            x => x.AddRangeAsync(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(users)), baseEventType, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task NullMember()
    {
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            1,
            [
                new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel", baseEventType),
                null
            ],
            "test-joined");

        var result = await addSubscribersCommand!.ExecuteAsync(args, serviceContext);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task EmptyCollection_False()
    {
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            1,
            [],
            "test-joined");
        var result = await addSubscribersCommand!.ExecuteAsync(args, serviceContext);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RepositoryException_False()
    {
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
        1,
        [
            new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel", baseEventType),
        ],
        "test-joined");

        usersRepositoryMock!.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ChannelMember>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("exception"));
        var result = await addSubscribersCommand!.ExecuteAsync(args, serviceContext);

        Assert.That(result, Is.False);
    }
}
