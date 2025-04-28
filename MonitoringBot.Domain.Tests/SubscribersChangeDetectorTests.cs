using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Services;

using NUnit.Framework;

namespace MonitoringBot.Domain.Tests;

public class SubscribersChangeDetectorTests
{
    private SubscribersChangeDetector? subscribersChangeDetector;
    private ProcessorSubscriber? subscriber;

    [SetUp]
    public void SetUp()
    {
        subscribersChangeDetector = new SubscribersChangeDetector();
        subscriber = new ProcessorSubscriber();
        subscribersChangeDetector.SubscribersChanged += subscriber.OnEvent;
    }

    [Test]
    public async Task BasicAddition_Success()
    {
        // Arrange
        var fromRepository = new List<ChannelMember>()
        { 
            new ChannelMember(55, "test1", false, "test1", "test1", "79999998888", new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)) 
        };


        var fromApi = new List<ChannelMember>()
        {
            new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
        };

        await subscribersChangeDetector!.ExecuteMonitoring(fromApi, fromRepository);

        Assert.That(subscriber!.EventArgs?.DifferenceCount, Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.MemberDifference.Count(), Is.EqualTo(1));
        Assert.That(subscriber!.EventArgs?.MemberDifference.First().Id, Is.EqualTo(77));
    }

    //[Test]
    //public async Task RangeAddition_Success()
    //{
    //    // Arrange
    //    var existingUsers = new List<ChannelMember>()
    //    { 
    //        new ChannelMember(55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new ChannelMember(77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //    };

    //    await usersRepository!.AddRange(existingUsers);

    //    var newUsers = new List<ChannelMember>()
    //    {
    //        new(88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
    //        new(99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
    //    };

    //    var incommingUsers = new List<ChannelMember>();
    //    incommingUsers.AddRange(existingUsers);
    //    incommingUsers.AddRange(newUsers);

    //    await monitoringEngine!.MonitoringStep(incommingUsers);

    //    Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(88));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Last().Id, Is.EqualTo(99));

    //    Assert.That(dbContext!.ChannelMembers.Count(), Is.EqualTo(4));
    //}

    //[Test]
    //public async Task BasicDeletion_Success()
    //{
    //    // Arrange
    //    var existingUsers = new List<ChannelMember>()
    //    {
    //        new ChannelMember(55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new ChannelMember(77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //    };

    //    await usersRepository!.AddRange(existingUsers);

    //    var incommingUsers = new List<ChannelMember>()
    //    {
    //        existingUsers[0]
    //    };

    //    await monitoringEngine!.MonitoringStep(incommingUsers);

    //    Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(-1));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(1));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(77));

    //    Assert.That(dbContext!.ChannelMembers.Count(), Is.EqualTo(1));
    //}

    //[Test]
    //public async Task RangeDeletion_Success()
    //{
    //    // Arrange
    //    var existingUsers = new List<ChannelMember>()
    //    {
    //        new (55, "test1", false, "test1", "test1", "79999998881",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new (77, "test2", false, "test2", "test2", "79999998882",new DateTime(2025,1,1,3,3,3), new DateTime(2025,1,1,3,3,3)),
    //        new (88, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5)),
    //        new (99, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2025, 1, 1, 3, 3, 5))
    //    };

    //    await usersRepository!.AddRange(existingUsers);

    //    var incommingUsers = new List<ChannelMember>()
    //    {
    //        existingUsers[2],
    //        existingUsers[3],
    //    };

    //    await monitoringEngine!.MonitoringStep(incommingUsers);

    //    Assert.That(monitoringEngine.CurrentStepDifferenceCount, Is.EqualTo(-2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Count(), Is.EqualTo(2));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.First().Id, Is.EqualTo(55));
    //    Assert.That(monitoringEngine.CurrentStepMemberDifference.Last().Id, Is.EqualTo(77));

    //    Assert.That(dbContext!.ChannelMembers.Count(), Is.EqualTo(2));
    //}

}

internal sealed class ProcessorSubscriber
{
    public SubscribersChangedEventArgs? EventArgs { get; private set; }
    public async Task OnEvent(object? sender, SubscribersChangedEventArgs e)
    {
        EventArgs = e;
    }
}