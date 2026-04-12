using server.Domain.Enums;
using server.Service.Models;

namespace server.Service.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResult> GetNotificationsAsync(int userId, int? workspaceId, int pageIndex = 1, int pageSize = 20, CancellationToken ct = default);

        Task<ApiResult> GetUnreadCountAsync(int userId, int? workspaceId, CancellationToken ct = default);

        Task<ApiResult> MarkAsReadAsync(int notificationId, int userId, CancellationToken ct = default);

        Task<ApiResult> MarkAllAsReadAsync(int userId, int? workspaceId, CancellationToken ct = default);

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
