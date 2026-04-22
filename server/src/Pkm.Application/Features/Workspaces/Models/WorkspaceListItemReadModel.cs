using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceListItemReadModel(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    WorkspaceRole CurrentUserRole);