namespace Pkm.Application.Features.Recommendations.Commands.RejectTaskRecommendation;

public sealed record RejectTaskRecommendationCommand(
    Guid RecommendationId);