using Microsoft.EntityFrameworkCore;

using MonitoringBot;
using MonitoringBot.Application;
using MonitoringBot.Application.Queries;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence;
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

var telegramService = new TelegramChannelService(config, settingsBuilder.TelegramApiSettings.ChannelReferenceLink);

var backgroundUserFetchService = new FetchUsersBackgroundService(
    telegramService,
    TimeSpan.FromSeconds(settingsBuilder.TelegramBotSettings!.CheckPeriodSeconds));

var client = new TelegramBotClient(settingsBuilder.TelegramBotSettings!.BotToken);

var messageBuilder = new MessageBuilder(userRepository);
var monitoringEngine = new SubscribersChangeDetector();
var getAllQuery = new GetAllCurrentSubscribersQuery(userRepository);



var monitoringProcessor = new SubscribersChangeProcessor(userRepository);
var monitoringPresentation = new MonitoringPresentation(messageBuilder);

var messagesService = new MessagesSendingService(client, monitoringPresentation, settingsBuilder.TelegramBotSettings.ChatIdsCollection);

var monitoringBotRunner = new MonitoringBotRunner(
    monitoringEngine,
    getAllQuery,
    messagesService,
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


