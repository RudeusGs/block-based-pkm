using Pkm.Domain.Audit;

namespace Pkm.Application.Features.Activity.Models;

public sealed record ActivityLogDto(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    string? UserName,
    string? UserFullName,
    string? UserAvatarUrl,
    ActivityAction Action,
    ActivityEntityType EntityType,
    Guid EntityId,
    string? Description,
    string? MetadataJson,
    string? IpAddress,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedDate);
