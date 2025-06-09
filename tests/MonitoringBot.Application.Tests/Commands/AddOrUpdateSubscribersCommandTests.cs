using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Events;
using MonitoringBot.CommonTestUtilities;
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

    [SetUp]
    public void SetUp()
    {
        baseChannelMembers = [new(2, "2", false, "2", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3),"test-channel", "old-event")];
        usersRepositoryMock = new Mock<UserRepository>();
        usersRepositoryMock.Setup(x => x.FindByIds(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync([]);
        addSubscribersCommand = new AddOrUpdateSubscribersCommand(usersRepositoryMock.Object);
    }

    [Test]
    public async Task Add_Success()
    {
        var users = new List<ChannelMember> { new(1, "1", false, "1", "1", "1", new DateTime(2025, 1, 1, 3, 3, 3), new DateTime(2025, 1, 1, 3, 3, 3), "test-channel", baseEventType) };
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            1,
            users,
            baseEventType);

        var result = await addSubscribersCommand!.ExecuteAsync(args);

        Assert.That(result, Is.True);
        usersRepositoryMock!.Verify(
            x => x.AddRange(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(users)), baseEventType),
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

        usersRepositoryMock.Setup(x => x.FindByIds(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(baseChannelMembers);

        var result = await addSubscribersCommand!.ExecuteAsync(args);

        Assert.That(result, Is.True);
        usersRepositoryMock!.Verify(
            x => x.AddRange(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(new List<ChannelMember> { addUser })), baseEventType),
            Times.Once());
        usersRepositoryMock!.Verify(
            x => x.UpdateRangeAsync(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(new List<ChannelMember> { updateUser })), baseEventType),
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

        usersRepositoryMock.Setup(x => x.FindByIds(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(baseChannelMembers);

        var result = await addSubscribersCommand!.ExecuteAsync(args);

        Assert.That(result, Is.True);
        usersRepositoryMock!.Verify(
            x => x.AddRange(It.Is<IEnumerable<ChannelMember>>(a => a.SequenceEqual(users)), baseEventType),
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

        var result = await addSubscribersCommand!.ExecuteAsync(args);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task EmptyCollection_False()
    {
        var args = new EntitiesCollectionChangedEventArgs<ChannelMember>(
            1,
            [],
            "test-joined");
        var result = await addSubscribersCommand!.ExecuteAsync(args);

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

        usersRepositoryMock!.Setup(x => x.AddRange(It.IsAny<IEnumerable<ChannelMember>>(), It.IsAny<string>()))
            .Throws(new Exception("exception"));
        var result = await addSubscribersCommand!.ExecuteAsync(args);

        Assert.That(result, Is.False);
    }
}
