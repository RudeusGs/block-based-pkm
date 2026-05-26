namespace Pkm.Infrastructure.Persistence;

internal static class LikePattern
{
    public static string Contains(string value)
        => $"%{Escape(value.Trim())}%";

    private static string Escape(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }
}
