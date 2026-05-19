<template>
  <section class="sidebar-insight-panel updates-panel">
    <header class="insight-panel-header">
      <div class="insight-panel-title-wrap">
        <span class="insight-panel-icon">
          <i class="bi bi-clock-history"></i>
        </span>

        <div class="insight-panel-title-copy">
          <strong>Updates</strong>
          <span>
            {{ unreadCount > 0 ? `${unreadCount} unread activity` : 'All caught up' }}
          </span>
        </div>
      </div>

      <div class="insight-panel-actions">
        <button
          type="button"
          class="insight-icon-button"
          title="Đánh dấu tất cả đã đọc"
          :disabled="!unreadCount || isLoading"
          @click.stop="emit('markAll')"
        >
          <i class="bi bi-check2-all"></i>
        </button>

        <button
          type="button"
          class="insight-icon-button"
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
      v-if="notifications.length && !isLoading && !error"
      class="insight-summary-row"
    >
      <span class="insight-summary-chip strong">
        {{ notifications.length }} latest
      </span>

      <span
        v-if="unreadCount > 0"
        class="insight-summary-chip unread"
      >
        {{ unreadCount }} unread
      </span>

      <span
        v-else
        class="insight-summary-chip"
      >
        Inbox zero
      </span>
    </div>

    <div
      v-if="isLoading"
      class="insight-skeleton-list"
    >
      <div
        v-for="index in 3"
        :key="index"
        class="update-skeleton-card"
      >
        <span class="skeleton-icon"></span>

        <div class="skeleton-lines">
          <span></span>
          <span></span>
          <span></span>
        </div>
      </div>
    </div>

    <div
      v-else-if="error"
      class="insight-state-card error"
    >
      <span class="insight-state-icon">
        <i class="bi bi-exclamation-triangle"></i>
      </span>

      <div class="insight-state-copy">
        <strong>Không thể tải Updates</strong>
        <p>{{ error }}</p>
      </div>

      <button
        type="button"
        class="insight-state-action"
        @click.stop="emit('refresh')"
      >
        Thử lại
      </button>
    </div>

    <div
      v-else-if="!notifications.length"
      class="insight-state-card empty"
    >
      <span class="insight-state-icon">
        <i class="bi bi-stars"></i>
      </span>

      <div class="insight-state-copy">
        <strong>Chưa có cập nhật mới</strong>
        <p>Mọi hoạt động quan trọng trong workspace sẽ hiện ở đây.</p>
      </div>
    </div>

    <div
      v-else
      class="update-card-list"
    >
      <article
        v-for="notification in notifications"
        :key="notification.id"
        class="update-card"
        :class="{ unread: !notification.isRead }"
      >
        <div class="update-card-rail">
          <span
            class="update-card-icon"
            :class="notificationTone(notification.type)"
          >
            <i :class="['bi', notificationIcon(notification.type)]"></i>
          </span>

          <span
            v-if="!notification.isRead"
            class="update-unread-pin"
            title="Unread"
          ></span>
        </div>

        <div class="update-card-body">
          <div class="update-card-topline">
            <span class="update-type-pill">
              {{ notificationTypeLabel(notification.type) }}
            </span>

            <time class="update-time">
              {{ formatDateTime(notification.createdDate) }}
            </time>
          </div>

          <h4 class="update-card-title">
            {{ notification.title }}
          </h4>

          <p class="update-card-message">
            {{ notification.message }}
          </p>

          <div class="update-card-footer">
            <span
              class="update-read-state"
              :class="{ unread: !notification.isRead }"
            >
              {{ notification.isRead ? 'Read' : 'Unread' }}
            </span>

            <button
              v-if="!notification.isRead"
              type="button"
              class="update-mark-button"
              title="Đánh dấu đã đọc"
              @click.stop="emit('markRead', notification.id)"
            >
              <i class="bi bi-check2"></i>
              <span>Done</span>
            </button>
          </div>
        </div>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Guid } from '@/api/models/common.model'
import type { NotificationResponse } from '@/api/models/notification.model'
import {
  formatDateTime,
  notificationIcon,
} from '../../utils/sidebar-format.util'

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

function notificationTone(type: string) {
  const normalized = normalize(type)

  if (normalized.includes('task')) return 'task'
  if (normalized.includes('comment')) return 'comment'
  if (normalized.includes('recommend')) return 'recommendation'
  if (normalized.includes('workspace')) return 'workspace'
  if (normalized.includes('page')) return 'page'

  return 'default'
}

