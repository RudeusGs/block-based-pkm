<template>
  <header class="app-top-nav sticky-top">
    <nav class="app-breadcrumb" aria-label="Breadcrumb">
      <button
        class="app-breadcrumb-pill"
        type="button"
        title="Workspace"
      >
        Workspace
      </button>

      <span>/</span>

      <button
        class="app-breadcrumb-pill app-breadcrumb-current-workspace"
        type="button"
        :title="workspaceNavigation.workspaceName.value"
      >
        {{ workspaceNavigation.workspaceName.value }}
      </button>

      <span>/</span>

      <strong :title="workspaceNavigation.pageName.value">
        <span
          v-if="workspaceNavigation.page.value"
          class="app-breadcrumb-page-icon"
        >
          {{ workspaceNavigation.pageIcon.value }}
        </span>

        {{ workspaceNavigation.pageName.value }}
      </strong>
    </nav>

    <div class="app-top-actions">
      <button
        class="app-task-jump"
        type="button"
        title="Jump to Work Tasks"
        @click="emit('jump-to-tasks')"
      >
        <span class="material-symbols-outlined">table_chart</span>
        Tasks
      </button>

      <div
        ref="notificationMenuRef"
        class="app-notification-wrap"
      >
        <button
          class="app-icon-btn app-notification-btn"
          type="button"
          title="Notifications"
          aria-label="Notifications"
          :aria-expanded="isNotificationMenuOpen"
          @click.stop="toggleNotifications"
        >
          <span class="material-symbols-outlined">notifications</span>

          <span
            v-if="notificationCenter.unreadCount.value > 0"
            class="app-notification-badge"
          >
            {{ notificationCenter.unreadCountLabel.value }}
          </span>

          <span
            class="app-realtime-dot"
            :class="{ connected: notificationCenter.isRealtimeConnected.value }"
            :title="
              notificationCenter.isRealtimeConnected.value
                ? 'Realtime connected'
                : 'Realtime disconnected'
            "
          ></span>
        </button>

        <Transition name="app-notification-pop">
          <section
            v-if="isNotificationMenuOpen"
            class="app-notification-menu"
            role="dialog"
            aria-label="Notifications"
            @click.stop
          >
            <header class="app-notification-head">
              <div>
                <strong>Notifications</strong>

                <span>
                  {{
                    notificationCenter.unreadCount.value > 0
                      ? `${notificationCenter.unreadCount.value} unread`
                      : 'All caught up'
                  }}
                </span>
              </div>

              <div class="app-notification-head-actions">
                <button
                  type="button"
                  title="Refresh"
                  :disabled="notificationCenter.isLoading.value"
                  @click="refreshNotifications"
                >
                  <i
                    class="bi bi-arrow-clockwise"
                    :class="{ spinning: notificationCenter.isLoading.value }"
                  ></i>
                </button>

                <button
                  type="button"
                  title="Mark all as read"
                  :disabled="
                    notificationCenter.unreadCount.value === 0 ||
                    notificationCenter.isMarkingAllRead.value
                  "
                  @click="notificationCenter.markAllAsRead"
                >
                  <i class="bi bi-check2-all"></i>
                </button>
              </div>
            </header>

            <p
              v-if="notificationCenter.realtimeError.value"
              class="app-notification-warning"
            >
              {{ notificationCenter.realtimeError.value }}
            </p>

            <div
              v-if="notificationCenter.isLoading.value"
              class="app-notification-skeleton-list"
            >
              <div
                v-for="index in 4"
                :key="index"
                class="app-notification-skeleton"
              >
                <span></span>
                <span></span>
              </div>
            </div>

            <div
              v-else-if="notificationCenter.error.value"
              class="app-notification-state"
            >
              <strong>Không thể tải thông báo</strong>
              <p>{{ notificationCenter.error.value }}</p>

              <button
                type="button"
                @click="refreshNotifications"
              >
                Thử lại
              </button>
            </div>

            <div
              v-else-if="!notificationCenter.notifications.value.length"
              class="app-notification-state"
            >
              <strong>Chưa có thông báo</strong>
              <p>Các cập nhật mới sẽ xuất hiện ở đây.</p>
            </div>

            <div
              v-else
              class="app-notification-list"
            >
              <article
                v-for="notification in notificationCenter.notifications.value"
                :key="notification.id"
                class="app-notification-item"
                :class="{ unread: !notification.isRead }"
              >
                <button
                  type="button"
                  class="app-notification-content"
                  @click="notificationCenter.markAsRead(notification.id)"
                >
                  <span
                    v-if="!notification.isRead"
                    class="app-notification-unread-dot"
                  ></span>

                  <span class="app-notification-copy">
                    <span class="app-notification-meta">
                      {{ notificationTypeLabel(notification.type) }}
                      <span>·</span>
                      {{ formatNotificationTime(notification.createdDate) }}
                    </span>

                    <strong>{{ notification.title }}</strong>

                    <span class="app-notification-message">
                      {{ notification.message }}
                    </span>
                  </span>
                </button>
              </article>
            </div>
          </section>
        </Transition>
      </div>

      <button class="app-icon-btn" type="button" title="Share">
        <span class="material-symbols-outlined">share</span>
      </button>

      <button class="app-icon-btn" type="button" title="Star">
        <span class="material-symbols-outlined">star</span>
      </button>

      <button class="app-icon-btn" type="button" title="History">
        <span class="material-symbols-outlined">history</span>
      </button>

      <button class="app-icon-btn" type="button" title="More">
        <span class="material-symbols-outlined">more_vert</span>
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useWorkspaceNavigation } from '@/modules/navigation/composables/useWorkspaceNavigation'
import { useNotificationCenter } from '@/modules/notifications/composables/useNotificationCenter'

