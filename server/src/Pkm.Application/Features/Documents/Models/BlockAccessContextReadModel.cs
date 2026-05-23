using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Documents.Models;

public sealed record BlockAccessContextReadModel(
    Guid BlockId,
    Guid PageId,
    Guid WorkspaceId,
    Guid OwnerId,
    WorkspaceRole? Role,
    WorkspaceVisibility Visibility,
    bool IsPageArchived);
