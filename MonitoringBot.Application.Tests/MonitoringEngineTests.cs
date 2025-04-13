using Microsoft.EntityFrameworkCore;

using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Persistense;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.Application.Tests;

public class MonitoringEngineTests
{
    private UsersRepositoryInMemoryImplementation usersRepository;
    private MonitoringEngine monitoringEngine;
    private MonitoringBotDbContextBase dbContext;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MonitoringBotDbContextInMemory>()
            .UseInMemoryDatabase("InMemoryDb")
            .Options;
        dbContext = new MonitoringBotDbContextInMemory(options);

        usersRepository = new UsersRepositoryInMemoryImplementation(dbContext);
        monitoringEngine = new MonitoringEngine(usersRepository);
    }

    [Test]
    public async Task BasicAddition_Success()
    {
        // Arrange
        var existingUsers = new List<ChannelMember>() { new ChannelMember(55, "test1", false, "test1", "test1", "79999998888",
            new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)) };
        
        await usersRepository.AddRange(existingUsers);

        var newUsers = new List<ChannelMember>()
        {
            new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        };

        var incommingUsers = new List<ChannelMember>()
        {
            existingUsers[0],
            newUsers[0]
        };

        await monitoringEngine.MonitoringStep(incommingUsers);

        Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(1));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(1));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(77));

        Assert.That(dbContext.ChannelMembers.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task RangeAddition_Success()
    {
        // Arrange
        var existingUsers = new List<ChannelMember>()
        { 
            new ChannelMember(55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new ChannelMember(77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
        };

        await usersRepository.AddRange(existingUsers);

        var newUsers = new List<ChannelMember>()
        {
            new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        };

        var incommingUsers = new List<ChannelMember>();
        incommingUsers.AddRange(existingUsers);
        incommingUsers.AddRange(newUsers);

        await monitoringEngine.MonitoringStep(incommingUsers);

        Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(2));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(2));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(88));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.Last().Id, Is.EqualTo(99));

        Assert.That(dbContext.ChannelMembers.Count(), Is.EqualTo(4));
    }

    [Test]
    public async Task BasicDeletion_Success()
    {
        // Arrange
        // Arrange
        var existingUsers = new List<ChannelMember>()
        {
            new ChannelMember(55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new ChannelMember(77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
        };

        await usersRepository.AddRange(existingUsers);

        var incommingUsers = new List<ChannelMember>()
        {
            existingUsers[0]
        };

        await monitoringEngine.MonitoringStep(incommingUsers);

        Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(-1));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(1));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(77));

        Assert.That(dbContext.ChannelMembers.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task RangeDeletion_Success()
    {
        // Arrange
        // Arrange
        var existingUsers = new List<ChannelMember>()
        {
            new (55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new (77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
            new (88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
            new (99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        };

        await usersRepository.AddRange(existingUsers);

        var incommingUsers = new List<ChannelMember>()
        {
            existingUsers[2],
            existingUsers[3],
        };

        await monitoringEngine.MonitoringStep(incommingUsers);

        Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(-2));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(2));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(55));
        Assert.That(monitoringEngine.CurrentStepMemberDifference.Last().Id, Is.EqualTo(77));

        Assert.That(dbContext.ChannelMembers.Count(), Is.EqualTo(2));
    }
}
