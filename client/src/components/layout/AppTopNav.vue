<template>
  <header class="app-top-nav sticky-top">
    <nav
      class="app-breadcrumb"
      aria-label="Breadcrumb"
    >
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

      <button
        class="app-icon-btn"
        type="button"
        title="Friends & profiles"
        aria-label="Friends & profiles"
        @click="openSocialHub"
      >
        <span class="material-symbols-outlined">group</span>
      </button>

      <button
        class="app-icon-btn"
        type="button"
        title="Messages"
        aria-label="Messages"
        @click="openMessenger()"
      >
        <span class="material-symbols-outlined">chat_bubble</span>
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

        <Transition name="app-pop">
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
                  <span class="material-symbols-outlined">refresh</span>
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
                  <span class="material-symbols-outlined">done_all</span>
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

      <button
        class="app-icon-btn"
        type="button"
        title="Share"
      >
        <span class="material-symbols-outlined">share</span>
      </button>

      <button
        class="app-icon-btn"
        type="button"
        title="Star"
      >
        <span class="material-symbols-outlined">star</span>
      </button>

      <button
        class="app-icon-btn"
        type="button"
        title="Activity log"
        aria-label="Activity log"
        @click="openActivityLog"
      >
        <span class="material-symbols-outlined">history</span>
      </button>

      <div
        ref="moreMenuRef"
        class="app-more-wrap"
      >
        <button
          class="app-icon-btn"
          type="button"
          title="More"
          aria-label="More actions"
          :aria-expanded="isMoreMenuOpen"
          @click.stop="toggleMoreMenu"
        >
          <span class="material-symbols-outlined">more_vert</span>
        </button>

        <Transition name="app-pop">
          <section
            v-if="isMoreMenuOpen"
            class="app-more-menu"
            role="menu"
            aria-label="More actions"
            @click.stop
          >
            <div class="app-more-menu-head">
              <strong>{{ workspaceNavigation.workspaceName.value }}</strong>
              <span>Workspace actions</span>
            </div>

            <button
              type="button"
              class="app-more-item primary"
              role="menuitem"
              @click="openInviteModal"
            >
              <span class="material-symbols-outlined">person_add</span>

              <span>
                <strong>Mời thành viên</strong>
                <small>Gửi lời mời qua Gmail</small>
              </span>
            </button>

            <div class="app-more-separator"></div>

            <button
              type="button"
              class="app-more-item"
              role="menuitem"
              @click="openWorkspaceSettings"
            >
              <span class="material-symbols-outlined">settings</span>

              <span>
                <strong>Cài đặt workspace</strong>
                <small>Quyền, tên và mô tả</small>
              </span>
            </button>

            <button
              type="button"
              class="app-more-item"
              role="menuitem"
              @click="openWorkspaceMembers"
            >
              <span class="material-symbols-outlined">group</span>

              <span>
                <strong>Thành viên</strong>
                <small>Xem danh sách người trong workspace</small>
              </span>
            </button>

            <div class="app-more-separator"></div>

            <button
              type="button"
              class="app-more-item danger"
              role="menuitem"
              :disabled="!currentWorkspaceId"
              @click="openDeleteWorkspaceConfirm"
            >
              <span class="material-symbols-outlined">delete</span>

              <span>
                <strong>Xóa workspace này</strong>
                <small>Xóa workspace và toàn bộ dữ liệu bên trong</small>
              </span>
            </button>
          </section>
        </Transition>
      </div>
    </div>

    <InviteWorkspaceMemberModal
      :open="isInviteModalOpen"
      :workspace-name="workspaceNavigation.workspaceName.value"
      :email="inviteMember.email.value"
      :role="inviteMember.role.value"
      :error="inviteMember.inviteError.value"
      :can-submit="inviteMember.canSubmit.value"
      :is-submitting="inviteMember.isInviting.value"
      @close="closeInviteModal"
      @submit="submitInvite"
      @update:email="inviteMember.email.value = $event"
      @update:role="inviteMember.role.value = $event"
    />

    <WorkspaceSettingsModal
      :open="isWorkspaceSettingsOpen"
      :workspace-id="currentWorkspaceId"
      @close="isWorkspaceSettingsOpen = false"
      @updated="handleWorkspaceUpdated"
    />

    <ConfirmActionModal
      :open="deleteWorkspace.isDeleteWorkspaceConfirmOpen.value"
      title="Xóa workspace này?"
      :message="deleteWorkspaceConfirmMessage"
      description="Toàn bộ page, task và member liên quan đến workspace này sẽ bị xóa theo backend. Hành động này không thể hoàn tác."
      confirm-label="Xóa workspace"
      submitting-label="Đang xóa..."
      :is-submitting="deleteWorkspace.isDeletingWorkspace.value"
      :error="deleteWorkspace.deleteWorkspaceError.value"
      @close="deleteWorkspace.closeDeleteWorkspaceConfirm"
      @confirm="confirmDeleteWorkspace"
    />
  </header>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useToast } from '@/components/composables/useToast'
