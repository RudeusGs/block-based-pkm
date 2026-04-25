namespace Pkm.Application.Features.Recommendations.Commands.GenerateTaskRecommendations;

public sealed record GenerateTaskRecommendationsCommand(
    Guid WorkspaceId,
    Guid? PageId = null,
    bool Force = false);