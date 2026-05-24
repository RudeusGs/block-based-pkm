<template>
  <section class="notion-sidebar-panel updates-panel">
    <header class="notion-panel-header">
      <div class="notion-panel-title">
        <strong>Cập nhật</strong>
        <span>{{ unreadCount > 0 ? `${unreadCount} chưa đọc` : 'Đã đọc hết' }}</span>
      </div>

      <div class="notion-panel-actions">
        <button
          type="button"
          class="notion-panel-action"
          title="Đánh dấu tất cả đã đọc"
          :disabled="!unreadCount || isLoading"
          @click.stop="emit('markAll')"
        >
          <i class="bi bi-check2-all"></i>
        </button>

        <button
          type="button"
          class="notion-panel-action"
          title="Tải lại"
          :disabled="isLoading"
          @click.stop="emit('refresh')"
        >
          <i
            class="bi bi-arrow-clockwise"
            :class="{ spinning: isLoading }"
          ></i>
        </button>
      </div>
    </header>

    <div
      v-if="isLoading"
      class="notion-skeleton-list"
    >
      <div
        v-for="index in 3"
        :key="index"
        class="notion-skeleton-row"
      >
        <span></span>
        <span></span>
      </div>
    </div>

    <div
      v-else-if="error"
      class="notion-empty-state error"
    >
      <strong>Không thể tải cập nhật</strong>
      <p>{{ error }}</p>

      <button
        type="button"
        @click.stop="emit('refresh')"
      >
        Thử lại
      </button>
    </div>

    <div
      v-else-if="!notifications.length"
      class="notion-empty-state"
    >
      <strong>Chưa có cập nhật mới</strong>
      <p>Mọi hoạt động quan trọng trong không gian sẽ hiện ở đây.</p>
    </div>

    <div
      v-else
      class="notion-update-list"
    >
      <article
        v-for="notification in notifications"
        :key="notification.id"
        class="notion-update-row"
        :class="{ unread: !notification.isRead }"
      >
        <span
          v-if="!notification.isRead"
          class="notion-unread-dot"
          title="Chưa đọc"
        ></span>

        <div class="notion-update-content">
          <div class="notion-update-topline">
            <span class="notion-update-type">
              {{ notificationTypeLabel(notification.type) }}
            </span>

            <time>
              {{ formatDateTime(notification.createdDate) }}
            </time>
          </div>

          <h4>{{ notification.title }}</h4>

          <p>{{ notification.message }}</p>
        </div>

        <button
          v-if="!notification.isRead"
          type="button"
          class="notion-read-button"
          title="Đánh dấu đã đọc"
          @click.stop="emit('markRead', notification.id)"
        >
          <i class="bi bi-check2"></i>
        </button>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Guid } from '@/api/models/common.model'
import type { NotificationResponse } from '@/api/models/notification.model'
import { formatDateTime } from '../../utils/sidebar-format.util'

const props = defineProps<{
  notifications: NotificationResponse[]
  isLoading: boolean
  error: string | null
}>()

const emit = defineEmits<{
  refresh: []
  markAll: []
  markRead: [notificationId: Guid]
}>()

const unreadCount = computed(() => {
  return props.notifications.filter((notification) => !notification.isRead)
    .length
})

function normalize(value: string | null | undefined) {
  return value?.trim().toLowerCase() ?? ''
}

function notificationTypeLabel(type: string) {
  const normalized = normalize(type)

  if (!normalized) return 'Cập nhật'
  if (normalized.includes('task')) return 'Công việc'
  if (normalized.includes('comment')) return 'Bình luận'
  if (normalized.includes('recommend')) return 'AI'
  if (normalized.includes('workspace')) return 'Không gian'
  if (normalized.includes('page')) return 'Trang'

  return type
}
</script>

<style scoped>
.notion-sidebar-panel {
  margin: 2px 0 10px;
  padding: 4px 0 8px;
  border-bottom: 1px solid var(--sidebar-border);
}

