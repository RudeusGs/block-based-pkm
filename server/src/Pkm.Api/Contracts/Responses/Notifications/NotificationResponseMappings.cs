using Pkm.Application.Features.Notifications.Models;

namespace Pkm.Api.Contracts.Responses.Notifications;

public static class NotificationResponseMappings
{
    public static NotificationResponse ToResponse(this NotificationDto dto)
        => new(
            dto.Id,
            dto.UserId,
            dto.WorkspaceId,
            dto.Type.ToString(),
            dto.Title,
            dto.Message,
            dto.ReferenceId,
            dto.ReferenceType,
            dto.IsRead,
            dto.ReadAtUtc,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static NotificationPagedResultResponse ToResponse(this NotificationPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);

    public static NotificationUnreadCountResponse ToResponse(this NotificationUnreadCountDto dto)
        => new(
            dto.UserId,
            dto.WorkspaceId,
            dto.UnreadCount);
}