using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Services;

public sealed class RecommendationCandidateDeduplicator : IRecommendationCandidateDeduplicator
{
    private readonly IWorkTaskRecommendationReadRepository _recommendationReadRepository;

    public RecommendationCandidateDeduplicator(
        IWorkTaskRecommendationReadRepository recommendationReadRepository)
    {
        _recommendationReadRepository = recommendationReadRepository;
    }

    public async Task<IReadOnlyList<RecommendationCandidateReadModel>> RemovePreviouslyRecommendedSemanticDuplicatesAsync(
        IReadOnlyList<RecommendationCandidateReadModel> candidates,
        IReadOnlySet<Guid> previouslyRecommendedTaskIds,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0 || previouslyRecommendedTaskIds.Count == 0)
            return candidates;

        var previouslyRecommendedTaskDetails =
            await _recommendationReadRepository.ListRecommendationTaskDetailsByIdsAsync(
                currentUserId,
                previouslyRecommendedTaskIds,
                cancellationToken);

        var previouslyRecommendedKeys = previouslyRecommendedTaskDetails.Values
            .Select(TaskSemanticKeyBuilder.BuildTitleKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (previouslyRecommendedKeys.Count == 0)
        {
            return candidates
                .Where(x => !previouslyRecommendedTaskIds.Contains(x.TaskId))
                .ToArray();
        }

        return candidates
            .Where(candidate =>
            {
                if (previouslyRecommendedTaskIds.Contains(candidate.TaskId))
                    return false;

                var candidateKey = TaskSemanticKeyBuilder.BuildTitleKey(candidate);

                if (string.IsNullOrWhiteSpace(candidateKey))
                    return true;

                return !previouslyRecommendedKeys.Any(previousKey =>
                    TaskSemanticKeyBuilder.IsSimilar(previousKey, candidateKey));
            })
            .ToArray();
    }
}
