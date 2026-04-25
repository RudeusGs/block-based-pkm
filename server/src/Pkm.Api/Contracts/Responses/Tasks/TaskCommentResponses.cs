namespace Pkm.Api.Contracts.Responses.Tasks;

public sealed record TaskCommentResponse(
    Guid Id,
    Guid TaskId,
    Guid UserId,
    Guid? ParentId,
    string Content,
    bool IsDeleted,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    DateTimeOffset? DeletedDate);

public sealed record TaskCommentPagedResultResponse(
    IReadOnlyList<TaskCommentResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);