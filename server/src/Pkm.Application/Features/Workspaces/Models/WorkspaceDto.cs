using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    WorkspaceVisibility Visibility,
    Guid OwnerId,
    Guid? LastModifiedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    WorkspaceRole? CurrentUserRole,
    bool CanRead,
    bool CanWrite,
    bool CanManageMembers);
