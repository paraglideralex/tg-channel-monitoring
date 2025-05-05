using System.Text;

namespace MonitoringBot.Infrastructure.Extensions;
public static class TimeSpanExtensions
{
    public static string FormattedDuration(this TimeSpan duration)
    {
        var sb = new StringBuilder(32);

        if (duration.Days != 0)
            sb.Append(duration.Days).Append("d, ");

        if (duration.Hours != 0)
            sb.Append(duration.Hours).Append("h, ");

        if (duration.Minutes != 0)
            sb.Append(duration.Minutes).Append("m, ");

        if (duration.Seconds != 0)
            sb.Append(duration.Seconds).Append("s, ");

        if (sb.Length > 0)
            sb.Length -= 2;

        return sb.ToString();
    }
}
