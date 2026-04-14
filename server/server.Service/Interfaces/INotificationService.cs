using server.Domain.Enums;
using server.Service.Models;

namespace server.Service.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResult> GetNotificationsAsync(int? workspaceId, PagingRequest? paging = null, CancellationToken ct = default);

        Task<ApiResult> GetUnreadCountAsync(int? workspaceId, CancellationToken ct = default);

        Task<ApiResult> MarkAsReadAsync(int notificationId, CancellationToken ct = default);

        Task<ApiResult> MarkAllAsReadAsync(int? workspaceId, CancellationToken ct = default);

        Task<ApiResult> PushNotificationAsync(
            int userId,
            int? workspaceId,
            NotificationType type,
            string title,
            string message,
            string? referenceId = null,
            string? referenceType = null,
            CancellationToken ct = default);
    }
}
