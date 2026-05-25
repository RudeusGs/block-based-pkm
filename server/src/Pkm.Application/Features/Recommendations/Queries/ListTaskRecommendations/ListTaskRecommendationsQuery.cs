using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Queries.ListTaskRecommendations;

public sealed record ListTaskRecommendationsQuery(
    Guid? WorkspaceId = null,
    StatusTaskRecommendation? Status = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<TaskRecommendationPagedResultDto>;
