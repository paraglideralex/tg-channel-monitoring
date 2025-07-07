using CrystalQuartz.Application;
using CrystalQuartz.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MonitoringBot;
using MonitoringBot.Application.BackgroundJobs;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Services;
using MonitoringBot.Infrastructure.Services.FaultSafety;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;

using Quartz;
using Quartz.Impl;
using Quartz.Simpl;

using System.Runtime;

using Telegram.BotAPI;

//LoggingSetup.SetupLogging();

//var timeProvider = new SystemTimeProvider();

var settingsBuilder = new SettingsBuilder();
settingsBuilder.Build();

//var schedulerFactory = new StdSchedulerFactory();
//var scheduler = await schedulerFactory.GetScheduler();

//var builder = WebApplication.CreateBuilder(args);

//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();

//builder.Services.AddQuartz(q =>
//{
//    q.SchedulerId = settingsBuilder.QuartzSettingsSection!.SchedulerId!;
//    q.SetProperty("quartz.serializer.type", settingsBuilder.QuartzSettingsSection!.SerializerType!);
//    q.SetProperty("quartz.scheduler.instanceName", settingsBuilder.QuartzSettingsSection!.InstanceName!);
//    q.SetProperty("quartz.threadPool.threadCount", settingsBuilder.QuartzSettingsSection!.ThreadCount!);
//});

//builder.Services.AddSingleton(provider =>
//{
//    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
//    var scheduler = schedulerFactory.GetScheduler().Result;
//    return scheduler;
//});

//builder.Services.AddTransient<Biba>();
//builder.Services.AddTransient<Boba>();

//builder.Services.AddDbContext<MonitoringBotDbContextBase>(options =>
//{
//    options.UseNpgsql(settingsBuilder.DatabaseSettingsSection!.BaseConnectionString);
//});

//builder.Services.AddTransient<EventRepository<ChannelMember>, EventsRepositoryImplementation<ChannelMember>>();
//builder.Services.AddTransient<SnapshotRepository,  SnapshotRepositoryImplementation>();
//builder.Services.AddTransient<ITimeProvider, SystemTimeProvider>();
//builder.Services.AddTransient<AggregateSnapshotCreator<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();
//builder.Services.AddTransient<CreateSnapshotCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();
//builder.Services.AddTransient<BaseCommandJob<CreateSnapshotCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>, CreateSnapshotArguments>>();
//builder.Services.AddTransient<CreateSnapshotArguments>();

//builder.Services.AddTransient<EntitiesQuantityCounterBase<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>,
//    EntitiesQuantityCounter<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

//// Web routing for CrystalQuartz
//builder.Services.AddRouting();

//var app = builder.Build();

//// 👇 Важно: routing нужен для CrystalQuartz
//app.UseRouting();

//app.UseCrystalQuartz(app.Services.GetRequiredService<IScheduler>, new CrystalQuartzOptions
//{
//    Path = "/quartz"
//});

//app.MapGet("/", () => "Hello Quartz! Navigate to /quartz");


//// Получаем IScheduler из контейнера
//var scheduler1 = app.Services.GetRequiredService<IScheduler>();


//await ApplyMigrationsIfNeededAsync<MonitoringBotDbContextBase>(app);

//// Запускаем джобу вручную
//await scheduler1.Start();

//var jobStarter = new SnapshotCollectingJobScheduler<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(
//    settingsBuilder.SnapshotCollectingSettingsSection,
//    timeProvider,
//    scheduler1);

//await jobStarter.ScheduleAsync(
//    new CreateSnapshotArguments { AggregateName = settingsBuilder.TelegramApiSettingsSection.ChannelReferenceLink, TimeStamp = timeProvider.UtcNow },
//    new CancellationToken());

////await scheduler1.ScheduleJob(job, trigger);

//var quartzTask = app.RunAsync();

//var dbContext = new DesignTimeDbContextFactory().CreateDbContext([]);



//var userRepository = new UsersRepositoryInMemoryImplementation(dbContext);
//var eventRepository = new EventsRepositoryImplementation<ChannelMember>(dbContext);

//var config = new TelegramConfig(
//    settingsBuilder.TelegramApiSettingsSection!.ApiId,
//    settingsBuilder.TelegramApiSettingsSection.ApiHash,
//    settingsBuilder.TelegramApiSettingsSection.PhoneNumber);

//var retryService = new RetryService();

