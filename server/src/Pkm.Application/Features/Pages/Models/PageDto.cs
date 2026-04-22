namespace Pkm.Application.Features.Pages.Models;

public sealed record PageDto(
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