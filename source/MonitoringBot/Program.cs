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

LoggingSetup.SetupLogging();

var settingsBuilder = new SettingsBuilder();
settingsBuilder.Build();

var dbContextOptions = new DbContextOptionsBuilder<MonitoringBotDbContextInMemory>()
    .UseInMemoryDatabase("InMemoryDb")
    .Options;

var dbContext = new MonitoringBotDbContextInMemory(dbContextOptions);

var userRepository = new UsersRepositoryInMemoryImplementation(dbContext);

var config = new TelegramConfig(
    settingsBuilder.TelegramApiSettings!.ApiId,
    settingsBuilder.TelegramApiSettings.ApiHash,
    settingsBuilder.TelegramApiSettings.PhoneNumber);

var retryService = new RetryService();

var telegramService = new TelegramChannelService(config, 
    settingsBuilder.TelegramApiSettings.ChannelReferenceLink, retryService);

var backgroundUserFetchService = new FetchUsersBackgroundService(
    telegramService,
    TimeSpan.FromSeconds(settingsBuilder.TelegramBotSettings!.CheckPeriodSeconds));

var client = new TelegramBotClient(settingsBuilder.TelegramBotSettings!.BotToken);

var messageBuilder = new MessageBuilder(userRepository);
var monitoringEngine = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>();
var getAllQuery = new GetAllCurrentSubscribersQuery(userRepository);

var addUserCommand = new AddOrUpdateSubscribersCommand(userRepository);
var deleteUserCommand = new DeleteSubscribersCommand(userRepository);

var monitoringProcessor = new SubscribersChangeProcessor(addUserCommand);
var monitoringPresentation = new MonitoringPresentation(messageBuilder);

var subscribersChangeMessagingService = new EntitiesChangeMessagingService<ChannelMember>(
    client,
    settingsBuilder.TelegramBotSettings.ChatIdsCollection);

var eventRepository = new EventsRepositoryImplementation<ChannelMember>(dbContext);

var addEventsCommand = new AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(eventRepository);

var getEventsInPeriodQuery = new GetEventsInPeriodQueryExecution<ChannelMember>(eventRepository);

var eventsProcessor = new EventsMonitoringProcessor<ChannelMember>(getEventsInPeriodQuery);

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
    messageProducer);

await monitoringBotRunner.InitializeAsync();
await monitoringBotRunner.MainLoopAsync();


