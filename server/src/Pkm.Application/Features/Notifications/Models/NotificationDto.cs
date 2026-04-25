using Pkm.Domain.Notifications;

namespace Pkm.Application.Features.Notifications.Models;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    Guid? WorkspaceId,
    NotificationType Type,
    string Title,
    string Message,
    Guid? ReferenceId,
    string? ReferenceType,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);