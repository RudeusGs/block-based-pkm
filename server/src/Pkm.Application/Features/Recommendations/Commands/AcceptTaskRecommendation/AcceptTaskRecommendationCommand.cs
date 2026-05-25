using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Commands.AcceptTaskRecommendation;

public sealed record AcceptTaskRecommendationCommand(Guid RecommendationId) : ICommand<TaskRecommendationDto>;
