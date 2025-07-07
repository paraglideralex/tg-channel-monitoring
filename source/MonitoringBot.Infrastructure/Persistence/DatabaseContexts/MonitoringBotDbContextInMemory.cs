using Microsoft.EntityFrameworkCore;
namespace MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

public class MonitoringBotDbContextInMemory : MonitoringBotDbContextBase
{
    public MonitoringBotDbContextInMemory(DbContextOptions<MonitoringBotDbContextInMemory> options) : base(options) { }
}
