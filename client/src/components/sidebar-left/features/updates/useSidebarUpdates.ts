import { ref } from 'vue'
import { notificationController } from '@/api/services/notification.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { NotificationResponse } from '@/api/models/notification.model'

export function useSidebarUpdates() {
  const notifications = ref<NotificationResponse[]>([])
  const unreadNotificationCount = ref(0)
  const isLoadingNotifications = ref(false)
  const notificationError = ref<string | null>(null)

  async function fetchUnreadCount(workspaceId: Guid | null) {
    try {
      const result = await notificationController.getUnreadCount({
        workspaceId: workspaceId ?? null,
      })

      if (result.isSuccess && result.data) {
        unreadNotificationCount.value = result.data.unreadCount
      }
    } catch {
      // Badge phụ, không phá UI nếu API lỗi.
    }
  }

  async function fetchNotifications(workspaceId: Guid | null) {
    if (isLoadingNotifications.value) return

    isLoadingNotifications.value = true
    notificationError.value = null

    try {
      const result = await notificationController.list({
        workspaceId: workspaceId ?? null,
        pageNumber: 1,
        pageSize: 8,
      })

      if (!result.isSuccess || !result.data) {
        notificationError.value = getApiResultErrorMessage(
          result,
          'Không thể tải cập nhật.'
        )
        return
      }

      notifications.value = result.data.items
    } catch (error) {
      notificationError.value = getApiErrorMessage(
        error,
        'Không thể tải cập nhật.'
      )
    } finally {
      isLoadingNotifications.value = false
    }
  }

  async function markNotificationAsRead(notificationId: Guid, workspaceId: Guid | null) {
    try {
      const result = await notificationController.markAsRead(notificationId)

      if (!result.isSuccess || !result.data) return

      notifications.value = notifications.value.map((notification) => {
        if (notification.id !== notificationId) return notification
        return result.data as NotificationResponse
      })

      void fetchUnreadCount(workspaceId)
    } catch {
      // Không cần hiện lỗi lớn cho thao tác phụ.
    }
  }

  async function markAllNotificationsAsRead(workspaceId: Guid | null) {
    try {
      const result = await notificationController.markAllAsRead({
        workspaceId: workspaceId ?? null,
      })

      if (result.isSuccess && result.data) {
        unreadNotificationCount.value = result.data.unreadCount
      }

      notifications.value = notifications.value.map((notification) => ({
        ...notification,
        isRead: true,
      }))
    } catch (error) {
      notificationError.value = getApiErrorMessage(
        error,
        'Không thể đánh dấu đã đọc.'
      )
    }
  }

  return {
    notifications,
    unreadNotificationCount,
    isLoadingNotifications,
    notificationError,
    fetchUnreadCount,
    fetchNotifications,
    markNotificationAsRead,
    markAllNotificationsAsRead,
  }
}
