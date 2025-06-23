using MonitoringBot.Application.Events;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;

using Moq;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;

public class NotificationMessagesTests : IntegrationTestsBase
{
    private EntitiesChangeMessageProducer<ChannelMember> entitiesChangeMessageProducer;
    private MessageConsumer messageConsumer;
    private GetTimeSpanBetweenLastEventsQueryExecution<ChannelMember> getTimeSpanBetweenLastEventsQueryExecution;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersQuantityChanged;

        getTimeSpanBetweenLastEventsQueryExecution = new(eventsRepository, timeProviderMock.Object);
        entitiesChangeMessageProducer = new SubscribersChangeMessageProducer(
            new MonitoringPresentation(
                new MessageBuilder(usersRepository, getTimeSpanBetweenLastEventsQueryExecution, timeProviderMock.Object)
                ));

        eventsMonitoringProcessor.EntitiesJoined += entitiesChangeMessageProducer.OnEntitiesJoined;
        eventsMonitoringProcessor.EntitiesLeft += entitiesChangeMessageProducer.OnEntitiesLeft;

        messageConsumer = new();
        entitiesChangeMessageProducer.MessageProduced += messageConsumer.OnMessageProduced;
    }

    [TearDown]
    public override async Task TearDown()
    {
        await base.TearDown();
        messageConsumer.Messages.Clear();
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
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(1));

        var message = messageConsumer.Messages[0];
        Assert.That(message, Contains.Substring("Ура, новые подпищщики!"));
        Assert.That(message, Contains.Substring("Последнее действие: Подписка"));
        Assert.That(message, Contains.Substring("Информация от: 01.01.2025 03:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2025 03:03"));
    }

    [Test]
    public async Task JoinedOneOld_Success()
    {
        // Arrange

        // First joins at first time
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns(
            [new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2024, 1, 1, 3, 3, 5), "test-channel", null)]
            );
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // First leaves
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns([]);
        timeProviderMock.Reset();
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 2, 2, 3, 3, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // First joins again

        var fromApi = new List<ChannelMember> 
        {
            new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 3, 25, 3, 3, 5), new DateTime(2025, 3, 25, 3, 3, 5),"test-channel", null)
        };
        timeProviderMock.Reset();
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 3, 25, 3, 3, 5));
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns(fromApi);
        //await usersRepository!.AddAsync(
        //    new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2024, 1, 1, 3, 3, 5), "test-channel", nameof(SubscriberLeftEvent)),
        //    nameof(SubscriberLeftEvent));

        // Act
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(3));

        var message = messageConsumer.Messages[2];
        Assert.That(message, Contains.Substring("Ура, новые подпищщики!"));
        Assert.That(message, Contains.Substring("Последнее действие: Подписка"));
        Assert.That(message, Contains.Substring("Информация от: 25.02.2025 03:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2024 03:03"));
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
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(1));

        var message = messageConsumer.Messages[0];
        Assert.That(message, Contains.Substring("Неееет! От нас свалили!"));
        Assert.That(message, Contains.Substring("Последнее действие: Отписка"));
        Assert.That(message, Contains.Substring("Информация от: 01.01.2025 03:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2025 03:03"));
    }

}

internal class MessageConsumer
{
    public List<string> Messages { get; private set; } = [];

    public async Task OnMessageProduced(object? sender, MessageCreatedEventArgs args)
    {
        await Task.Delay(1);
        Messages.Add(args.Message);
    }
}
