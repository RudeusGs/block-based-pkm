import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  GetUnreadNotificationCountParams,
  ListNotificationsParams,
  MarkAllNotificationsAsReadParams,
  NotificationPagedResultResponse,
  NotificationResponse,
  NotificationUnreadCountResponse,
} from '../models/notification.model'

export const notificationController = {
  list(params?: ListNotificationsParams) {
    return api.get<ApiResult<NotificationPagedResultResponse>>(
      'notifications',
      params
    )
  },

  getUnreadCount(params?: GetUnreadNotificationCountParams) {
    return api.get<ApiResult<NotificationUnreadCountResponse>>(
      'notifications/unread-count',
      params
    )
  },

  markAsRead(notificationId: Guid) {
    return api.patch<ApiResult<NotificationResponse>>(
      `notifications/${notificationId}:read`
    )
  },

  markAsUnread(notificationId: Guid) {
    return api.patch<ApiResult<NotificationResponse>>(
      `notifications/${notificationId}:unread`
    )
  },

  markAllAsRead(params?: MarkAllNotificationsAsReadParams) {
    return api.post<ApiResult<NotificationUnreadCountResponse>>(
      'notifications/mark-all-read',
      undefined,
      { params }
    )
  },

  delete(notificationId: Guid) {
    return api.delete<ApiResult>(`notifications/${notificationId}`)
  },
}
