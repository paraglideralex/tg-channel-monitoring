using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using MonitoringBot.Application.Abstractions;
using MonitoringBot.Application.BackgroundJobs;
using MonitoringBot.Application.Commands;
using MonitoringBot.Application.Queries.BotUsers;
using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Application.Queries.Projections;
using MonitoringBot.Application.Queries.Snapshots;
using MonitoringBot.Application.Services;
using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.RepositoriesAbstarctions;
using MonitoringBot.Domain.Services;
using MonitoringBot.Infrastructure;
using MonitoringBot.Infrastructure.Caching;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Services;
using MonitoringBot.Infrastructure.Services.FaultSafety;
using MonitoringBot.Infrastructure.Services.TelegramApi.Data;
using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;
using MonitoringBot.Presentation;
using MonitoringBot.Services.MessagesSending;

using Quartz;

using Telegram.BotAPI;

namespace MonitoringBot;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonitoringBotServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        // Build settings
        var settingsBuilder = new SettingsBuilder();
        settingsBuilder.Build();

        services.AddSingleton(settingsBuilder);
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        // Telegram settings
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            return new TelegramConfig(
                settings.TelegramApiSettingsSection.ApiId,
                settings.TelegramApiSettingsSection.ApiHash,
                settings.TelegramApiSettingsSection.PhoneNumber);
        });

        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            return settingsBuilder.TelegramBotSettingsSection;
        });

        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            return settingsBuilder.TelegramApiSettingsSection;
        });

        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            return settingsBuilder.DatabaseSettingsSection;
        });

        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            return settingsBuilder.QuartzSettingsSection;
        });

        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            return new TelegramBotClient(settings.TelegramBotSettingsSection.BotToken);
        });

        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            return settingsBuilder.SnapshotCollectingSettingsSection;
        });

        services.AddSingleton<MonitoringCancellationContext>();
        services.AddSingleton<IMemoryCache, MemoryCache>();
        services.AddSingleton<CacheKeysFactory>();

        // Retry service
        services.AddScoped<RetryServiceBase, RetryService>();

        // TelegramChannelService
        services.AddScoped<TelegramChannelService>();
        services.AddScoped<FetchUsersBackgroundServiceBase, FetchUsersBackgroundService>();

        services.AddScoped<IEventsMonitoringProcessor<ChannelMember>, EventsMonitoringProcessor<ChannelMember>>();

        // DB Context
        services.AddDbContext<MonitoringBotDbContextBase>(options =>
        {
            options.UseNpgsql(settingsBuilder.DatabaseSettingsSection.BaseConnectionString);

        });

        services.AddDbContextFactory<MonitoringBotDbContextBase>(options =>
        {
            options.UseNpgsql(settingsBuilder.DatabaseSettingsSection.BaseConnectionString);
        },
        lifetime: ServiceLifetime.Scoped);

        services.AddTransient<EventRepository<ChannelMember>, EventsRepositoryImplementation<ChannelMember>>();
        services.AddTransient<SnapshotRepository, SnapshotRepositoryImplementation>();
        services.AddTransient<UserRepository, UsersRepositoryImplementation>();
        services.AddTransient<ApiUsersRepository,  ApiUsersRepositoryImplementation>();
        services.AddTransient<BotUserRepository, BotUserRepositoryImplementation>();

        services.AddTransient<AggregateSnapshotCreator<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();
        services.AddTransient<CreateSnapshotCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();
        services.AddTransient<BaseCommandJob<CreateSnapshotCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>, CreateSnapshotArguments>>();
        services.AddScoped<CreateSnapshotArguments>(provider => 
        {
            var settings = provider.GetRequiredService<SettingsBuilder>();
            var channel = settings.TelegramApiSettingsSection.ChannelReferenceLink;

            var timeProvider = provider.GetRequiredService<ITimeProvider>();

            return new CreateSnapshotArguments
            {
                AggregateName = channel,
                TimeStamp = timeProvider.UtcNow
            };
        });

        services.AddTransient<EntitiesQuantityCounterBase<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>,
            EntitiesQuantityCounter<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

        // Queries/Commands
        services.AddTransient<AddOrUpdateSubscribersCommand>();
        //services.AddTransient<GetAllCurrentSubscribersQuery>();
        services.AddTransient<GetEventsInPeriodQueryExecution<ChannelMember>>();
        services.AddTransient<GetTimeSpanBetweenLastEventsQueryExecution<ChannelMember>>();
        services.AddTransient<GetUsersCountForPeriodQueryExecution>();
        services.AddTransient<UsersCountForPeriodCore>();
        services.AddTransient<ClosestSnapshotByTimeQueryExecution>();
        services.AddTransient<GetSubscribersByIdentitiesQuery>();
        services.AddTransient<GetAllCurrentSubscribersIdentitiesQuery>();
        services.AddTransient<GetUserByNickNameQueryExecution>();

        services.AddTransient<IEntitiesChangeDetector<long>, EntitiesChangeDetector<long>>();
        services.AddTransient<AddEventsCommand<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

        services.AddTransient<IEntitiesChangeEventsCreator<ChannelMember>, EntitiesChangeEventsCreator<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

        services.AddTransient<SubscribersChangeProcessor>();
        services.AddTransient<MessageBuilder>();
        services.AddTransient<MonitoringPresentation>();
        services.AddTransient<EntitiesChangeMessagingService<ChannelMember>>();
        services.AddTransient<EntitiesChangeMessageProducer<ChannelMember>, SubscribersChangeMessageProducer>();
        services.AddTransient<AddBotUserCommand>();
        services.AddTransient<GetActiveBotUsersQuery>();
        services.AddTransient<IBotUsersService,  BotUsersService>();

        services.AddScoped<IMonitoringService<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>, SubscribersMonitoringService>();

        // MonitoringBotRunner
        services.AddScoped<SnapshotCollectingJobScheduler<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

        services.AddScoped<MonitoringBotRunner<ChannelMember, SubscriberJoinedEvent, SubscriberLeftEvent>>();

        // Hosted service
        services.AddHostedService<MonitoringBotBackgroundService>();

        // Quartz
        services.AddQuartz(q =>
        {
            q.SchedulerId = settingsBuilder.QuartzSettingsSection.SchedulerId!;
            q.SetProperty("quartz.serializer.type", settingsBuilder.QuartzSettingsSection.SerializerType!);
            q.SetProperty("quartz.scheduler.instanceName", settingsBuilder.QuartzSettingsSection.InstanceName!);
            q.SetProperty("quartz.threadPool.threadCount", settingsBuilder.QuartzSettingsSection.ThreadCount!);
        });

        services.AddSingleton(provider =>
        {
            var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
            var scheduler = schedulerFactory.GetScheduler().Result;
            Console.WriteLine("Scheduler инициализирован");
            scheduler.Start().Wait();
            return scheduler;
        });

        services.AddRouting();

        return services;
    }
}