//var telegramService = new TelegramChannelService(config, 
//    settingsBuilder.TelegramApiSettingsSection.ChannelReferenceLink,
//    retryService,
//    timeProvider);

//var backgroundUserFetchService = new FetchUsersBackgroundService(
//    telegramService,
//    TimeSpan.FromSeconds(settingsBuilder.TelegramBotSettingsSection!.CheckPeriodSeconds));

//var client = new TelegramBotClient(settingsBuilder.TelegramBotSettingsSection!.BotToken);

//var monitoringEngine = new EntitiesChangeDetector<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(timeProvider);
//var getAllQuery = new GetAllCurrentSubscribersQuery(userRepository);

//var addUserCommand = new AddOrUpdateSubscribersCommand(userRepository, timeProvider);
//var deleteUserCommand = new DeleteSubscribersCommand(userRepository);

//var monitoringProcessor = new SubscribersChangeProcessor(addUserCommand);

//var lastPrevious = new GetTimeSpanBetweenLastEventsQueryExecution<ChannelMember>(eventRepository, timeProvider);
//var usersInPeriodQuery = new GetUsersCountForPeriodQueryExecution(userRepository, eventRepository, new UsersCountForPeriodCore());
//var messageBuilder = new MessageBuilder(userRepository, lastPrevious, usersInPeriodQuery, timeProvider);
//var monitoringPresentation = new MonitoringPresentation(messageBuilder);

//var subscribersChangeMessagingService = new EntitiesChangeMessagingService<ChannelMember>(
//    client,
//    settingsBuilder.TelegramBotSettingsSection.ChatIdsCollection);

//var addEventsCommand = new AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(eventRepository);

//var getEventsInPeriodQuery = new GetEventsInPeriodQueryExecution<ChannelMember>(eventRepository);

//var eventsProcessor = new EventsMonitoringProcessor<ChannelMember>(getEventsInPeriodQuery, timeProvider);

//var monitoringService = new SubscribersMonitoringService(
//    backgroundUserFetchService,
//    getAllQuery,
//    monitoringEngine,
//    addEventsCommand,
//    eventsProcessor,
//    settingsBuilder.TelegramApiSettingsSection.ChannelReferenceLink);

//var messageProducer = new SubscribersChangeMessageProducer(monitoringPresentation);

//var snapshotCollectingJobScheduler = new SnapshotCollectingJobScheduler<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(
//    settingsBuilder.SnapshotCollectingSettingsSection!,
//    timeProvider,
//    scheduler);

//var monitoringBotRunner = new MonitoringBotRunner<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>(
//    getAllQuery,
//    subscribersChangeMessagingService,
//    client,
//    backgroundUserFetchService,
//    messageBuilder,
//    monitoringProcessor,
//    monitoringPresentation,
//    settingsBuilder.TelegramBotSettingsSection.ChatIdsCollection,
//    settingsBuilder.TelegramBotSettingsSection.CheckPeriodSeconds,
//    settingsBuilder.TelegramApiSettingsSection.ChannelReferenceLink,
//    eventsProcessor,
//    monitoringService,
//    messageProducer,
//    timeProvider,
//    snapshotCollectingJobScheduler);

//await monitoringBotRunner.InitializeAsync();
//await monitoringBotRunner.MainLoopAsync();
//await quartzTask;

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://localhost:5000");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//builder.WebHost.UseUrls("http://localhost:5000");

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000); // слушать 127.0.0.1:5000
});

LoggingSetup.SetupLogging();

builder.Services.AddMonitoringBotServices(builder.Configuration);
//builder.Services.AddRouting();
var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Запрос: {context.Request.Path}");
    await next();
});

app.UseRouting();

app.UseCrystalQuartz(
    () => app.Services.GetRequiredService<IScheduler>(), 
    new CrystalQuartzOptions
    {
        Path = "/quartz"
    });

app.MapGet("/", () => "Hello Quartz!");


await ApplyMigrationsIfNeededAsync<MonitoringBotDbContextBase>(app);


var addresses = app.Urls;
app.UseCors(c => c.AllowAnyOrigin());
await app.RunAsync();


static async Task ApplyMigrationsIfNeededAsync<T>(WebApplication app) where T : DbContext
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<T>();
    await using (db)
    {
        var pendingMigrations = (await db!.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).ToList();
        if (pendingMigrations.Count > 0)
            await db.Database.MigrateAsync().ConfigureAwait(false);
    }
}