using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Commands.GenerateTaskRecommendations;

public sealed record GenerateTaskRecommendationsCommand(
    Guid WorkspaceId,
    Guid? PageId = null,
    bool Force = false) : ICommand<IReadOnlyList<TaskRecommendationDto>>;
