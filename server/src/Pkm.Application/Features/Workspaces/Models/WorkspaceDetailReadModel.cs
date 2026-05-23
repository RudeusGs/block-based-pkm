using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceDetailReadModel(
    Guid Id,
    string Name,
    string? Description,
    WorkspaceVisibility Visibility,
    Guid OwnerId,
    Guid? LastModifiedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

