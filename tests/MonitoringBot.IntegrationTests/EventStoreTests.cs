using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using MonitoringBot.Domain.Entities;
using MonitoringBot.Domain.Events;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;
using MonitoringBot.Infrastructure.RepositoriesImplementations;
using MonitoringBot.Infrastructure.Settings;

using NUnit.Framework;

namespace MonitoringBot.IntegrationTests;
public class EventStoreTests: IntegrationTestsBase
{

    public static MonitoringBotDbContextBase CreatePostgres(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MonitoringBotDbContextBase>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new MonitoringBotDbContextBase(options);
        context.Database.Migrate();

        return context;
    }

    [Test]
    //[Ignore("нужен был чисто для отладки счётчика")]
    public async Task Basic()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        var configPath = Path.Combine(AppContext.BaseDirectory, "Configurations");

        var builder = new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile("testDatabaseSettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"testDatabaseSettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        IConfiguration appsettings = builder.Build();

        var connection = appsettings.GetSection("Database").Get<DatabaseSettings>()
            ?? throw new InvalidDataException($"{nameof(DatabaseSettings)} section doesn't exist.");

        using var context = CreatePostgres(connection.BaseConnectionString);

        var eventsRepo = new EventsRepositoryImplementation<ChannelMember>(context);

        var event1 = new EntitiesChangedDomainEventBase<ChannelMember> { Id = Guid.NewGuid(), ChannelName = "test", EntityIdProjection = "test", EntityNameProjection = "test", TimeStamp = DateTime.UtcNow };
        var event2 = new EntitiesChangedDomainEventBase<ChannelMember> { Id = Guid.NewGuid(), ChannelName = "test", EntityIdProjection = "test", EntityNameProjection = "test", TimeStamp = DateTime.UtcNow };
        var event3 = new EntitiesChangedDomainEventBase<ChannelMember> { Id= Guid.NewGuid(), ChannelName = "test", EntityIdProjection = "test", EntityNameProjection = "test", TimeStamp = DateTime.UtcNow };

        await eventsRepo.AddRangeAsync([event1]);
        await eventsRepo.AddRangeAsync([event2, event3]);


        var all = await context.Events.ToListAsync();
    }
}
