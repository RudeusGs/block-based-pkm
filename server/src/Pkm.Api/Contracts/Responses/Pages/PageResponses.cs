using Pkm.Api.Contracts.Responses.Blocks;

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
    bool IsPublished,
    string? PublicToken,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy,
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


public sealed record PageQuickAccessResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ParentPageId,
    string WorkspaceName,
    string Title,
    string? Icon,
    string? CoverImage,
    bool IsArchived,
    long CurrentRevision,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    bool IsFavorite,
    DateTimeOffset? FavoritedAtUtc,
    DateTimeOffset? LastVisitedAtUtc,
    int VisitCount);

public sealed record PageQuickAccessPagedResultResponse(
    IReadOnlyList<PageQuickAccessResponse> Items,
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





public sealed record PagePublishResponse(
    Guid PageId,
    bool IsPublished,
    string? PublicToken,
    string? PublicViewerPath,
    string? PublicApiPath,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy);

public sealed record PublishedBlockResponse(
    Guid Id,
    Guid? ParentBlockId,
    string Type,
    string? TextContent,
    string? PropsJson,
    int SchemaVersion,
    string OrderKey,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record PublishedPageDocumentResponse(
    Guid PageId,
    string Title,
    string? Icon,
    string? CoverImage,
    long CurrentRevision,
    DateTimeOffset PublishedAt,
    IReadOnlyList<PublishedBlockResponse> Blocks,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
