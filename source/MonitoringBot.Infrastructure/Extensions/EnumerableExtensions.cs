namespace MonitoringBot.Infrastructure.Extensions;

public static class EnumerableExtensions
{
    public static bool AllNotNull<T>(this IEnumerable<T> enumerable) =>
        enumerable.All(x => x != null);
}
