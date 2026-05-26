using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Services;

public interface IRecommendationCandidateDeduplicator
{
    Task<IReadOnlyList<RecommendationCandidateReadModel>> RemovePreviouslyRecommendedSemanticDuplicatesAsync(
        IReadOnlyList<RecommendationCandidateReadModel> candidates,
        IReadOnlySet<Guid> previouslyRecommendedTaskIds,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
