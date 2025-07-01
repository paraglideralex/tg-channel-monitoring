using Microsoft.EntityFrameworkCore;

namespace MonitoringBot.Infrastructure.Persistence;

public class MonitoringBotDbContextBase: DbContext
{
    public MonitoringBotDbContextBase(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ChannelMemberEntity> ChannelMembers { get; set; }
    public DbSet<EventEntity> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChannelMemberEntity>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<EventEntity>(entity =>
        {
            modelBuilder.Entity<EventEntity>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<EventEntity>()
                .HasIndex(e => e.SequenceNumber);

            entity.Property(e => e.SequenceNumber)
                .ValueGeneratedOnAdd()
                .UseIdentityByDefaultColumn();
        });
    }
}
