using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Commands.RejectTaskRecommendation;

public sealed record RejectTaskRecommendationCommand(Guid RecommendationId) : ICommand<TaskRecommendationDto>;
