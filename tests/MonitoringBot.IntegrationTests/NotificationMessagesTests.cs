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
        timeProviderMock.Reset();
    }

    [Test]
    public async Task JoinedOneNew_Success()
    {
        // Arrange & Act
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot())
            .Returns([new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2024, 1, 1, 3, 3, 5), "test-channel", null)]);
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(1));

        var message = messageConsumer.Messages[0];
        Assert.That(message, Contains.Substring("Ура, новые подпищщики!"));
        Assert.That(message, Contains.Substring("Последнее действие: Подписка"));
        Assert.That(message, Contains.Substring("Информация от: 01.01.2025 03:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2024 03:03"));
    }

    [Test]
    public async Task JoinedOneOld_Success()
    {
        // Arrange & Act

        // First joins at first time
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot())
            .Returns([new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2024, 1, 1, 3, 3, 5), "test-channel", null)]);
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // First leaves
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns([]);
        timeProviderMock.Reset();
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 2, 2, 9, 1, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // First joins again
        timeProviderMock.Reset();
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 3, 25, 5, 3, 7));
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot())
            .Returns([new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 3, 25, 5, 3, 7), new DateTime(2025, 3, 25, 5, 3, 7), "test-channel", null)]);

        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(3));

        var message = messageConsumer.Messages[2];
        Assert.That(message, Contains.Substring("Ура, новые подпищщики!"));
        Assert.That(message, Contains.Substring("Он снова вернулся к нам после перерыва: 50d, 20h, 2m, 2s"));
        Assert.That(message, Contains.Substring("Последнее действие: Подписка"));
        Assert.That(message, Contains.Substring("Информация от: 25.03.2025 05:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2024 03:03"));
    }

    [Test]
    public async Task TwoLeft_Success()
    {
        // Arrange & Act

        // Two Join
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot())
            .Returns([new(77, "test2", false, "test2", "test2", "79999998889", new DateTime(2025, 1, 1, 3, 3, 5), new DateTime(2024, 1, 1, 3, 3, 5), "test-channel", null),
                      new(88, "test3", false, "test3", "test3", "79999998887", new DateTime(2025, 1, 1, 3, 5, 5), new DateTime(2024, 1, 1, 3, 5, 5), "test-channel", null)]);
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 1, 1, 3, 3, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Two leave
        fetchUsersBackgroundServiceMock.Setup(x => x.GetSnapshot()).Returns([]);
        timeProviderMock.Reset();
        timeProviderMock.Setup(x => x.Now).Returns(new DateTime(2025, 2, 2, 9, 1, 5));
        await subscribersMonitoringService.ProcessMonitoringAsync();

        // Assert
        Assert.That(messageConsumer.Messages.Count, Is.EqualTo(2));

        var message = messageConsumer.Messages[1];
        Assert.That(message, Contains.Substring("Неееет! От нас свалили!"));
        Assert.That(message, Contains.Substring("Пользователь #1:"));
        Assert.That(message, Contains.Substring("Пользователь #2:"));
        Assert.That(message, Contains.Substring("Человека хватило на 32d, 5h, 58m..."));
        Assert.That(message, Contains.Substring("ID: 77"));
        Assert.That(message, Contains.Substring("ID: 88"));
        Assert.That(message, Contains.Substring("Последнее действие: Отписка"));
        Assert.That(message, Contains.Substring("Информация от: 01.01.2025 03:03"));
        Assert.That(message, Contains.Substring("Впервые зарегистрирован: 01.01.2024 03:05"));
    }

}

internal class MessageConsumer
{
    public List<string> Messages { get; private set; } = [];

    public Task OnMessageProduced(object? sender, MessageCreatedEventArgs args)
    {
        Messages.Add(args.Message);
        return Task.CompletedTask;
    }
}
