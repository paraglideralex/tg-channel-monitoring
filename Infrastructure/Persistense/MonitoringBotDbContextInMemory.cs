using Microsoft.EntityFrameworkCore;
namespace MonitoringBot.Infrastructure.Persistense;

public class MonitoringBotDbContextInMemory : MonitoringBotDbContextBase
{
    public MonitoringBotDbContextInMemory(DbContextOptions<MonitoringBotDbContextInMemory> options) : base(options) { }
}
