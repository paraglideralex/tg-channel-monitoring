using Microsoft.EntityFrameworkCore;

using MonitoringBot;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Services.FaultSafety;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;
using MonitoringBot.Application.Abstractions;

using Telegram.BotAPI;
using MonitoringBot.Infrastructure.Services;

LoggingSetup.SetupLogging();

var timeProvider = new SystemTimeProvider();

var settingsBuilder = new SettingsBuilder();
settingsBuilder.Build();

var dbContextOptions = new DbContextOptionsBuilder<MonitoringBotDbContextInMemory>()
    .UseInMemoryDatabase("InMemoryDb")
    .Options;

var dbContext = new MonitoringBotDbContextInMemory(dbContextOptions);

var userRepository = new UsersRepositoryInMemoryImplementation(dbContext);
var eventRepository = new EventsRepositoryImplementation<ChannelMember>(dbContext);

var config = new TelegramConfig(
    settingsBuilder.TelegramApiSettings!.ApiId,
    settingsBuilder.TelegramApiSettings.ApiHash,
    settingsBuilder.TelegramApiSettings.PhoneNumber);

var retryService = new RetryService();

var telegramService = new TelegramChannelService(config, 
    settingsBuilder.TelegramApiSettings.ChannelReferenceLink,
    retryService,
    timeProvider);

var backgroundUserFetchService = new FetchUsersBackgroundService(
    telegramService,
    TimeSpan.FromSeconds(settingsBuilder.TelegramBotSettings!.CheckPeriodSeconds));

var client = new TelegramBotClient(settingsBuilder.TelegramBotSettings!.BotToken);

var monitoringEngine = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(timeProvider);
var getAllQuery = new GetAllCurrentSubscribersQuery(userRepository);

var addUserCommand = new AddOrUpdateSubscribersCommand(userRepository, timeProvider);
var deleteUserCommand = new DeleteSubscribersCommand(userRepository);

var monitoringProcessor = new SubscribersChangeProcessor(addUserCommand);

var lastPrevious = new GetTimeSpanBetweenLastEventsQueryExecution<ChannelMember>(eventRepository, timeProvider);
var messageBuilder = new MessageBuilder(userRepository, lastPrevious, timeProvider);
var monitoringPresentation = new MonitoringPresentation(messageBuilder);

var subscribersChangeMessagingService = new EntitiesChangeMessagingService<ChannelMember>(
    client,
    settingsBuilder.TelegramBotSettings.ChatIdsCollection);

var addEventsCommand = new AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(eventRepository);

var getEventsInPeriodQuery = new GetEventsInPeriodQueryExecution<ChannelMember>(eventRepository);

var eventsProcessor = new EventsMonitoringProcessor<ChannelMember>(getEventsInPeriodQuery, timeProvider);

var monitoringService = new SubscribersMonitoringService(
    backgroundUserFetchService,
    getAllQuery,
    monitoringEngine,
    addEventsCommand,
    eventsProcessor,
    settingsBuilder.TelegramApiSettings.ChannelReferenceLink);

var messageProducer = new SubscribersChangeMessageProducer(monitoringPresentation);

var monitoringBotRunner = new MonitoringBotRunner<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(
    getAllQuery,
    subscribersChangeMessagingService,
    client,
    backgroundUserFetchService,
    messageBuilder,
    monitoringProcessor,
    monitoringPresentation,
    settingsBuilder.TelegramBotSettings.ChatIdsCollection,
    settingsBuilder.TelegramBotSettings.CheckPeriodSeconds,
    settingsBuilder.TelegramApiSettings.ChannelReferenceLink,
    eventsProcessor,
    monitoringService,
    messageProducer,
    timeProvider);

await monitoringBotRunner.InitializeAsync();
await monitoringBotRunner.MainLoopAsync();


