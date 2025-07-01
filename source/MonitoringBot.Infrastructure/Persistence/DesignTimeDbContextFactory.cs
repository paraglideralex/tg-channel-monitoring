using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MonitoringBot.Infrastructure.Settings;

namespace MonitoringBot.Infrastructure.Persistence;
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MonitoringBotDbContextBase>
{
    public MonitoringBotDbContextBase CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        var configPath = Path.Combine(AppContext.BaseDirectory, "Configurations");

        var builder = new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile("databaseSettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"databaseSettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        IConfiguration appsettings = builder.Build();

        var connection = appsettings.GetSection("Database").Get<DatabaseSettings>()
            ?? throw new InvalidDataException($"{nameof(DatabaseSettings)} section doesn't exist.");


        var optionsBuilder = new DbContextOptionsBuilder<MonitoringBotDbContextBase>();
        optionsBuilder.UseNpgsql(connection.BaseConnectionString);

        return new MonitoringBotDbContextBase(optionsBuilder.Options);
    }
}
