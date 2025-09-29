using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringBot.Infrastructure.Persistence.Entities;

public sealed class ApiUserEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required long Id { get; set; }
    public string? NickName { get; set; }
    public bool IsBot { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? ChannelReference { get; set; }
    public DateTime? Joined { get; set; }
    public DateTime? TimeStamp { get; set; }
    public bool IsCurrent { get; set; }
    public override string ToString() =>
        $"{ChannelReference}_{Id}_{NickName}_Joined:{Joined}_{TimeStamp}";
}