function notificationTypeLabel(type: string) {
  const normalized = normalize(type)

  if (!normalized) return 'Update'
  if (normalized.includes('task')) return 'Task'
  if (normalized.includes('comment')) return 'Comment'
  if (normalized.includes('recommend')) return 'AI'
  if (normalized.includes('workspace')) return 'Workspace'
  if (normalized.includes('page')) return 'Page'

  return type
}
</script>

<style scoped>
.sidebar-insight-panel {
  margin: -2px 0 12px;
  padding: 10px;
  border: 1px solid var(--sidebar-border);
  border-radius: 16px;
  background:
    radial-gradient(circle at top left, rgba(255, 255, 255, 0.045), transparent 42%),
    rgba(23, 23, 23, 0.94);
}

.insight-panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 10px;
}

.insight-panel-title-wrap {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 9px;
}

.insight-panel-icon {
  width: 32px;
  height: 32px;
  border: 1px solid var(--sidebar-border);
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--sidebar-text);
  background: linear-gradient(135deg, #252525, #171717);
}

.insight-panel-icon i {
  font-size: 14px;
}

.insight-panel-title-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.insight-panel-title-copy strong {
  color: var(--sidebar-text);
  font-size: 13.5px;
  font-weight: 850;
  letter-spacing: -0.01em;
}

.insight-panel-title-copy span {
  overflow: hidden;
  color: var(--sidebar-faint);
  font-size: 11px;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.insight-panel-actions {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  flex-shrink: 0;
}

.insight-icon-button {
  width: 28px;
  height: 28px;
  border: 1px solid transparent;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--sidebar-faint);
  background: transparent;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease,
    opacity 140ms ease;
}

.insight-icon-button:hover:not(:disabled) {
  color: var(--sidebar-text);
  border-color: var(--sidebar-border);
  background: var(--sidebar-bg-hover);
}

.insight-icon-button:disabled {
  opacity: 0.42;
  cursor: not-allowed;
}

.insight-icon-button i {
  font-size: 13px;
}

.insight-icon-button i.spinning {
  animation: sidebar-spin 0.8s linear infinite;
}

.insight-summary-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 9px;
}

.insight-summary-chip {
  min-width: 0;
  height: 24px;
  border: 1px solid var(--sidebar-border);
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  padding: 0 8px;
  color: var(--sidebar-faint);
  background: var(--sidebar-bg);
  font-size: 10.5px;
  font-weight: 800;
}

.insight-summary-chip.strong {
  color: var(--sidebar-muted);
}

.insight-summary-chip.unread {
  color: #fef3c7;
  border-color: rgba(251, 191, 36, 0.24);
  background: rgba(120, 53, 15, 0.18);
}

.update-card-list,
.insight-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.update-card {
  position: relative;
  display: flex;
  gap: 9px;
  border: 1px solid transparent;
  border-radius: 14px;
  padding: 9px;
  background: var(--sidebar-bg);
  transition:
    background-color 140ms ease,
    border-color 140ms ease,
    transform 140ms ease;
}

.update-card:hover {
  border-color: var(--sidebar-border);
  background: var(--sidebar-bg-hover);
  transform: translateY(-1px);
}

.update-card.unread {
  border-color: rgba(255, 255, 255, 0.11);
  background:
    linear-gradient(135deg, rgba(255, 255, 255, 0.045), transparent),
    #181818;
}

.update-card-rail {
  position: relative;
  flex-shrink: 0;
}

.update-card-icon {
  width: 31px;
  height: 31px;
  border: 1px solid var(--sidebar-border);
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--sidebar-muted);
  background: var(--sidebar-bg-soft);
}

.update-card-icon i {
  font-size: 13px;
}

.update-card-icon.task {
  color: #a7f3d0;
  background: rgba(20, 83, 45, 0.2);
}

.update-card-icon.comment {
  color: #bfdbfe;
  background: rgba(30, 64, 175, 0.2);
}

.update-card-icon.recommendation {
  color: #ddd6fe;
  background: rgba(91, 33, 182, 0.22);
}

.update-card-icon.workspace {
  color: #fde68a;
  background: rgba(120, 53, 15, 0.2);
}

