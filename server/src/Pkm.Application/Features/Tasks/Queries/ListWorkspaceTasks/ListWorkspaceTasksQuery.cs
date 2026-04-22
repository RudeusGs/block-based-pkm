using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Queries.ListWorkspaceTasks;

public sealed record ListWorkspaceTasksQuery(
    Guid WorkspaceId,
    string? Keyword = null,
    StatusWorkTask? Status = null,
    PriorityWorkTask? Priority = null,
    Guid? AssigneeUserId = null,
    DateTimeOffset? DueFrom = null,
    DateTimeOffset? DueTo = null,
    bool IncludeCompleted = true,
    int PageNumber = 1,
    int PageSize = 20);