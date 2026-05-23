using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Recommendations.Services;

public sealed class RecommendationScoringService : IRecommendationScoringService
{
    public IReadOnlyList<ScoredRecommendationCandidate> Score(
        IReadOnlyList<RecommendationCandidateReadModel> candidates,
        UserTaskPreference preference,
        UserTaskHistoryStatsDto historyStats,
        DateTimeOffset now,
        bool isPersonalWorkspace)
    {
        var minPriority = preference.MinPriorityForRecommendation;

        var scoredCandidates = candidates
            .Where(x => x.Status != StatusWorkTask.Done)
            .Where(x => x.Priority >= minPriority)
            .Select(candidate => isPersonalWorkspace
                ? ScorePersonalWorkspace(candidate, preference, historyStats, now)
                : ScoreTeamWorkspace(candidate, preference, historyStats, now))
            .Where(x => x.Score >= preference.RecommendationSensitivity)
            .ToArray();

        return DeduplicateSemanticCandidates(scoredCandidates)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Candidate.DueDate ?? DateTimeOffset.MaxValue)
            .ThenByDescending(x => x.Candidate.Priority)
            .ThenByDescending(x => x.Candidate.UpdatedDate ?? x.Candidate.CreatedDate)
            .Take(preference.MaxRecommendationsPerSession)
            .ToArray();
    }

    private static IReadOnlyList<ScoredRecommendationCandidate> DeduplicateSemanticCandidates(
        IReadOnlyList<ScoredRecommendationCandidate> scoredCandidates)
    {
        var groups = new Dictionary<string, List<ScoredRecommendationCandidate>>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<ScoredRecommendationCandidate>();

        foreach (var candidate in scoredCandidates)
        {
            var key = TaskSemanticKeyBuilder.BuildTitleKey(candidate.Candidate);

            if (string.IsNullOrWhiteSpace(key))
            {
                unique.Add(candidate);
                continue;
            }

            if (!groups.TryGetValue(key, out var group))
            {
                group = new List<ScoredRecommendationCandidate>();
                groups[key] = group;
            }

            group.Add(candidate);
        }

        foreach (var group in groups.Values)
        {
            var best = group
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Candidate.DueDate ?? DateTimeOffset.MaxValue)
                .ThenByDescending(x => x.Candidate.Priority)
                .ThenByDescending(x => x.Candidate.UpdatedDate ?? x.Candidate.CreatedDate)
                .First();

            unique.Add(group.Count <= 1
                ? best
                : best with
                {
                    Reason = AppendReason(
                        best.Reason,
                        $"Đã gộp {group.Count} task có nội dung tương tự để tránh gợi ý trùng lặp.")
                });
        }

        return unique;
    }

    private static string AppendReason(string reason, string extra)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return extra;

        if (reason.Contains(extra, StringComparison.OrdinalIgnoreCase))
            return reason;

        return $"{reason} {extra}";
    }

    private static ScoredRecommendationCandidate ScorePersonalWorkspace(
        RecommendationCandidateReadModel candidate,
        UserTaskPreference preference,
        UserTaskHistoryStatsDto historyStats,
        DateTimeOffset now)
    {
        decimal score = 0;
        var reasons = new List<string>();

        score += PriorityScore(candidate.Priority);
        AddPriorityReason(reasons, candidate.Priority);

        score += DueDateScore(candidate.DueDate, now, reasons);

        if (candidate.Status == StatusWorkTask.Doing)
        {
            score += 12m;
            reasons.Add("Bạn đang làm dở task này.");
        }

        if (candidate.IsAssignedToCurrentUser)
        {
            score += 16m;
            reasons.Add("Task này đang được giao cho bạn.");
        }

        if (!candidate.HasAnyAssignee)
        {
            score += 8m;
            reasons.Add("Task này chưa có người nhận.");
        }

        if (candidate.CreatedById == historyStats.UserId)
        {
            score += 8m;
            reasons.Add("Task này do bạn tạo.");
        }

        score += LowSignalPenalty(candidate, reasons);
        score += TitleQualityPenalty(candidate, reasons);

        if (historyStats.HasEnoughPersonalSignal)
        {
            score += PersonalHabitScore(candidate, historyStats, now, reasons);
        }
        else
        {
            reasons.Add("Chưa đủ dữ liệu thói quen, hệ thống ưu tiên deadline, mức độ quan trọng và trách nhiệm được giao.");
        }

        score += FreshnessScore(candidate.CreatedDate, now, reasons);

        score = Math.Clamp(score, 0m, 100m);

        return new ScoredRecommendationCandidate(
            candidate,
            score,
            BuildReason(reasons, isPersonalWorkspace: true));
    }

    private static ScoredRecommendationCandidate ScoreTeamWorkspace(
        RecommendationCandidateReadModel candidate,
        UserTaskPreference preference,
        UserTaskHistoryStatsDto historyStats,
        DateTimeOffset now)
    {
        decimal score = 0;
        var reasons = new List<string>();

        score += PriorityScore(candidate.Priority);
        AddPriorityReason(reasons, candidate.Priority);

        score += DueDateScore(candidate.DueDate, now, reasons);

        if (candidate.IsAssignedToCurrentUser)
        {
            score += 28m;
            reasons.Add("Task này đang được giao cho bạn.");
        }
        else if (!candidate.HasAnyAssignee)
        {
            score += 18m;
            reasons.Add("Task này chưa có người nhận.");
        }

        if (candidate.Status == StatusWorkTask.Doing)
        {
            score += 10m;
            reasons.Add("Task này đang trong trạng thái đang làm.");
        }

        score += LowSignalPenalty(candidate, reasons);
        score += TitleQualityPenalty(candidate, reasons);

        score += TeamFreshnessScore(candidate.CreatedDate, candidate.UpdatedDate, now, reasons);

        score = Math.Clamp(score, 0m, 100m);

        return new ScoredRecommendationCandidate(
            candidate,
            score,
            BuildReason(reasons, isPersonalWorkspace: false));
    }

    private static decimal LowSignalPenalty(
        RecommendationCandidateReadModel candidate,
        List<string> reasons)
    {
        if (candidate.DueDate.HasValue || candidate.IsAssignedToCurrentUser || candidate.Status == StatusWorkTask.Doing)
            return 0m;

        if (candidate.Priority == PriorityWorkTask.High)
            return 0m;

        reasons.Add("Task thiếu deadline/người phụ trách nên AI giảm độ ưu tiên để tránh gợi ý mơ hồ.");
        return -14m;
    }

    private static decimal TitleQualityPenalty(
        RecommendationCandidateReadModel candidate,
        List<string> reasons)
    {
        var semanticKey = TaskSemanticKeyBuilder.BuildTitleKey(candidate);

        if (string.IsNullOrWhiteSpace(semanticKey))
        {
            reasons.Add("Tiêu đề task chưa đủ rõ để AI hiểu mục tiêu công việc.");
            return -18m;
        }

        var tokenCount = semanticKey.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (tokenCount <= 1 && !candidate.DueDate.HasValue && candidate.Priority != PriorityWorkTask.High)
        {
            reasons.Add("Tiêu đề task khá chung chung, nên thêm ngữ cảnh hoặc deadline để AI gợi ý chuẩn hơn.");
            return -10m;
        }

        return 0m;
    }

    private static decimal PriorityScore(PriorityWorkTask priority)
    {
        return priority switch
        {
            PriorityWorkTask.High => 35m,
            PriorityWorkTask.Medium => 22m,
            PriorityWorkTask.Low => 10m,
            _ => 0m
        };
    }

    private static void AddPriorityReason(List<string> reasons, PriorityWorkTask priority)
    {
        switch (priority)
        {
            case PriorityWorkTask.High:
                reasons.Add("Task này có mức ưu tiên cao.");
                break;

            case PriorityWorkTask.Medium:
                reasons.Add("Task này có mức ưu tiên vừa.");
                break;

            case PriorityWorkTask.Low:
                reasons.Add("Task này có mức ưu tiên thấp.");
                break;
        }
    }

    private static decimal DueDateScore(
        DateTimeOffset? dueDate,
        DateTimeOffset now,
        List<string> reasons)
    {
        if (!dueDate.HasValue)
        {
            reasons.Add("Task chưa có deadline nên chỉ được ưu tiên khi có tín hiệu quan trọng khác.");
            return 2m;
        }

        var due = dueDate.Value;

        if (due < now)
        {
            reasons.Add("Task đã quá hạn, nên xử lý hoặc cập nhật trạng thái sớm.");
            return 30m;
        }

        var hours = (due - now).TotalHours;

        if (hours <= 4)
        {
            reasons.Add("Task sắp tới hạn trong vài giờ tới.");
            return 28m;
        }

        if (hours <= 24)
        {
            reasons.Add("Task cần xử lý trong hôm nay.");
            return 22m;
        }

        if (hours <= 72)
        {
            reasons.Add("Task có deadline gần trong vài ngày tới.");
            return 12m;
        }

        return 0m;
    }

    private static decimal PersonalHabitScore(
        RecommendationCandidateReadModel candidate,
        UserTaskHistoryStatsDto historyStats,
        DateTimeOffset now,
        List<string> reasons)
    {
        decimal score = 0;

        if (historyStats.CompletedByTaskId.TryGetValue(candidate.TaskId, out var completedCount))
        {
            var boost = Math.Min(12m, completedCount * 4m);
            score += boost;
            reasons.Add("Bạn từng hoàn thành task này trước đây nên có tín hiệu lịch sử tích cực.");
        }

        if (historyStats.SkippedOrAbandonedByTaskId.TryGetValue(candidate.TaskId, out var skippedCount))
        {
            var penalty = Math.Min(24m, skippedCount * 8m);
            score -= penalty;
            reasons.Add("Bạn từng bỏ qua task này trước đây nên hệ thống giảm độ ưu tiên.");
        }

        if (candidate.CreatedById == historyStats.UserId && historyStats.CompletedCreatedByUserCount > 0)
        {
            score += Math.Min(10m, historyStats.CompletedCreatedByUserCount * 2m);
            reasons.Add("Bạn thường hoàn thành tốt các task do chính bạn tạo.");
        }

        if (candidate.IsAssignedToCurrentUser && historyStats.CompletedAssignedToUserCount > 0)
        {
            score += Math.Min(10m, historyStats.CompletedAssignedToUserCount * 2m);
            reasons.Add("Bạn thường hoàn thành tốt các task được giao cho mình.");
        }

        if (historyStats.CompletionRatio >= 0.7m)
        {
            score += 8m;
            reasons.Add("Tỉ lệ hoàn thành gần đây của bạn đang tốt.");
        }
        else if (historyStats.CompletionRatio <= 0.35m)
        {
            score -= 6m;
            reasons.Add("Gần đây bạn bỏ qua khá nhiều task nên hệ thống giảm độ ưu tiên.");
        }

        if (historyStats.MostProductiveHour.HasValue)
        {
            var distance = Math.Abs(now.Hour - historyStats.MostProductiveHour.Value);
            distance = Math.Min(distance, 24 - distance);

            if (distance <= 2)
            {
                score += 5m;
                reasons.Add("Thời điểm hiện tại gần với khung giờ bạn thường hoàn thành task.");
            }
        }

        if (historyStats.MostProductiveDayOfWeek.HasValue &&
            historyStats.MostProductiveDayOfWeek.Value == (int)now.DayOfWeek)
        {
            score += 4m;
            reasons.Add("Hôm nay trùng với ngày bạn thường xử lý task hiệu quả.");
        }

        return score;
    }

    private static decimal FreshnessScore(
        DateTimeOffset createdDate,
        DateTimeOffset now,
        List<string> reasons)
    {
        var ageHours = (now - createdDate).TotalHours;

        if (ageHours <= 24)
        {
            reasons.Add("Task này mới được tạo gần đây.");
            return 5m;
        }

        return 0m;
    }

    private static decimal TeamFreshnessScore(
        DateTimeOffset createdDate,
        DateTimeOffset? updatedDate,
        DateTimeOffset now,
        List<string> reasons)
    {
        var changedAt = updatedDate ?? createdDate;
        var ageHours = (now - changedAt).TotalHours;

        if (ageHours <= 24)
        {
            reasons.Add("Task này mới được cập nhật gần đây.");
            return 8m;
        }

        if (ageHours <= 72)
        {
            reasons.Add("Task này có thay đổi gần đây trong workspace.");
            return 4m;
        }

        return 0m;
    }

    private static string BuildReason(
        IReadOnlyList<string> reasons,
        bool isPersonalWorkspace)
    {
        var prefix = isPersonalWorkspace
            ? "AI ưu tiên theo deadline, trách nhiệm và thói quen làm việc: "
            : "AI ưu tiên theo deadline, người phụ trách và độ quan trọng trong workspace: ";

        var cleanReasons = reasons
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(3)
            .ToArray();

        if (cleanReasons.Length == 0)
        {
            return isPersonalWorkspace
                ? "AI chọn task này dựa trên deadline, trách nhiệm và thói quen làm việc của bạn."
                : "AI chọn task này dựa trên deadline, người phụ trách và mức độ ưu tiên của task.";
        }

        return prefix + string.Join(" ", cleanReasons);
    }
}



