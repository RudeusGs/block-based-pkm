using Pkm.Api.Contracts.Responses.Auth;
using Pkm.Api.Contracts.Responses.Blocks;
using Pkm.Api.Contracts.Responses.Pages;
using Pkm.Api.Contracts.Responses.Workspaces;
using AppAuth = Pkm.Application.Features.Authentication.Models;
using AppDocuments = Pkm.Application.Features.Documents.Models;
using AppPages = Pkm.Application.Features.Pages.Models;
using AppWorkspaces = Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Api.Contracts.Responses;

public static class ResponseMappings
{
    public static AuthUserResponse ToResponse(this AppAuth.AuthUserDto dto)
        => new(
            dto.Id,
            dto.UserName,
            dto.Email,
            dto.FullName,
            dto.AvatarUrl,
            dto.Status,
            dto.IsAuthenticated);

    public static AuthTokenResponse ToResponse(this AppAuth.AuthTokenDto dto)
        => new(
            dto.AccessToken,
            dto.TokenType,
            dto.ExpiresIn,
            dto.User.ToResponse());

    public static BlockResponse ToResponse(this AppDocuments.BlockDto dto)
        => new(
            dto.Id,
            dto.PageId,
            dto.ParentBlockId,
            dto.Type,
            dto.TextContent,
            dto.PropsJson,
            dto.SchemaVersion,
            dto.OrderKey,
            dto.CreatedBy,
            dto.LastModifiedBy,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static BlockLeaseResponse ToResponse(this AppDocuments.BlockLeaseDto dto)
        => new(
            dto.BlockId,
            dto.PageId,
            dto.Granted,
            dto.Status,
            dto.HolderUserId,
            dto.HolderDisplayName,
            dto.ExpiresAtUtc,
            dto.IsHeldByCurrentUser);

    public static BlockMutationResponse ToResponse(this AppDocuments.BlockMutationDto dto)
        => new(
            dto.PageId,
            dto.BlockId,
            dto.AppliedRevision,
            dto.Block?.ToResponse());

    public static PageDocumentResponse ToResponse(this AppDocuments.PageDocumentDto dto)
        => new(
            dto.PageId,
            dto.CurrentRevision,
            dto.Blocks.Select(x => x.ToResponse()).ToArray());

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

    public static WorkspaceResponse ToResponse(this AppWorkspaces.WorkspaceDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.OwnerId,
            dto.LastModifiedBy,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.CurrentUserRole?.ToString(),
            dto.CanRead,
            dto.CanWrite,
            dto.CanManageMembers);

    public static WorkspaceListItemResponse ToResponse(this AppWorkspaces.WorkspaceListItemDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.OwnerId,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.CurrentUserRole.ToString());

    public static WorkspaceMemberResponse ToResponse(this AppWorkspaces.WorkspaceMemberDto dto)
        => new(
            dto.WorkspaceId,
            dto.UserId,
            dto.Role.ToString(),
            dto.IsOwner,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static WorkspacePagedResultResponse ToResponse(this AppWorkspaces.WorkspacePagedResultDto dto)
        => new(
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.Items.Select(x => x.ToResponse()).ToArray());
}