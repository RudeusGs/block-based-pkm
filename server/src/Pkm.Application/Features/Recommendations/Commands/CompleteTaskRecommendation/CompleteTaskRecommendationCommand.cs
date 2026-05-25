using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Commands.CompleteTaskRecommendation;

public sealed record CompleteTaskRecommendationCommand(
    Guid RecommendationId,
    string? Notes = null) : ICommand<TaskRecommendationDto>;
