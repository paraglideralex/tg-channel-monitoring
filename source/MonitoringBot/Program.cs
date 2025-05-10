using Microsoft.EntityFrameworkCore;

using MonitoringBot;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Services.FaultSafety;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;

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
var monitoringEngine = new EntitiesChangeDetector<ChannelMember>();
var getAllQuery = new GetAllCurrentSubscribersQuery(userRepository);

var addUserCommand = new AddSubscribersCommand(userRepository);
var deleteUserCommand = new DeleteSubscribersCommand(userRepository);

var monitoringProcessor = new SubscribersChangeProcessor(addUserCommand, deleteUserCommand);
var monitoringPresentation = new MonitoringPresentation(messageBuilder);

var subscribersChangeMessagingService = new SubscribersChangeMessageService(
    client,
    settingsBuilder.TelegramBotSettings.ChatIdsCollection,
    monitoringPresentation);

var monitoringBotRunner = new MonitoringBotRunner(
    monitoringEngine,
    getAllQuery,
    subscribersChangeMessagingService,
    client,
    backgroundUserFetchService,
    messageBuilder,
    monitoringProcessor,
    monitoringPresentation,
    settingsBuilder.TelegramBotSettings.ChatIdsCollection,
    settingsBuilder.TelegramBotSettings.CheckPeriodSeconds,
    settingsBuilder.TelegramApiSettings.ChannelReferenceLink);

await monitoringBotRunner.InitializeAsync();
await monitoringBotRunner.MainLoopAsync();


