using MonitoringBot.Application.Queries.Events;
using MonitoringBot.CommonTestUtilities;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;
using Moq;
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot.IntegrationTests;
public class QueryMessagesTests : IntegrationTestsBase
{
    private EntitiesChangeMessageProducer<ChannelMember> entitiesChangeMessageProducer;
    private MessageConsumer messageConsumer;
    private GetTimeSpanBetweenLastEventsQueryExecution<ChannelMember> getTimeSpanBetweenLastEventsQueryExecution;
    private GetUsersCountForPeriodQueryExecution getUsersCountForPeriodQueryExecution;
    private MessageBuilder messageBuilder;
    private EventsSequencesExamples eventsSequencesExamples;
    private const string defaultChannel = "DefaultChannel";

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        eventsMonitoringProcessor.EntitiesJoined += subscribersChangeProcessor!.OnSubscribersQuantityChanged;
        eventsMonitoringProcessor.EntitiesLeft += subscribersChangeProcessor!.OnSubscribersQuantityChanged;

        getTimeSpanBetweenLastEventsQueryExecution = new(eventsRepository, timeProviderMock.Object);
        getUsersCountForPeriodQueryExecution = new(usersRepository, eventsRepository, new UsersCountForPeriodCore());
        messageBuilder = new MessageBuilder(usersRepository, getTimeSpanBetweenLastEventsQueryExecution,
            getUsersCountForPeriodQueryExecution, timeProviderMock.Object);

        entitiesChangeMessageProducer = new SubscribersChangeMessageProducer(new MonitoringPresentation(messageBuilder));

        eventsMonitoringProcessor.EntitiesJoined += entitiesChangeMessageProducer.OnEntitiesJoined;
        eventsMonitoringProcessor.EntitiesLeft += entitiesChangeMessageProducer.OnEntitiesLeft;

        eventsSequencesExamples = new();
    }

    [TearDown]
    public override async Task TearDown()
    {
        await base.TearDown();
        messageConsumer?.Messages?.Clear();
        timeProviderMock.Reset();
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

        var lastSubscribedAsync = await messageBuilder.LastSubscribedAsync();
        var lastUnsubscribedAsync = await messageBuilder.LastUnsubscribedAsync();
        var lastAsync = await messageBuilder.LastAsync();

        // Assert
    }


    [Test]
    public async Task JoineddOneOld_Success()
    {
        // Arrange & Act

        var generatedEvent1 = eventsSequencesExamples.CreateJoinDomainEvent(1, defaultChannel, null, new DateTime(2025,1,1,10,0,0));        // 1
        var generatedEvent2 = eventsSequencesExamples.CreateJoinDomainEvent(2, defaultChannel, null, new DateTime(2025, 1, 2, 2, 0, 0));    // 2
        var generatedEvent3 = eventsSequencesExamples.CreateJoinDomainEvent(3, defaultChannel, null, new DateTime(2025, 1, 3, 9, 0, 0));    // 3

        var generatedEvent4 = eventsSequencesExamples.CreateJoinDomainEvent(4, defaultChannel, null, new DateTime(2025, 1, 4, 6, 0, 0));    // 4
        var generatedEvent41 = eventsSequencesExamples.CreateLeftDomainEvent(3, defaultChannel, null, new DateTime(2025, 1, 4, 9, 0, 0));   // 3

        var generatedEvent5 = eventsSequencesExamples.CreateLeftDomainEvent(2, defaultChannel, null, new DateTime(2025, 1, 5, 11, 0, 0));   // 2
        var generatedEvent51 = eventsSequencesExamples.CreateJoinDomainEvent(3, defaultChannel, null, new DateTime(2025, 1, 5, 19, 0, 0));  // 3

        var generatedEvent6 = eventsSequencesExamples.CreateLeftDomainEvent(3, defaultChannel, null, new DateTime(2025, 1, 6, 8, 0, 0));    // 2
        var generatedEvent61 = eventsSequencesExamples.CreateJoinDomainEvent(2, defaultChannel, null, new DateTime(2025, 1, 6, 12, 0, 0));   // 3
        var generatedEvent62 = eventsSequencesExamples.CreateJoinDomainEvent(5, defaultChannel, null, new DateTime(2025, 1, 6, 14, 0, 0));   // 4

        var generatedEvent7 = eventsSequencesExamples.CreateLeftDomainEvent(1, defaultChannel, null, new DateTime(2025, 1, 7, 2, 0, 0));    // 3
        var generatedEvent71 = eventsSequencesExamples.CreateJoinDomainEvent(3, defaultChannel, null, new DateTime(2025, 1, 7, 15, 0, 0));  // 4
        var generatedEvent72 = eventsSequencesExamples.CreateJoinDomainEvent(1, defaultChannel, null, new DateTime(2025, 1, 7, 19, 0, 0));  // 5

        var range = new List<EntitiesChangedDomainEventBase<ChannelMember>>
        {
            generatedEvent1, generatedEvent2, generatedEvent3, generatedEvent4, generatedEvent41, generatedEvent5, generatedEvent51,
            generatedEvent6, generatedEvent61, generatedEvent62, generatedEvent7, generatedEvent71, generatedEvent72
        };

        var channelMember1 = eventsSequencesExamples.CreateChannelMember(1, defaultChannel, nameof(SubscriberJoinedEvent));
        var channelMember3 = eventsSequencesExamples.CreateChannelMember(3, defaultChannel, nameof(SubscriberJoinedEvent));
        var channelMember5 = eventsSequencesExamples.CreateChannelMember(5, defaultChannel, nameof(SubscriberJoinedEvent));
        var channelMember2 = eventsSequencesExamples.CreateChannelMember(2, defaultChannel, nameof(SubscriberJoinedEvent));
        var channelMember4 = eventsSequencesExamples.CreateChannelMember(4, defaultChannel, nameof(SubscriberJoinedEvent));

        await eventsRepository.AddRangeAsync(range);
        await usersRepository.AddRangeAsync(new List<ChannelMember> { channelMember1, channelMember2, channelMember3, channelMember4, channelMember5 },
            nameof(SubscriberJoinedEvent));

        var query = new GetUsersCountForPeriodQueryExecution(usersRepository, eventsRepository, new UsersCountForPeriodCore());

        var message = await messageBuilder.CountHistoryAsync(new DateTime(2025, 1, 7, 20, 0, 0), new DateTime(2025, 1, 1, 9, 0, 0), TimeSpan.FromHours(24));

        var message2 = await messageBuilder.CountHistoryAsync(new DateTime(2025, 1, 7, 20, 0, 0), new DateTime(2024, 12, 1, 9, 0, 0), TimeSpan.FromHours(24));
        // Assert
    }
}
