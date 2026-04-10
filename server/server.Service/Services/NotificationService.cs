using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Infrastructure.Realtime.Interfaces;
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

        public async Task<ApiResult> GetNotificationsAsync(int userId, int? workspaceId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var query = _dataContext.Notifications.Where(n => n.UserId == userId);

                if (workspaceId.HasValue)
                {
                    query = query.Where(n => n.WorkspaceId == workspaceId.Value);
                }

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return ApiResult.Success(new
                {
                    Items = notifications,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUnreadCountAsync(int userId, int? workspaceId)
        {
            try
            {
                var query = _dataContext.Notifications.Where(n => n.UserId == userId && !n.IsRead);

                if (workspaceId.HasValue)
                {
                    query = query.Where(n => n.WorkspaceId == workspaceId.Value);
                }

                var count = await query.CountAsync();
                return ApiResult.Success(count);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _dataContext.Notifications.FindAsync(notificationId);
                if (notification == null) return ApiResult.Fail("Không tìm thấy thông báo");
                if (notification.UserId != userId) return ApiResult.Fail("Không có quyền");

                notification.MarkAsRead();
                await SaveChangesAsync();

                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> MarkAllAsReadAsync(int userId, int? workspaceId)
        {
            try
            {
                var query = _dataContext.Notifications.Where(n => n.UserId == userId && !n.IsRead);

                if (workspaceId.HasValue)
                    query = query.Where(n => n.WorkspaceId == workspaceId.Value);

                var unread = await query.ToListAsync();
                foreach (var n in unread)
                {
                    n.MarkAsRead();
                }

                if (unread.Any())
                    await SaveChangesAsync();

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
            string? referenceType = null)
        {
            try
            {
                var notification = new Notification(userId, workspaceId, type, title, message, referenceId, referenceType);
                _dataContext.Notifications.Add(notification);
                await SaveChangesAsync();

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
