using Pkm.Application.Features.Activity.Models;

namespace Pkm.Api.Contracts.Responses.ActivityLogs;

public sealed record ActivityLogResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    string? UserName,
    string? UserFullName,
    string? UserAvatarUrl,
    string Action,
    string EntityType,
    Guid EntityId,
    string? Description,
    string? MetadataJson,
    string? IpAddress,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedDate);

public sealed record ActivityLogPagedResultResponse(
    IReadOnlyList<ActivityLogResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public static class ActivityLogResponseMappings
{
    public static ActivityLogResponse ToResponse(this ActivityLogDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.UserId,
            dto.UserName,
            dto.UserFullName,
            dto.UserAvatarUrl,
            dto.Action.ToString(),
            dto.EntityType.ToString(),
            dto.EntityId,
            dto.Description,
            dto.MetadataJson,
            dto.IpAddress,
            dto.OccurredAt,
            dto.CreatedDate);

    public static ActivityLogPagedResultResponse ToResponse(this ActivityLogPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);
}
