namespace Pkm.Api.Contracts.Responses.Pages;

public sealed record PageResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ParentPageId,
    string Title,
    string? Icon,
    string? CoverImage,
    bool IsArchived,
    DateTimeOffset? ArchivedAt,
    long CurrentRevision,
    Guid CreatedBy,
    Guid? LastModifiedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record PagePagedResultResponse(
    IReadOnlyList<PageResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PagePresenceUserResponse(
    Guid UserId,
    string? UserName,
    int ConnectionCount,
    DateTimeOffset LastSeenUtc);

public sealed record PagePresenceResponse(
    Guid WorkspaceId,
    Guid PageId,
    IReadOnlyList<PagePresenceUserResponse> ActiveUsers);