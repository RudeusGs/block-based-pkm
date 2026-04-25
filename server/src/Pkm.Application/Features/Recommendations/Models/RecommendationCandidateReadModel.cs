using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Recommendations.Models;

public sealed record RecommendationCandidateReadModel(
    Guid TaskId,
    Guid WorkspaceId,
    Guid? PageId,
    string Title,
    string? Description,
    StatusWorkTask Status,
    PriorityWorkTask Priority,
    DateTimeOffset? DueDate,
    Guid CreatedById,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    bool IsAssignedToCurrentUser,
    bool HasAnyAssignee);