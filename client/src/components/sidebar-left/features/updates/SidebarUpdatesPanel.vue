<template>
  <section class="lunar-panel">
    <div class="lunar-panel-head">
      <span>Updates</span>

      <div class="lunar-panel-actions">
        <button
          type="button"
          title="Đánh dấu tất cả đã đọc"
          @click.stop="emit('markAll')"
        >
          <i class="bi bi-check2-all"></i>
        </button>

        <button
          type="button"
          title="Tải lại"
          @click.stop="emit('refresh')"
        >
          <i class="bi bi-arrow-clockwise"></i>
        </button>
      </div>
    </div>

    <div
      v-if="isLoading"
      class="lunar-empty compact"
    >
      Đang tải updates…
    </div>

    <div
      v-else-if="error"
      class="lunar-error compact"
    >
      <p>{{ error }}</p>
    </div>

    <div
      v-else-if="!notifications.length"
      class="lunar-empty compact"
    >
      Chưa có cập nhật mới.
    </div>

    <div
      v-else
      class="lunar-update-list"
    >
      <article
        v-for="notification in notifications"
        :key="notification.id"
        class="lunar-update-item"
        :class="{ unread: !notification.isRead }"
      >
        <span class="lunar-update-icon">
          <i :class="['bi', notificationIcon(notification.type)]"></i>
        </span>

        <span class="lunar-update-main">
          <span class="lunar-update-title">
            {{ notification.title }}
          </span>

          <span class="lunar-update-message">
            {{ notification.message }}
          </span>

          <span class="lunar-update-time">
            {{ formatDateTime(notification.createdDate) }}
          </span>
        </span>

        <button
          v-if="!notification.isRead"
          type="button"
          class="lunar-dot-btn"
          title="Đánh dấu đã đọc"
          @click.stop="emit('markRead', notification.id)"
        >
          <i class="bi bi-check"></i>
        </button>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { Guid } from '@/api/models/common.model'
import type { NotificationResponse } from '@/api/models/notification.model'
import {
  formatDateTime,
  notificationIcon,
} from '../../utils/sidebar-format.util'

defineProps<{
  notifications: NotificationResponse[]
  isLoading: boolean
  error: string | null
}>()

const emit = defineEmits<{
  refresh: []
  markAll: []
  markRead: [notificationId: Guid]
}>()
</script>