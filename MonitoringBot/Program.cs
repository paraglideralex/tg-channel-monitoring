using Microsoft.EntityFrameworkCore;

using MonitoringBot;
using MonitoringBot.Application;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Infrastructure.Services.TelegramApi.PollingUpdates;
using MonitoringBot.Infrastructure.Settings;
using MonitoringBot.Presentation;

using Telegram.BotAPI;

LoggingSetup.SetupLogging();

var settingsBuilder = new SettingsBuilder();
settingsBuilder.Build();

var dbContextOptions = new DbContextOptionsBuilder<MonitoringBotDbContextInMemory>()
    .UseInMemoryDatabase("InMemoryDb")
    .Options;

//var dbContext = new MonitoringBotDbContextInMemory(dbContextOptions);

//var userRepository = new UsersRepositoryInMemoryImplementation(dbContext);

var config = new TelegramConfig(
    settingsBuilder.TelegramApiSettings!.ApiId,
    settingsBuilder.TelegramApiSettings.ApiHash,
    settingsBuilder.TelegramApiSettings.PhoneNumber);

//var telegramService = new TelegramChannelService(config, settingsBuilder.TelegramApiSettings.ChannelReferenceLink);

//var backgroundUserFetchService = new FetchUsersBackgroundService(
//    telegramService, 
//    TimeSpan.FromSeconds(settingsBuilder.TelegramBotSettings!.CheckPeriodSeconds));

//var client = new TelegramBotClient(settingsBuilder.TelegramBotSettings!.BotToken);

//var messageBuilder = new MessageBuilder(userRepository);
//var monitoringEngine = new MonitoringEngine(userRepository);
//var monitoringPresentation = new MonitoringPresentation(messageBuilder);

//var monitoringBotRunner = new MonitoringBotRunner(
//    client,
//    backgroundUserFetchService,
//    messageBuilder,
//    monitoringEngine,
//    monitoringPresentation,
//    settingsBuilder.TelegramBotSettings.ChatIdsCollection,
//    settingsBuilder.TelegramBotSettings.CheckPeriodSeconds,
//    settingsBuilder.TelegramApiSettings.ChannelReferenceLink);

//await monitoringBotRunner.InitializeAsync();
//await monitoringBotRunner.MainLoopAsync();


var updatesService = new TelegramApiUpdatesProcessor(config,
    settingsBuilder.TelegramApiSettings.ChannelReferenceLink);

await updatesService.LoginAsync();
await updatesService.RunAsync();

