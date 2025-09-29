using CrystalQuartz.Application;
using CrystalQuartz.AspNetCore;

using Microsoft.EntityFrameworkCore;

using MonitoringBot;
using MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

using Quartz;

var settingsBuilder = new SettingsBuilder();
settingsBuilder.Build();

var builder = WebApplication.CreateBuilder(args);

LoggingSetup.SetupLogging();

builder.Services.AddMonitoringBotServices(builder.Configuration);

var app = builder.Build();

app.UseCrystalQuartz(
    () => app.Services.GetRequiredService<IScheduler>(),
    new CrystalQuartzOptions
    {
        Path = "/quartz"
    });

app.MapGet("/", () => "Go to /quartz to see background jobs details.");

await ApplyMigrationsIfNeededAsync<MonitoringBotDbContextBase>(app);

await app.RunAsync(settingsBuilder.QuartzSettingsSection?.DebuggerEndpoint);

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