const emit = defineEmits<{
  'jump-to-tasks': []
}>()

const workspaceNavigation = useWorkspaceNavigation()
const notificationCenter = useNotificationCenter()

const isNotificationMenuOpen = ref(false)
const notificationMenuRef = ref<HTMLElement | null>(null)

function toggleNotifications() {
  isNotificationMenuOpen.value = !isNotificationMenuOpen.value

  if (isNotificationMenuOpen.value) {
    void refreshNotifications()
  }
}

async function refreshNotifications() {
  await Promise.all([
    notificationCenter.fetchNotifications(),
    notificationCenter.fetchUnreadCount(),
  ])
}

function notificationTypeLabel(type: string) {
  const normalized = type.trim().toLowerCase()

  if (!normalized) return 'Update'
  if (normalized.includes('task')) return 'Task'
  if (normalized.includes('comment')) return 'Comment'
  if (normalized.includes('recommend')) return 'AI'
  if (normalized.includes('workspace')) return 'Workspace'
  if (normalized.includes('page')) return 'Page'

  return type
}

function formatNotificationTime(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return ''

  const diffMs = Date.now() - date.getTime()
  const diffMinutes = Math.max(0, Math.floor(diffMs / 60000))

  if (diffMinutes < 1) return 'Just now'
  if (diffMinutes < 60) return `${diffMinutes}m ago`

  const diffHours = Math.floor(diffMinutes / 60)
  if (diffHours < 24) return `${diffHours}h ago`

  const diffDays = Math.floor(diffHours / 24)
  if (diffDays < 7) return `${diffDays}d ago`

  return date.toLocaleDateString()
}

function handleDocumentClick(event: MouseEvent) {
  if (!isNotificationMenuOpen.value) return

  const target = event.target as Node | null

  if (target && notificationMenuRef.value?.contains(target)) {
    return
  }

  isNotificationMenuOpen.value = false
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    isNotificationMenuOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
  window.addEventListener('keydown', handleKeydown)

  notificationCenter.bindRealtime()

  void notificationCenter.fetchUnreadCount()
  void notificationCenter.fetchNotifications()
  void notificationCenter.startRealtime()
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick)
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped>
.app-top-nav {
  z-index: 50;
  min-height: 48px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 0 14px;
  color: #f1f1f1;
  background: rgba(15, 15, 15, 0.92);
  border-bottom: 1px solid #2f2f2f;
  backdrop-filter: blur(18px);
}

