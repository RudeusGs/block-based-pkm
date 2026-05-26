using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceListItemReadModel(
    Guid Id,
    string Name,
    string? Description,
    string? AvatarUrl,
    WorkspaceVisibility Visibility,
    Guid OwnerId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    WorkspaceRole CurrentUserRole);
