namespace Pkm.Application.Features.Recommendations.Models;

public sealed record TaskRecommendationPagedResultDto(
    IReadOnlyList<TaskRecommendationDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);