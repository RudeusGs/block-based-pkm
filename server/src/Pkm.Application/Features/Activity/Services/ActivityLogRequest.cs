using Pkm.Domain.Audit;

namespace Pkm.Application.Features.Activity.Services;

public sealed record ActivityLogRequest(
    Guid WorkspaceId,
    Guid UserId,
    ActivityAction Action,
    ActivityEntityType EntityType,
    Guid EntityId,
    string? Description = null,
    string? MetadataJson = null,
    string? IpAddress = null);
