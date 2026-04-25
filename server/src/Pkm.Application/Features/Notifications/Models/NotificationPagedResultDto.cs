namespace Pkm.Application.Features.Notifications.Models;

public sealed record NotificationPagedResultDto(
    IReadOnlyList<NotificationDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);