.app-breadcrumb {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 4px;
  color: #8a8a8a;
  font-size: 13px;
  font-weight: 500;
}

.app-breadcrumb span {
  color: #555;
}

.app-breadcrumb strong {
  min-width: 0;
  overflow: hidden;
  color: #e7e7e7;
  font-size: 13px;
  font-weight: 650;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.app-breadcrumb-page-icon {
  margin-right: 5px;
  color: inherit;
  font-size: 13px;
}

.app-breadcrumb-pill {
  border: 0;
  border-radius: 6px;
  padding: 4px 7px;
  color: #a3a3a3;
  background: transparent;
  font: inherit;
}

.app-breadcrumb-pill:hover {
  color: #f1f1f1;
  background: #242424;
}

.app-breadcrumb-current-workspace {
  max-width: 180px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.app-top-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.app-task-jump,
.app-icon-btn {
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #a3a3a3;
  background: transparent;
  font: inherit;
}

.app-task-jump {
  min-height: 32px;
  gap: 6px;
  padding: 0 9px;
  font-size: 13px;
  font-weight: 600;
}

.app-task-jump:hover,
.app-icon-btn:hover {
  color: #f1f1f1;
  background: #242424;
}

.app-task-jump .material-symbols-outlined,
.app-icon-btn .material-symbols-outlined {
  font-size: 18px;
}

.app-icon-btn {
  position: relative;
  width: 32px;
  height: 32px;
}

.app-notification-wrap {
  position: relative;
}

.app-notification-btn {
  overflow: visible;
}

.app-notification-badge {
  position: absolute;
  top: 3px;
  right: 1px;
  min-width: 15px;
  height: 15px;
  padding: 0 4px;
  border: 2px solid #0f0f0f;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #111;
  background: #f1f1f1;
  font-size: 9px;
  font-weight: 800;
  line-height: 1;
}

.app-realtime-dot {
  position: absolute;
  right: 5px;
  bottom: 5px;
  width: 6px;
  height: 6px;
  border: 1px solid #0f0f0f;
  border-radius: 999px;
  background: #737373;
}

.app-realtime-dot.connected {
  background: #75b798;
}

.app-notification-menu {
  position: absolute;
  top: calc(100% + 10px);
  right: 0;
  width: min(380px, calc(100vw - 24px));
  overflow: hidden;
  border: 1px solid #2f2f2f;
  border-radius: 12px;
  background: #191919;
  box-shadow: 0 24px 80px rgba(0, 0, 0, 0.42);
}

.app-notification-head {
  min-height: 58px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 12px 10px;
  border-bottom: 1px solid #2b2b2b;
}

.app-notification-head div:first-child {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.app-notification-head strong {
  color: #f1f1f1;
  font-size: 14px;
  font-weight: 720;
}

.app-notification-head span {
  color: #8a8a8a;
  font-size: 12px;
}

.app-notification-head-actions {
  display: inline-flex;
  align-items: center;
  gap: 2px;
}

.app-notification-head-actions button {
  width: 28px;
  height: 28px;
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #8a8a8a;
  background: transparent;
}

.app-notification-head-actions button:hover:not(:disabled) {
  color: #f1f1f1;
  background: #242424;
}

.app-notification-head-actions button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.app-notification-head-actions i {
  font-size: 14px;
}

.app-notification-head-actions i.spinning {
  animation: app-top-spin 0.8s linear infinite;
}

.app-notification-warning {
  margin: 0;
  padding: 8px 12px;
  border-bottom: 1px solid #2b2b2b;
  color: #d6b15d;
  background: #1d1a12;
  font-size: 12px;
  line-height: 1.4;
}

.app-notification-list,
.app-notification-skeleton-list {
  max-height: min(520px, calc(100vh - 140px));
  overflow-y: auto;
  padding: 6px;
}

.app-notification-list::-webkit-scrollbar,
.app-notification-skeleton-list::-webkit-scrollbar {
  width: 10px;
}

.app-notification-list::-webkit-scrollbar-track,
.app-notification-skeleton-list::-webkit-scrollbar-track {
  background: transparent;
}

.app-notification-list::-webkit-scrollbar-thumb,
.app-notification-skeleton-list::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.app-notification-item {
  border-radius: 8px;
}

.app-notification-item:hover {
  background: #242424;
}

.app-notification-content {
  width: 100%;
  border: 0;
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 8px;
  color: inherit;
  background: transparent;
  text-align: left;
}

.app-notification-unread-dot {
  width: 7px;
  height: 7px;
  margin-top: 7px;
  border-radius: 999px;
  flex-shrink: 0;
  background: #f1f1f1;
}

.app-notification-item:not(.unread) .app-notification-content {
  padding-left: 23px;
}

.app-notification-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.app-notification-meta {
  display: flex;
  align-items: center;
  gap: 5px;
  color: #8a8a8a;
  font-size: 11px;
  line-height: 1.3;
}

.app-notification-copy strong {
  overflow: hidden;
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 650;
  line-height: 1.35;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.app-notification-message {
  display: -webkit-box;
  overflow: hidden;
  color: #8a8a8a;
  font-size: 12px;
  line-height: 1.4;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.app-notification-state {
  padding: 18px 14px 20px;
  color: #8a8a8a;
  font-size: 13px;
  line-height: 1.45;
}

.app-notification-state strong {
  display: block;
  color: #d4d4d4;
  font-size: 13px;
  font-weight: 650;
}

.app-notification-state p {
  margin: 4px 0 0;
}

.app-notification-state button {
  margin-top: 10px;
  border: 0;
  border-radius: 6px;
  padding: 5px 8px;
  color: #f1f1f1;
  background: #242424;
  font-size: 12px;
}

.app-notification-skeleton {
  padding: 8px;
}

.app-notification-skeleton span {
  display: block;
  height: 8px;
  border-radius: 999px;
  background: linear-gradient(90deg, #1d1d1d, #282828, #1d1d1d);
  background-size: 220% 100%;
  animation: app-top-skeleton 1.2s ease-in-out infinite;
}

.app-notification-skeleton span:first-child {
  width: 78%;
}

.app-notification-skeleton span:last-child {
  width: 58%;
  margin-top: 8px;
}

.app-notification-pop-enter-active,
.app-notification-pop-leave-active {
  transition:
    opacity 0.14s ease,
    transform 0.14s ease;
}

.app-notification-pop-enter-from,
.app-notification-pop-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.98);
}

.app-task-jump:focus-visible,
.app-icon-btn:focus-visible,
.app-breadcrumb-pill:focus-visible,
.app-notification-content:focus-visible,
.app-notification-head-actions button:focus-visible,
.app-notification-state button:focus-visible {
  outline: 2px solid #525252;
  outline-offset: 2px;
}

@keyframes app-top-spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes app-top-skeleton {
  0% {
    background-position: 120% 0;
  }

  100% {
    background-position: -120% 0;
  }
}

@media (max-width: 768px) {
  .app-top-nav {
    padding: 0 10px;
  }

  .app-breadcrumb-current-workspace {
    max-width: 120px;
  }

  .app-breadcrumb button:nth-of-type(1) {
    display: none;
  }

  .app-breadcrumb span:nth-of-type(1) {
    display: none;
  }

  .app-task-jump {
    width: 32px;
    padding: 0;
  }

  .app-task-jump:not(:hover) {
    font-size: 0;
  }

  .app-task-jump .material-symbols-outlined {
    font-size: 18px;
  }

  .app-top-actions > .app-icon-btn:not(.app-notification-btn):nth-last-child(n + 3) {
    display: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .app-notification-pop-enter-active,
  .app-notification-pop-leave-active,
  .app-notification-head-actions i.spinning,
  .app-notification-skeleton span {
    transition: none;
    animation: none;
  }
}
</style>