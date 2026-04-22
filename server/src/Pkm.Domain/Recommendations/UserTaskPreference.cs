using Pkm.Domain.Common;
using Pkm.Domain.Tasks;

namespace Pkm.Domain.Recommendations;

/// <summary>
/// UserTaskPreference: cấu hình cá nhân hóa cho recommendation engine.
/// Domain chỉ giữ config + guard conditions, không biết chi tiết lưu trữ.
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
    /// Danh sách ngày làm việc ưu tiên (0 = Sunday -> 6 = Saturday)
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
            throw new DomainException("Giờ phải trong khoảng 0-23.");

        if (startHour >= endHour)
            throw new DomainException("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.");

        WorkDayStartHour = startHour;
        WorkDayEndHour = endHour;

        Touch(now);
    }

    public void UpdateSensitivity(int sensitivity, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (sensitivity < 0 || sensitivity > 100)
            throw new DomainException("Độ nhạy phải từ 0-100.");

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
            throw new DomainException("RecommendationIntervalMinutes phải lớn hơn 0.");

        RecommendationIntervalMinutes = intervalMinutes;
        Touch(now);
    }

    public void UpdateMaxRecommendations(int maxCount, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (maxCount <= 0)
            throw new DomainException("MaxRecommendationsPerSession phải lớn hơn 0.");

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
            throw new DomainException("Danh sách ngày không được null.");

        var normalized = days
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (normalized.Length == 0)
            throw new DomainException("Danh sách ngày không được rỗng.");

        if (normalized.Any(d => d < 0 || d > 6))
            throw new DomainException("Ngày phải trong khoảng 0-6.");

        return normalized;
    }
}