namespace MonitoringBot.Infrastructure;

public class ChannelMemberEntity
{
    public long Id { get; set; }
    public string? NickName { get; set; }
    public bool IsBot { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public DateTime? TimeStamp { get; set; }
    public DateTime? JoinedAt { get; set; }
}
