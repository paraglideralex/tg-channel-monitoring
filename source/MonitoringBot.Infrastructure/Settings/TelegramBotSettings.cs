namespace MonitoringBot.Infrastructure.Settings;

public class TelegramBotSettings
{
    public string BotToken { get; set; } = "";
    public int CheckPeriodSeconds { get; set; }
//    public List<long> ChatIdsCollection { get; set; } = new();
}