import { useWorkspaceNavigation } from '@/modules/navigation/composables/useWorkspaceNavigation'
import { useNotificationCenter } from '@/modules/notifications/composables/useNotificationCenter'
import { useInviteWorkspaceMember } from '@/modules/workspaces/composables/useInviteWorkspaceMember'
import { useDeleteWorkspace } from '@/modules/workspaces/composables/useDeleteWorkspace'
import type { Guid } from '@/api/models/common.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'
import InviteWorkspaceMemberModal from './InviteWorkspaceMemberModal.vue'
import ConfirmActionModal from '@/components/shared/ConfirmActionModal.vue'
import WorkspaceSettingsModal from '@/components/workspace/WorkspaceSettingsModal.vue'

const emit = defineEmits<{
  'jump-to-tasks': []
  'open-members': []
  'workspace-deleted': [workspaceId: Guid]
  'workspace-updated': [workspace: WorkspaceResponse]
  'open-activity-log': []
  'open-social': []
  'open-messenger': [userId?: Guid | null]
}>()

const toast = useToast()
const workspaceNavigation = useWorkspaceNavigation()
const notificationCenter = useNotificationCenter()
const inviteMember = useInviteWorkspaceMember()
const deleteWorkspace = useDeleteWorkspace()

const isNotificationMenuOpen = ref(false)
const isMoreMenuOpen = ref(false)
const isInviteModalOpen = ref(false)
const isWorkspaceSettingsOpen = ref(false)

const notificationMenuRef = ref<HTMLElement | null>(null)
const moreMenuRef = ref<HTMLElement | null>(null)

const currentWorkspaceId = computed<Guid | null>(() => {
  return workspaceNavigation.workspace.value?.id ?? null
})

const deleteWorkspaceConfirmMessage = computed(() => {
  const workspace = deleteWorkspace.workspaceToDelete.value

  if (!workspace) {
    return 'Bạn có chắc muốn xóa workspace này không?'
  }

  return `Bạn sắp xóa workspace "${workspace.name}". Toàn bộ page bên trong workspace này cũng sẽ bị ảnh hưởng.`
})

function toggleNotifications() {
  isNotificationMenuOpen.value = !isNotificationMenuOpen.value
  isMoreMenuOpen.value = false

  if (isNotificationMenuOpen.value) {
    void refreshNotifications()
  }
}

function toggleMoreMenu() {
  isMoreMenuOpen.value = !isMoreMenuOpen.value
  isNotificationMenuOpen.value = false
}

function openInviteModal() {
  isMoreMenuOpen.value = false
  inviteMember.resetInviteForm()
  isInviteModalOpen.value = true
}

function closeInviteModal() {
  isInviteModalOpen.value = false
}

function openWorkspaceMembers() {
  isMoreMenuOpen.value = false
  emit('open-members')
}

function openSocialHub() {
  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false
  emit('open-social')
}

function openMessenger(userId?: Guid | null) {
  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false
  emit('open-messenger', userId ?? null)
}

function openWorkspaceSettings() {
  isMoreMenuOpen.value = false
  isNotificationMenuOpen.value = false

  if (!currentWorkspaceId.value) {
    toast.warning('Chưa chọn workspace', 'Hãy chọn workspace trước khi mở settings.')
    return
  }

  isWorkspaceSettingsOpen.value = true
}

function handleWorkspaceUpdated(workspace: WorkspaceResponse) {
  isWorkspaceSettingsOpen.value = false

  workspaceNavigation.setWorkspace({
    id: workspace.id,
    name: workspace.name,
  })

  toast.success('Đã cập nhật workspace', `Workspace "${workspace.name}" đã được lưu.`)
  emit('workspace-updated', workspace)
}

function openActivityLog() {
  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false
  emit('open-activity-log')
}

function openDeleteWorkspaceConfirm() {
  const workspace = workspaceNavigation.workspace.value

  if (!workspace) {
    toast.warning('Chưa chọn workspace', 'Hãy chọn workspace trước khi xóa.')
    return
  }

  isMoreMenuOpen.value = false
  deleteWorkspace.requestDeleteWorkspace(workspace)
}