.update-card-icon.page {
  color: #fecdd3;
  background: rgba(159, 18, 57, 0.18);
}

.update-unread-pin {
  position: absolute;
  top: -2px;
  right: -2px;
  width: 9px;
  height: 9px;
  border: 2px solid var(--sidebar-bg);
  border-radius: 999px;
  background: #f5f5f5;
}

.update-card-body {
  min-width: 0;
  flex: 1;
}

.update-card-topline {
  min-width: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 4px;
}

.update-type-pill {
  max-width: 92px;
  overflow: hidden;
  border: 1px solid var(--sidebar-border);
  border-radius: 999px;
  padding: 2px 6px;
  color: var(--sidebar-faint);
  background: var(--sidebar-bg-soft);
  font-size: 9.5px;
  font-weight: 900;
  text-transform: uppercase;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.update-time {
  overflow: hidden;
  color: var(--sidebar-faint);
  font-size: 10px;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.update-card-title {
  overflow: hidden;
  margin: 0;
  color: var(--sidebar-text);
  font-size: 12.7px;
  font-weight: 850;
  line-height: 1.32;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.update-card-message {
  display: -webkit-box;
  overflow: hidden;
  margin: 4px 0 7px;
  color: var(--sidebar-faint);
  font-size: 11.2px;
  line-height: 1.38;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.update-card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.update-read-state {
  color: var(--sidebar-faint);
  font-size: 10.5px;
  font-weight: 800;
}

.update-read-state.unread {
  color: var(--sidebar-muted);
}

.update-mark-button {
  min-height: 24px;
  border: 1px solid var(--sidebar-border);
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 3px 7px;
  color: var(--sidebar-muted);
  background: var(--sidebar-bg-soft);
  font-size: 10.5px;
  font-weight: 850;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease;
}

.update-mark-button:hover {
  color: var(--sidebar-text);
  border-color: var(--sidebar-border-strong);
  background: var(--sidebar-bg-hover);
}

.insight-state-card {
  border: 1px solid var(--sidebar-border);
  border-radius: 14px;
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 11px;
  background: var(--sidebar-bg);
}

.insight-state-card.error {
  border-color: rgba(248, 113, 113, 0.24);
  background: rgba(127, 29, 29, 0.14);
}

.insight-state-icon {
  width: 30px;
  height: 30px;
  border: 1px solid var(--sidebar-border);
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--sidebar-muted);
  background: var(--sidebar-bg-soft);
}

.insight-state-copy {
  min-width: 0;
  flex: 1;
}

.insight-state-copy strong {
  display: block;
  color: var(--sidebar-text);
  font-size: 12.5px;
  font-weight: 850;
}

.insight-state-copy p {
  margin: 3px 0 0;
  color: var(--sidebar-faint);
  font-size: 11.2px;
  line-height: 1.4;
}

.insight-state-action {
  border: 1px solid var(--sidebar-border);
  border-radius: 8px;
  padding: 5px 8px;
  color: var(--sidebar-text);
  background: var(--sidebar-bg-soft);
  font-size: 11px;
  font-weight: 850;
}

.update-skeleton-card {
  display: flex;
  gap: 9px;
  border-radius: 14px;
  padding: 9px;
  background: var(--sidebar-bg);
}

.skeleton-icon,
.skeleton-lines span {
  display: block;
  border-radius: 999px;
  background: linear-gradient(90deg, #202020, #2a2a2a, #202020);
  background-size: 220% 100%;
  animation: sidebar-skeleton 1.2s ease-in-out infinite;
}

.skeleton-icon {
  width: 31px;
  height: 31px;
  border-radius: 10px;
  flex-shrink: 0;
}

.skeleton-lines {
  min-width: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding-top: 2px;
}

.skeleton-lines span {
  height: 8px;
}

.skeleton-lines span:nth-child(1) {
  width: 72%;
}

.skeleton-lines span:nth-child(2) {
  width: 100%;
}

.skeleton-lines span:nth-child(3) {
  width: 46%;
}

.insight-icon-button:focus-visible,
.update-mark-button:focus-visible,
.insight-state-action:focus-visible {
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
  .update-card,
  .insight-icon-button,
  .update-mark-button,
  .skeleton-icon,
  .skeleton-lines span,
  .insight-icon-button i.spinning {
    transition: none;
    animation: none;
  }
}
</style>