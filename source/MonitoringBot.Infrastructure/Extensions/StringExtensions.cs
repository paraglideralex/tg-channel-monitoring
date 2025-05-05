namespace MonitoringBot.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string? TakeAndFormatFirst(this string? input, int firstSymbolsCount = 300)
    {
        return (string.IsNullOrEmpty(input) || input.Length <= firstSymbolsCount)
        ? input
        : string.Concat(input.AsSpan(0, firstSymbolsCount), ".....");
    }
}
