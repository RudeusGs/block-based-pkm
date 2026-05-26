using Pkm.Domain.SharedKernel;
using Pkm.Domain.Tasks;

namespace Pkm.Domain.Recommendations;

/// <summary>
/// Personalized recommendation settings for a user in a workspace.
/// The domain owns configuration invariants and does not know persistence details.
/// </summary>
public sealed class UserTaskPreference : EntityBase
{
    private static readonly int[] DefaultPreferredDays = { 1, 2, 3, 4, 5 };

    private readonly List<int> _preferredDaysOfWeek = new();

    public Guid UserId { get; private set; }
    public Guid WorkspaceId { get; private set; }

    public int WorkDayStartHour { get; private set; }
    public int WorkDayEndHour { get; private set; }

    /// <summary>
    /// Preferred work days (0 = Sunday -> 6 = Saturday).
    /// </summary>
    public IReadOnlyCollection<int> PreferredDaysOfWeek => _preferredDaysOfWeek.AsReadOnly();

    public int MaxRecommendationsPerSession { get; private set; }
    public PriorityWorkTask MinPriorityForRecommendation { get; private set; }
    public int RecommendationSensitivity { get; private set; }
    public int RecommendationIntervalMinutes { get; private set; }
    public bool EnableAutoRecommendation { get; private set; }

    private UserTaskPreference() { }

    public UserTaskPreference(Guid id, Guid userId, Guid workspaceId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(userId, nameof(userId));
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));

        UserId = userId;
        WorkspaceId = workspaceId;

        ApplyDefaults();
    }

    public void UpdateWorkHours(int startHour, int endHour, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (startHour < 0 || startHour > 23 || endHour < 0 || endHour > 23)
            throw new DomainException("Preferred hours must be between 0 and 23.");

        if (startHour >= endHour)
            throw new DomainException("Preferred start hour must be earlier than preferred end hour.");

        WorkDayStartHour = startHour;
        WorkDayEndHour = endHour;

        Touch(now);
    }

    public void UpdateSensitivity(int sensitivity, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (sensitivity < 0 || sensitivity > 100)
            throw new DomainException("Recommendation sensitivity must be between 0 and 100.");

        RecommendationSensitivity = sensitivity;
        Touch(now);
    }

    public void SetPreferredDays(IEnumerable<int> days, DateTimeOffset now)
    {
        ThrowIfDeleted();

        var normalizedDays = NormalizePreferredDays(days);

        _preferredDaysOfWeek.Clear();
        _preferredDaysOfWeek.AddRange(normalizedDays);

        Touch(now);
    }

    public void SetAutoRecommendation(bool enable, DateTimeOffset now)
    {
        ThrowIfDeleted();

        EnableAutoRecommendation = enable;
        Touch(now);
    }

    public void UpdateRecommendationInterval(int intervalMinutes, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (intervalMinutes <= 0)
            throw new DomainException("Recommendation interval must be greater than zero.");

        RecommendationIntervalMinutes = intervalMinutes;
        Touch(now);
    }

    public void UpdateMaxRecommendations(int maxCount, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (maxCount <= 0)
            throw new DomainException("Maximum recommendations per session must be greater than zero.");

        MaxRecommendationsPerSession = maxCount;
        Touch(now);
    }

    public void UpdateMinPriority(PriorityWorkTask minPriority, DateTimeOffset now)
    {
        ThrowIfDeleted();

        MinPriorityForRecommendation = minPriority;
        Touch(now);
    }

    public void ResetToDefault(DateTimeOffset now)
    {
        ThrowIfDeleted();

        ApplyDefaults();
        Touch(now);
    }

    public bool IsSuitableForRecommendation(DateTimeOffset currentTime)
    {
        if (!EnableAutoRecommendation)
            return false;

        if (currentTime.Hour < WorkDayStartHour || currentTime.Hour >= WorkDayEndHour)
            return false;

        var currentDay = (int)currentTime.DayOfWeek;

        return _preferredDaysOfWeek.Count == 0 || _preferredDaysOfWeek.Contains(currentDay);
    }

    private void ApplyDefaults()
    {
        WorkDayStartHour = 8;
        WorkDayEndHour = 18;

        MaxRecommendationsPerSession = 3;
        RecommendationSensitivity = 50;
        RecommendationIntervalMinutes = 30;
        EnableAutoRecommendation = true;

        MinPriorityForRecommendation = PriorityWorkTask.Medium;

        _preferredDaysOfWeek.Clear();
        _preferredDaysOfWeek.AddRange(DefaultPreferredDays);
    }

    private static IReadOnlyCollection<int> NormalizePreferredDays(IEnumerable<int> days)
    {
        if (days is null)
            throw new DomainException("Preferred days must not be null.");

        var normalized = days
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (normalized.Length == 0)
            throw new DomainException("Preferred days must not be empty.");

        if (normalized.Any(d => d < 0 || d > 6))
            throw new DomainException("Preferred day values must be between 0 and 6.");

        return normalized;
    }
}
