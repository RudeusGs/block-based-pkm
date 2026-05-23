import { computed, reactive } from 'vue'
import { notificationController } from '@/api/services/notification.api'
import type { Guid } from '@/api/models/common.model'
import type { NotificationResponse } from '@/api/models/notification.model'
import { getApiErrorMessage } from '@/api/utils/api-error.util'
import { useToast } from '@/components/composables/useToast'
import { realtimeClient } from '@/realtime/realtime.client'
import type { RealtimeEnvelope } from '@/realtime/realtime.types'

interface UnreadCountPayload {
  userId?: Guid
  workspaceId?: Guid | null
  unreadCount?: number
  totalUnreadCount?: number
}

const state = reactive({
  notifications: [] as NotificationResponse[],
  unreadCount: 0,
  isLoading: false,
  isMarkingAllRead: false,
  error: null as string | null,
})

let isRealtimeBound = false
const unsubscribeRealtimeHandlers: Array<() => void> = []

function normalizeString(value: unknown) {
  return typeof value === 'string' ? value : ''
}

function normalizeNullableString(value: unknown) {
  if (typeof value !== 'string') return null

  const trimmed = value.trim()
  return trimmed ? trimmed : null
}

function normalizeBoolean(value: unknown) {
  return value === true
}

function unwrapRealtimePayload<TPayload>(envelope: RealtimeEnvelope<TPayload>) {
  return envelope.payload
}

function normalizeNotificationPayload(
  envelope: RealtimeEnvelope<unknown>
): NotificationResponse | null {
  const raw = unwrapRealtimePayload(envelope) as Record<string, unknown> | null

  if (!raw || typeof raw !== 'object') return null

  const id = normalizeString(raw.id ?? raw.Id)
  const userId = normalizeString(raw.userId ?? raw.UserId)
  const title = normalizeString(raw.title ?? raw.Title)
  const message = normalizeString(raw.message ?? raw.Message)

  if (!id || !userId || !title) return null

  return {
    id,
    userId,
    workspaceId: normalizeNullableString(raw.workspaceId ?? raw.WorkspaceId),
    type: normalizeString(raw.type ?? raw.Type) || 'General',
    title,
    message,
    referenceId: normalizeNullableString(raw.referenceId ?? raw.ReferenceId),
    referenceType: normalizeNullableString(raw.referenceType ?? raw.ReferenceType),
    isRead: normalizeBoolean(raw.isRead ?? raw.IsRead),
    readAtUtc: normalizeNullableString(raw.readAtUtc ?? raw.ReadAtUtc),
    createdDate:
      normalizeString(raw.createdDate ?? raw.CreatedDate) ||
      envelope.occurredAtUtc ||
      new Date().toISOString(),
    updatedDate: normalizeNullableString(raw.updatedDate ?? raw.UpdatedDate),
  }
}

function normalizeUnreadCountPayload(
  envelope: RealtimeEnvelope<unknown>
): UnreadCountPayload | null {
  const raw = unwrapRealtimePayload(envelope) as Record<string, unknown> | null

  if (!raw || typeof raw !== 'object') return null

  const unreadCount = Number(raw.unreadCount ?? raw.UnreadCount ?? 0)
  const totalUnreadCount = Number(
    raw.totalUnreadCount ?? raw.TotalUnreadCount ?? unreadCount
  )

  return {
    userId: normalizeNullableString(raw.userId ?? raw.UserId) ?? undefined,
    workspaceId: normalizeNullableString(raw.workspaceId ?? raw.WorkspaceId),
    unreadCount: Number.isFinite(unreadCount) ? unreadCount : 0,
    totalUnreadCount: Number.isFinite(totalUnreadCount)
      ? totalUnreadCount
      : unreadCount,
  }
}

function upsertNotification(notification: NotificationResponse) {
  const existingIndex = state.notifications.findIndex(
    (item) => item.id === notification.id
  )

  if (existingIndex >= 0) {
    state.notifications.splice(existingIndex, 1, notification)
    return
  }

  state.notifications.unshift(notification)

  if (state.notifications.length > 20) {
    state.notifications.splice(20)
  }
}

