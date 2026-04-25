namespace Pkm.Application.Features.Recommendations.Commands.CompleteTaskRecommendation;

public sealed record CompleteTaskRecommendationCommand(
    Guid RecommendationId,
    string? Notes = null);