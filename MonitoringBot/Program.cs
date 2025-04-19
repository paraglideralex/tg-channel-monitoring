using Microsoft.EntityFrameworkCore;

using MonitoringBot;
using MonitoringBot.Application;
using MonitoringBot.Infrastructure.Persistence;
using MonitoringBot.Presentation;

using Telegram.BotAPI;

using TgChannelApi;

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

var client = new TelegramBotClient(settingsBuilder.TelegramBotSettings!.BotToken);

var messageBuilder = new MessageBuilder(userRepository);
var monitoringEngine = new MonitoringEngine(userRepository);
var monitoringPresentation = new MonitoringPresentation(messageBuilder);

var monitoringBotRunner = new MonitoringBotRunner(
    client,
    telegramService,
    messageBuilder,
    monitoringEngine,
    monitoringPresentation,
    settingsBuilder.TelegramBotSettings.ChatIdsCollection,
    settingsBuilder.TelegramBotSettings.CheckPeriodSeconds,
    settingsBuilder.TelegramApiSettings.ChannelReferenceLink);

await monitoringBotRunner.InitializeAsync();
await monitoringBotRunner.MainLoopAsync();

