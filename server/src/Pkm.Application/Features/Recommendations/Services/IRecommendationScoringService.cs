using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Services;

public interface IRecommendationScoringService
{
    IReadOnlyList<ScoredRecommendationCandidate> Score(
        IReadOnlyList<RecommendationCandidateReadModel> candidates,
        UserTaskPreference preference,
        UserTaskHistoryStatsDto historyStats,
        DateTimeOffset now);
}

public sealed record ScoredRecommendationCandidate(
    RecommendationCandidateReadModel Candidate,
    decimal Score,
    string Reason);