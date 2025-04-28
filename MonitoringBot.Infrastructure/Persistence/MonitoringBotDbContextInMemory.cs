using Microsoft.EntityFrameworkCore;
namespace MonitoringBot.Infrastructure.Persistence;

public class MonitoringBotDbContextInMemory : MonitoringBotDbContextBase
{
    public MonitoringBotDbContextInMemory(DbContextOptions<MonitoringBotDbContextInMemory> options) : base(options) { }
}
