namespace Pkm.Infrastructure.Persistence.Cleanup;

public sealed class PageTrashCleanupOptions
{
    public const string SectionName = "PageTrashCleanup";

    public bool Enabled { get; init; } = true;

    /// <summary>
    /// How long a page can stay in Trash before it is removed from the application.
    /// </summary>
    public int RetentionDays { get; init; } = 30;

    /// <summary>
    /// How often the cleanup job wakes up.
    /// </summary>
    public int IntervalMinutes { get; init; } = 60;

    /// <summary>
    /// Maximum number of expired root pages processed per cleanup cycle.
    /// Descendant pages are included automatically and do not count against this limit.
    /// </summary>
    public int BatchSize { get; init; } = 100;
}
