using Microsoft.EntityFrameworkCore;
using MonitoringBot.Infrastructure.Persistence.Entities;
using MonitoringBot.Infrastructure.Persistence.Entities.Snapshots;

namespace MonitoringBot.Infrastructure.Persistence.DatabaseContexts;

public class MonitoringBotDbContextBase : DbContext
{
    public MonitoringBotDbContextBase(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ChannelMemberEntity> ChannelMembers { get; set; }
    public DbSet<EventEntity> Events { get; set; }
    public DbSet<AggregateSnapshotEntity> Snapshots { get; set; }
    public DbSet<BotUserEntity> BotUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ChannelMemberEntity>(channelMember =>
        {
            channelMember.HasKey(c => c.Id);
            channelMember.HasIndex(c => c.LastAction);
            channelMember.HasIndex(c => c.TimeStamp);
            channelMember.HasIndex(c => c.Id);
        });

        modelBuilder.Entity<EventEntity>(@event =>
        {
            @event.HasKey(e => e.Id);
            @event.HasIndex(e => new { e.TimeStamp, e.CurrentTimeSequenceNumber });
        });

        modelBuilder.Entity<AggregateSnapshotEntity>(snapshot =>
        {
            snapshot.HasKey(s => s.Id);
            snapshot.HasIndex(s => s.LastEventTimeStamp);
        });

        modelBuilder.Entity<BotUserEntity>(user =>
        {
            user.HasKey(u => u.Id);
            user.HasIndex(u => u.Id);
        });
    }
}
