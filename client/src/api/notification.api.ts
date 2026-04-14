import api from './base.api'
import type {
  GetNotificationsQuery,
  GetUnreadCountQuery,
  MarkAllAsReadQuery,
} from '@/models/notification.model'

export const NotificationAPI = {
  getNotifications: (query?: GetNotificationsQuery) =>
    api.get('notifications', query),

  getUnreadCount: (query?: GetUnreadCountQuery) =>
    api.get('notifications/unread-count', query),

  markAsRead: (notificationId: number) =>
    api.put(`notifications/${notificationId}/read`),

  markAllAsRead: (query?: MarkAllAsReadQuery) =>
    api.put('notifications/read-all', query),
}