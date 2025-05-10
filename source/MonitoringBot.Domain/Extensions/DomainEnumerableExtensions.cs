namespace MonitoringBot.Domain.Extensions;

public static class DomainEnumerableExtensions
{
    public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        return first.Except(second).Concat(second.Except(first));
    }

    public static IEnumerable<T> SymmetricDifference<T>(
        this IEnumerable<T> first,
        IEnumerable<T> second,
        IEqualityComparer<T> comparer)
    {
        return first.Except(second, comparer).Concat(second.Except(first, comparer));
    }
}
