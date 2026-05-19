using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceInvitationDto(
    Guid Id,
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    Guid InvitedByUserId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);
