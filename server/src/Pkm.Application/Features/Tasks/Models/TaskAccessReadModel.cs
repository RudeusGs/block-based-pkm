using Pkm.Domain.Tasks;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Tasks.Models;

public sealed record TaskAccessReadModel(
    Guid TaskId,
    Guid WorkspaceId,
    Guid CreatedById,
    WorkspaceRole? Role,
    StatusWorkTask Status);