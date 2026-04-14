using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Domain.Realtime;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;

namespace server.Service.Services
{
    public class NotificationService : BaseService, INotificationService
    {
        private readonly IRealtimeNotifier _notifier;

        public NotificationService(
            DataContext dataContext,
            IUserService userService,
            IRealtimeNotifier notifier)
            : base(dataContext, userService)
        {
            _notifier = notifier;
        }

        public async Task<ApiResult> GetNotificationsAsync(int? workspaceId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var query = _dataContext.Notifications
                    .Where(n => n.UserId == userId);

                if (workspaceId.HasValue)
                    query = query.Where(n => n.WorkspaceId == workspaceId.Value);

                var ordered = query.OrderByDescending(n => n.CreatedDate);

                return await GetPagedAsync(ordered, paging, ct);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUnreadCountAsync(int? workspaceId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var query = _dataContext.Notifications.Where(n => n.UserId == userId && !n.IsRead);

                if (workspaceId.HasValue)
                {
                    query = query.Where(n => n.WorkspaceId == workspaceId.Value);
                }

                var count = await query.CountAsync(ct);
                return ApiResult.Success(count);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> MarkAsReadAsync(int notificationId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var notification = await _dataContext.Notifications.FindAsync(new object[] { notificationId }, ct);
                if (notification == null) return ApiResult.Fail("Không tìm thấy thông báo");
                if (notification.UserId != userId) return ApiResult.Fail("Không có quyền");

                notification.MarkAsRead();
                await SaveChangesAsync(ct);

                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> MarkAllAsReadAsync(int? workspaceId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var query = _dataContext.Notifications.Where(n => n.UserId == userId && !n.IsRead);

                if (workspaceId.HasValue)
                    query = query.Where(n => n.WorkspaceId == workspaceId.Value);

                var unread = await query.ToListAsync(ct);
                foreach (var n in unread)
                {
                    n.MarkAsRead();
                }

                if (unread.Any())
                    await SaveChangesAsync(ct);

                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> PushNotificationAsync(
            int userId,
            int? workspaceId,
            NotificationType type,
            string title,
            string message,
            string? referenceId = null,
            string? referenceType = null,
            CancellationToken ct = default)
        {
            try
            {
                var notification = new Notification(userId, workspaceId, type, title, message, referenceId, referenceType);
                _dataContext.Notifications.Add(notification);
                await SaveChangesAsync(ct);

                if (workspaceId.HasValue)
                {
                    await ServiceHelper.SafeNotifyAsync(
                        _notifier,
                        workspaceId.Value,
                        "ReceiveNotification",
                        new { TargetUserId = userId, Notification = notification }
                    );
                }

                return ApiResult.Success(notification);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