.notion-panel-header {
  min-height: 30px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 0 4px 4px 7px;
}

.notion-panel-title {
  min-width: 0;
  display: flex;
  align-items: baseline;
  gap: 7px;
}

.notion-panel-title strong {
  color: var(--sidebar-text);
  font-size: 12px;
  font-weight: 700;
}

.notion-panel-title span {
  color: var(--sidebar-faint);
  font-size: 11px;
}

.notion-panel-actions {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}

.notion-panel-action,
.notion-read-button {
  width: 24px;
  height: 24px;
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--sidebar-faint);
  background: transparent;
}

.notion-panel-action:hover:not(:disabled),
.notion-read-button:hover {
  color: var(--sidebar-text);
  background: var(--sidebar-bg-hover);
}

.notion-panel-action i,
.notion-read-button i {
  font-size: 13px;
}

.notion-panel-action i.spinning {
  animation: sidebar-spin 0.8s linear infinite;
}

.notion-update-list,
.notion-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.notion-update-row {
  position: relative;
  min-width: 0;
  border-radius: 6px;
  display: flex;
  align-items: flex-start;
  gap: 7px;
  padding: 6px 6px 6px 7px;
  color: var(--sidebar-muted);
}

.notion-update-row:hover {
  background: var(--sidebar-bg-hover);
}

.notion-unread-dot {
  width: 6px;
  height: 6px;
  margin-top: 7px;
  border-radius: 999px;
  flex-shrink: 0;
  background: var(--sidebar-text);
}

.notion-update-row:not(.unread) {
  padding-left: 20px;
}

.notion-update-content {
  min-width: 0;
  flex: 1;
}

.notion-update-topline {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--sidebar-faint);
  font-size: 10.5px;
  line-height: 1.3;
}

.notion-update-topline time::before {
  content: '·';
  margin-right: 6px;
  color: var(--sidebar-border-strong);
}

.notion-update-type {
  color: var(--sidebar-muted);
}

.notion-update-content h4 {
  overflow: hidden;
  margin: 2px 0 0;
  color: var(--sidebar-text);
  font-size: 13px;
  font-weight: 650;
  line-height: 1.35;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.notion-update-content p {
  display: -webkit-box;
  overflow: hidden;
  margin: 2px 0 0;
  color: var(--sidebar-faint);
  font-size: 11.5px;
  line-height: 1.35;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.notion-empty-state {
  padding: 8px 7px 10px;
  color: var(--sidebar-faint);
  font-size: 12px;
  line-height: 1.45;
}

.notion-empty-state strong {
  display: block;
  color: var(--sidebar-muted);
  font-size: 12.5px;
  font-weight: 650;
}

.notion-empty-state p {
  margin: 3px 0 0;
}

.notion-empty-state.error strong {
  color: var(--sidebar-danger);
}

.notion-empty-state button {
  margin-top: 7px;
  border: 0;
  border-radius: 6px;
  padding: 4px 7px;
  color: var(--sidebar-text);
  background: var(--sidebar-bg-hover);
  font-size: 12px;
}

.notion-skeleton-row {
  padding: 7px;
}

.notion-skeleton-row span {
  display: block;
  height: 8px;
  border-radius: 999px;
  background: linear-gradient(90deg, #191919, #242424, #191919);
  background-size: 220% 100%;
  animation: sidebar-skeleton 1.2s ease-in-out infinite;
}

.notion-skeleton-row span:first-child {
  width: 74%;
}

.notion-skeleton-row span:last-child {
  width: 58%;
  margin-top: 7px;
}

.notion-panel-action:focus-visible,
.notion-read-button:focus-visible,
.notion-empty-state button:focus-visible {
  outline: 2px solid #525252;
  outline-offset: 2px;
}

@keyframes sidebar-spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes sidebar-skeleton {
  0% {
    background-position: 120% 0;
  }

  100% {
    background-position: -120% 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .notion-panel-action i.spinning,
  .notion-skeleton-row span {
    animation: none;
  }
}
</style>
