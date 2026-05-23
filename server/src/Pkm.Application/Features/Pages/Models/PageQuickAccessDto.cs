namespace Pkm.Application.Features.Pages.Models;

public sealed record PageQuickAccessDto(
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

public sealed record PageQuickAccessPagedResultDto(
    IReadOnlyList<PageQuickAccessDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
