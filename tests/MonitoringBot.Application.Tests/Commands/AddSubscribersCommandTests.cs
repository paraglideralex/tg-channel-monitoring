using MonitoringBot.Application.Commands;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Infrastructure;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests.Commands;
public class AddSubscribersCommandTests
{
    private Mock<UserRepository>? usersRepositoryMock;
    private AddSubscribersCommand? addSubscribersCommand;
    private List<ChannelMember?>? allChannelMembers;

    [SetUp]
    public void SetUp()
    {
        usersRepositoryMock = new Mock<UserRepository>();
        addSubscribersCommand = new AddSubscribersCommand(usersRepositoryMock.Object);
    }

    [Test]
    public async Task Base()
    {
        allChannelMembers =
        [
            new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
        ];
        var result = await addSubscribersCommand!.ExecuteAsync(allChannelMembers!);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task NullMember()
    {
        allChannelMembers = 
        [
            new(1, "1", false, "1", "1", "1", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            null
        ];
        var result = await addSubscribersCommand!.ExecuteAsync(allChannelMembers!);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task EmptyCollection_False()
    {
        allChannelMembers = [];
        var result = await addSubscribersCommand!.ExecuteAsync(allChannelMembers!);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task NullCollection_False()
    {
        allChannelMembers = null;
        var result = await addSubscribersCommand!.ExecuteAsync(allChannelMembers!);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RepositoryException_False()
    {
        usersRepositoryMock!.Setup(x => x.AddRange(It.IsAny<IEnumerable<ChannelMember>>()))
            .Throws(new Exception("exception"));
        var result = await addSubscribersCommand!.ExecuteAsync(allChannelMembers!);

        Assert.That(result, Is.False);
    }
}
