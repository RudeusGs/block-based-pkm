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
        DateTimeOffset now)
    {
        var minPriority = preference.MinPriorityForRecommendation;

        return candidates
            .Where(x => x.Status != StatusWorkTask.Done)
            .Where(x => x.Priority >= minPriority)
            .Select(candidate => ScoreOne(candidate, preference, historyStats, now))
            .Where(x => x.Score >= preference.RecommendationSensitivity)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Candidate.DueDate ?? DateTimeOffset.MaxValue)
            .ThenByDescending(x => x.Candidate.Priority)
            .Take(preference.MaxRecommendationsPerSession)
            .ToArray();
    }

    private static ScoredRecommendationCandidate ScoreOne(
        RecommendationCandidateReadModel candidate,
        UserTaskPreference preference,
        UserTaskHistoryStatsDto historyStats,
        DateTimeOffset now)
    {
        decimal score = 0;
        var reasons = new List<string>();

        var priorityScore = candidate.Priority switch
        {
            PriorityWorkTask.High => 35m,
            PriorityWorkTask.Medium => 22m,
            PriorityWorkTask.Low => 10m,
            _ => 0m
        };

        score += priorityScore;
        reasons.Add($"priority:{candidate.Priority}+{priorityScore:0}");

        if (candidate.DueDate.HasValue)
        {
            var due = candidate.DueDate.Value;

            if (due < now)
            {
                score += 30m;
                reasons.Add("overdue+30");
            }
            else
            {
                var hours = (due - now).TotalHours;

                if (hours <= 4)
                {
                    score += 28m;
                    reasons.Add("due_soon_4h+28");
                }
                else if (hours <= 24)
                {
                    score += 22m;
                    reasons.Add("due_today+22");
                }
                else if (hours <= 72)
                {
                    score += 12m;
                    reasons.Add("due_3_days+12");
                }
            }
        }
        else
        {
            score += 4m;
            reasons.Add("no_due_date+4");
        }

        if (!candidate.HasAnyAssignee)
        {
            score += 12m;
            reasons.Add("unassigned+12");
        }

        if (candidate.CreatedById == historyStats.UserId)
        {
            score += 8m;
            reasons.Add("created_by_user+8");
        }

        if (candidate.IsAssignedToCurrentUser)
        {
            score += 20m;
            reasons.Add("assigned_to_user+20");
        }

        if (historyStats.CompletedByTaskId.TryGetValue(candidate.TaskId, out var completedCount))
        {
            var boost = Math.Min(15m, completedCount * 5m);
            score += boost;
            reasons.Add($"completed_history+{boost:0}");
        }

        if (historyStats.SkippedOrAbandonedByTaskId.TryGetValue(candidate.TaskId, out var skippedCount))
        {
            var penalty = Math.Min(25m, skippedCount * 8m);
            score -= penalty;
            reasons.Add($"skipped_or_abandoned-{penalty:0}");
        }

        if (historyStats.CompletedCount > 0)
        {
            var completionRatio =
                historyStats.CompletedCount /
                (decimal)Math.Max(1, historyStats.CompletedCount + historyStats.AbandonedCount + historyStats.SkippedCount);

            var boost = Math.Round(completionRatio * 10m, 2);
            score += boost;
            reasons.Add($"user_completion_ratio+{boost:0.##}");
        }

        var ageHours = (now - candidate.CreatedDate).TotalHours;
        if (ageHours <= 24)
        {
            score += 6m;
            reasons.Add("new_task+6");
        }

        score = Math.Clamp(score, 0m, 100m);

        return new ScoredRecommendationCandidate(
            candidate,
            score,
            string.Join("; ", reasons));
    }
}