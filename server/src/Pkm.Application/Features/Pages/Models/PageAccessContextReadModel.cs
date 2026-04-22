using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Pages.Models;

public sealed record PageAccessContextReadModel(
    Guid PageId,
    Guid WorkspaceId,
    Guid OwnerId,
    WorkspaceRole? Role,
    bool IsArchived);