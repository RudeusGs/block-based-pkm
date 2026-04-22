namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceDetailReadModel(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    Guid? LastModifiedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);