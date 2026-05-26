using AppDocuments = Pkm.Application.Features.Documents.Models;
using AppPages = Pkm.Application.Features.Pages.Models;

namespace Pkm.Api.Contracts.Responses.Pages;

public static class PageResponseMappings
{
    public static PageQuickAccessResponse ToResponse(this AppPages.PageQuickAccessDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.ParentPageId,
            dto.WorkspaceName,
            dto.Title,
            dto.Icon,
            dto.CoverImage,
            dto.IsArchived,
            dto.CurrentRevision,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.IsFavorite,
            dto.FavoritedAtUtc,
            dto.LastVisitedAtUtc,
            dto.VisitCount);

    public static PageQuickAccessPagedResultResponse ToResponse(this AppPages.PageQuickAccessPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);

    public static PagePresenceUserResponse ToResponse(this AppDocuments.PagePresenceUserDto dto)
        => new(
            dto.UserId,
            dto.UserName,
            dto.ConnectionCount,
            dto.LastSeenUtc);

    public static PagePresenceResponse ToResponse(this AppDocuments.PagePresenceDto dto)
        => new(
            dto.WorkspaceId,
            dto.PageId,
            dto.ActiveUsers.Select(x => x.ToResponse()).ToArray());

    public static PageResponse ToResponse(this AppPages.PageDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.ParentPageId,
            dto.Title,
            dto.Icon,
            dto.CoverImage,
            dto.IsArchived,
            dto.ArchivedAt,
            dto.CurrentRevision,
            dto.CreatedBy,
            dto.LastModifiedBy,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static PagePagedResultResponse ToResponse(this AppPages.PagePagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);
}
