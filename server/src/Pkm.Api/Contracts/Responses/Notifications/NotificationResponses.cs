using Pkm.Domain.Notifications;

namespace Pkm.Api.Contracts.Responses.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid UserId,
    Guid? WorkspaceId,
    string Type,
    string Title,
    string Message,
    Guid? ReferenceId,
    string? ReferenceType,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record NotificationPagedResultResponse(
    IReadOnlyList<NotificationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record NotificationUnreadCountResponse(
    Guid UserId,
    Guid? WorkspaceId,
    int UnreadCount);