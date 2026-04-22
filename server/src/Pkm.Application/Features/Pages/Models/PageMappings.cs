using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Pages.Models;

public static class PageMappings
{
    public static PageDto ToDto(this Page page)
        => new(
            page.Id,
            page.WorkspaceId,
            page.ParentPageId,
            page.Title,
            page.Icon,
            page.CoverImage,
            page.IsArchived,
            page.ArchivedAt,
            page.CurrentRevision,
            page.CreatedBy,
            page.LastModifiedBy,
            page.CreatedDate,
            page.UpdatedDate);
}