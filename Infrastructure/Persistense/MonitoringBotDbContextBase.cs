using Microsoft.EntityFrameworkCore;

namespace MonitoringBot.Infrastructure.Persistense;

public class MonitoringBotDbContextBase: DbContext
{
    public MonitoringBotDbContextBase(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ChannelMemberEntity> ChannelMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChannelMemberEntity>().HasKey(u => u.Id);
    }
}