async function fetchNotifications() {
  state.isLoading = true
  state.error = null

  try {
    const result = await notificationController.list({
      pageNumber: 1,
      pageSize: 12,
    })

    if (!result.isSuccess || !result.data) {
      state.error = result.message || 'Không thể tải notifications.'
      return
    }

    state.notifications = result.data.items
  } catch (error) {
    state.error = getApiErrorMessage(error, 'Không thể tải notifications.')
  } finally {
    state.isLoading = false
  }
}

async function fetchUnreadCount() {
  try {
    const result = await notificationController.getUnreadCount()

    if (result.isSuccess && result.data) {
      state.unreadCount = result.data.unreadCount
    }
  } catch {
    // Silent fallback.
  }
}

async function markAsRead(notificationId: Guid) {
  const current = state.notifications.find((item) => item.id === notificationId)
  if (!current || current.isRead) return

  current.isRead = true
  state.unreadCount = Math.max(0, state.unreadCount - 1)

  try {
    const result = await notificationController.markAsRead(notificationId)

    if (!result.isSuccess || !result.data) {
      current.isRead = false
      state.unreadCount += 1
      return
    }

    upsertNotification(result.data)
    await fetchUnreadCount()
  } catch {
    current.isRead = false
    state.unreadCount += 1
  }
}

async function markAllAsRead() {
  state.isMarkingAllRead = true

  const snapshot = state.notifications.map((item) => ({ ...item }))
  const previousUnreadCount = state.unreadCount

  state.notifications.forEach((item) => {
    item.isRead = true
    item.readAtUtc = item.readAtUtc ?? new Date().toISOString()
  })

  state.unreadCount = 0

  try {
    const result = await notificationController.markAllAsRead()

    if (!result.isSuccess) {
      state.notifications = snapshot
      state.unreadCount = previousUnreadCount
      return
    }

    state.unreadCount = result.data?.unreadCount ?? 0
    await fetchNotifications()
  } catch {
    state.notifications = snapshot
    state.unreadCount = previousUnreadCount
  } finally {
    state.isMarkingAllRead = false
  }
}

function bindRealtime() {
  if (isRealtimeBound) return

  isRealtimeBound = true

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('NotificationCreated', async (envelope) => {
      const notification = normalizeNotificationPayload(envelope)

      if (!notification) {
        await fetchNotifications()
        await fetchUnreadCount()
        return
      }

      upsertNotification(notification)

      if (!notification.isRead) {
        state.unreadCount += 1
      }

      const toast = useToast()
      toast.info(notification.title, notification.message, 4500)

      await fetchUnreadCount()
    })
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('NotificationReadChanged', async (envelope) => {
      const notification = normalizeNotificationPayload(envelope)

      if (notification) {
        upsertNotification(notification)
      }

      await fetchUnreadCount()
    })
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('NotificationUnreadCountChanged', (envelope) => {
      const payload = normalizeUnreadCountPayload(envelope)

      if (!payload) return

      state.unreadCount =
        payload.totalUnreadCount ?? payload.unreadCount ?? state.unreadCount
    })
  )
}

async function startRealtime() {
  bindRealtime()
  await realtimeClient.start()
}

function unbindRealtime() {
  while (unsubscribeRealtimeHandlers.length > 0) {
    const unsubscribe = unsubscribeRealtimeHandlers.pop()
    unsubscribe?.()
  }

  isRealtimeBound = false
}

export function useNotificationCenter() {
  const unreadCountLabel = computed(() => {
    if (state.unreadCount <= 0) return ''
    return state.unreadCount > 99 ? '99+' : String(state.unreadCount)
  })

  return {
    notifications: computed(() => state.notifications),
    unreadCount: computed(() => state.unreadCount),
    unreadCountLabel,
    isLoading: computed(() => state.isLoading),
    isMarkingAllRead: computed(() => state.isMarkingAllRead),
    error: computed(() => state.error),

    realtimeStatus: realtimeClient.status,
    realtimeError: realtimeClient.error,
    isRealtimeConnected: realtimeClient.isConnected,

    fetchNotifications,
    fetchUnreadCount,
    markAsRead,
    markAllAsRead,
    startRealtime,
    bindRealtime,
    unbindRealtime,
  }
}
