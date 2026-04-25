namespace Pkm.Application.Features.Recommendations.Commands.AcceptTaskRecommendation;

public sealed record AcceptTaskRecommendationCommand(
    Guid RecommendationId);