async function confirmDeleteWorkspace() {
  const deletedWorkspace = await deleteWorkspace.confirmDeleteWorkspace()

  if (!deletedWorkspace) {
    return
  }

  toast.success(
    'Đã xóa workspace',
    `Workspace "${deletedWorkspace.name}" đã được xóa.`
  )

  if (currentWorkspaceId.value === deletedWorkspace.id) {
    workspaceNavigation.clearNavigation()
  }

  emit('workspace-deleted', deletedWorkspace.id)
}

async function submitInvite() {
  const result = await inviteMember.inviteMember(currentWorkspaceId.value)

  if (!result) {
    return
  }

  toast.success(
    'Đã gửi lời mời',
    `Lời mời tham gia workspace đã được gửi đến ${result.email}.`
  )

  isInviteModalOpen.value = false
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
  const target = event.target as Node | null

  if (
    target &&
    notificationMenuRef.value?.contains(target)
  ) {
    return
  }

  if (
    target &&
    moreMenuRef.value?.contains(target)
  ) {
    return
  }

  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    isNotificationMenuOpen.value = false
    isMoreMenuOpen.value = false
    isInviteModalOpen.value = false
    isWorkspaceSettingsOpen.value = false
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
  z-index: 920;
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

.app-notification-wrap,
.app-more-wrap {
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

.app-notification-menu,
.app-more-menu {
  position: absolute;
  z-index: 930;
  top: calc(100% + 10px);
  right: 0;
  overflow: hidden;
  border: 1px solid #2f2f2f;
  border-radius: 10px;
  background: #191919;
  box-shadow: 0 24px 80px rgba(0, 0, 0, 0.42);
}

.app-notification-menu {
  width: min(380px, calc(100vw - 24px));
}

.app-more-menu {
  width: min(292px, calc(100vw - 24px));
  padding: 6px;
}

.app-more-menu-head {
  padding: 9px 9px 8px;
}

.app-more-menu-head strong {
  display: block;
  overflow: hidden;
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 650;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.app-more-menu-head span {
  display: block;
  margin-top: 2px;
  color: #858585;
  font-size: 11.5px;
}

.app-more-item {
  width: 100%;
  border: 0;
  border-radius: 7px;
  display: flex;
  align-items: flex-start;
  gap: 9px;
  padding: 9px;
  color: #cfcfcf;
  background: transparent;
  text-align: left;
  cursor: pointer;
}

.app-more-item:hover:not(:disabled) {
  color: #f1f1f1;
  background: #242424;
}

.app-more-item.primary {
  background: #202020;
}

.app-more-item.primary:hover {
  background: #282828;
}

.app-more-item.danger {
  color: #f1a6a6;
}

.app-more-item.danger:hover:not(:disabled) {
  color: #ffc8c8;
  background: rgba(255, 94, 94, 0.1);
}

.app-more-item:disabled {
  opacity: 0.48;
  cursor: not-allowed;
}

.app-more-item > .material-symbols-outlined {
  width: 22px;
  margin-top: 1px;
  color: #9b9b9b;
  font-size: 18px;
}

.app-more-item span:last-child {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.app-more-item strong {
  color: inherit;
  font-size: 13px;
  font-weight: 620;
  line-height: 1.25;
}

.app-more-item small {
  color: #858585;
  font-size: 11.5px;
  line-height: 1.35;
}

.app-more-separator {
  height: 1px;
  margin: 6px 4px;
  background: #2b2b2b;
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

.app-notification-head-actions .material-symbols-outlined {
  font-size: 17px;
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

.app-pop-enter-active,
.app-pop-leave-active {
  transition:
    opacity 140ms ease,
    transform 140ms ease;
}

.app-pop-enter-from,
.app-pop-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.98);
}

.app-task-jump:focus-visible,
.app-icon-btn:focus-visible,
.app-breadcrumb-pill:focus-visible,
.app-notification-content:focus-visible,
.app-notification-head-actions button:focus-visible,
.app-notification-state button:focus-visible,
.app-more-item:focus-visible {
  outline: 2px solid #525252;
  outline-offset: 2px;
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

  .app-breadcrumb button:nth-of-type(1),
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
  .app-pop-enter-active,
  .app-pop-leave-active,
  .app-notification-skeleton span {
    transition: none;
    animation: none;
  }
}
</style>
