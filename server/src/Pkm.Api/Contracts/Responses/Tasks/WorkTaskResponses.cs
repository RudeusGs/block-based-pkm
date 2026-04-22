namespace Pkm.Api.Contracts.Responses.Tasks;

public sealed record WorkTaskAssigneeResponse(
    Guid UserId);

public sealed record WorkTaskResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? PageId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTimeOffset? DueDate,
    Guid CreatedById,
    Guid? LastModifiedById,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    IReadOnlyList<WorkTaskAssigneeResponse> Assignees);

public sealed record WorkTaskPagedResultResponse(
    IReadOnlyList<WorkTaskResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);