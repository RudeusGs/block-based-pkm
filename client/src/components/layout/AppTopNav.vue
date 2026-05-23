<template>
  <header class="app-top-nav sticky-top">
    <nav
      class="app-page-tabs"
      aria-label="Open pages"
    >
      <div
        v-if="workspaceNavigation.pageTabs.value.length"
        class="app-page-tab-strip"
        role="tablist"
      >
        <div
          v-for="tab in workspaceNavigation.pageTabs.value"
          :key="tab.id"
          class="app-page-tab"
          :class="{ active: tab.id === activePageId }"
          :title="`${tab.workspaceName} / ${tab.title}`"
          role="presentation"
        >
          <button
            class="app-page-tab-main"
            type="button"
            role="tab"
            :aria-selected="tab.id === activePageId"
            @click="selectPageTab(tab.id)"
          >
            <span class="app-page-tab-icon">
              {{ tab.icon || '📄' }}
            </span>

            <span class="app-page-tab-title">
              {{ tab.title }}
            </span>
          </button>

          <button
            class="app-page-tab-close"
            type="button"
            title="Close tab"
            aria-label="Close page tab"
            @click.stop="closePageTab(tab.id)"
          >
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
      </div>

      <div
        v-else
        class="app-page-tabs-empty"
        :title="workspaceNavigation.workspaceName.value"
      >
        <span class="material-symbols-outlined">tab</span>
        <span>{{ workspaceNavigation.workspaceName.value }}</span>
      </div>
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
        class="app-ai-reminder-btn"
        type="button"
        :title="aiReminderTitle"
        aria-label="AI reminders"
        @click="openAiReminders"
      >
        <span class="material-symbols-outlined">auto_awesome</span>
        Reminders

        <span
          v-if="aiReminderCount > 0"
          class="app-ai-reminder-badge"
        >
          {{ aiReminderLabel || aiReminderCount }}
        </span>
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
        v-if="canOpenWorkspaceShare"
        class="app-icon-btn"
        type="button"
        :title="shareButtonTitle"
        aria-label="Share workspace"
        @click="openWorkspaceShare"
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
        v-if="canReadActivityLog"
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
              v-if="canInviteMembers"
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

            <div
              v-if="canInviteMembers && (canManageWorkspaceSettings || canDeleteWorkspace)"
              class="app-more-separator"
            ></div>

            <button
              v-if="canManageWorkspaceSettings"
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

            <div
              v-if="canDeleteWorkspace"
              class="app-more-separator"
            ></div>

            <button
              v-if="canDeleteWorkspace"
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
      v-if="canInviteMembers"
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
      v-if="canManageWorkspaceSettings"
      :open="isWorkspaceSettingsOpen"
      :workspace-id="currentWorkspaceId"
      @close="isWorkspaceSettingsOpen = false"
      @updated="handleWorkspaceUpdated"
    />

    <WorkspaceShareModal
      :open="isWorkspaceShareOpen"
      :workspace-id="currentWorkspaceId"
      :workspace-name="workspaceNavigation.workspaceName.value"
      :can-share="canShareWorkspace"
      @close="isWorkspaceShareOpen = false"
    />

    <ConfirmActionModal
      v-if="canDeleteWorkspace"
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
import WorkspaceShareModal from '@/components/workspace/WorkspaceShareModal.vue'

const props = withDefaults(
  defineProps<{
    canShareWorkspace?: boolean
    canInviteMembers?: boolean
    canManageWorkspaceSettings?: boolean
    canDeleteWorkspace?: boolean
    canReadActivityLog?: boolean
    aiReminderCount?: number
    aiReminderLabel?: string
  }>(),
  {
    canShareWorkspace: false,
    canInviteMembers: false,
    canManageWorkspaceSettings: false,
    canDeleteWorkspace: false,
    canReadActivityLog: false,
    aiReminderCount: 0,
    aiReminderLabel: '',
  }
)

const emit = defineEmits<{
  'jump-to-tasks': []
  'open-ai-reminders': []
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
const isWorkspaceShareOpen = ref(false)

const notificationMenuRef = ref<HTMLElement | null>(null)
const moreMenuRef = ref<HTMLElement | null>(null)

const currentWorkspaceId = computed<Guid | null>(() => {
  return workspaceNavigation.workspace.value?.id ?? null
})

const activePageId = computed<Guid | null>(() => {
  return workspaceNavigation.page.value?.id ?? null
})

const canShareWorkspace = computed(() => {
  return Boolean(currentWorkspaceId.value && props.canShareWorkspace)
})

const canOpenWorkspaceShare = computed(() => {
  return Boolean(currentWorkspaceId.value && props.canShareWorkspace)
})

const canInviteMembers = computed(() => {
  return Boolean(currentWorkspaceId.value && props.canInviteMembers)
})

const canManageWorkspaceSettings = computed(() => {
  return Boolean(currentWorkspaceId.value && props.canManageWorkspaceSettings)
})

const canDeleteWorkspace = computed(() => {
  return Boolean(currentWorkspaceId.value && props.canDeleteWorkspace)
})

const canReadActivityLog = computed(() => {
  return Boolean(currentWorkspaceId.value && props.canReadActivityLog)
})

const shareButtonTitle = computed(() => {
  if (!currentWorkspaceId.value) return 'Chọn workspace trước khi share'
  if (!props.canShareWorkspace) return 'Chỉ Owner hoặc Manager mới được share workspace'
  return 'Share workspace qua Messenger'
})

const aiReminderTitle = computed(() => {
  if (props.aiReminderCount > 0) {
    return `${props.aiReminderCount} AI reminder cần xem`
  }

  return 'AI Reminders: quá hạn, sắp tới hạn, priority cao'
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

  if (!canInviteMembers.value) return

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

function openAiReminders() {
  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false
  emit('open-ai-reminders')
}

function openWorkspaceShare() {
  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false

  if (!currentWorkspaceId.value) {
    toast.warning('Chưa chọn workspace', 'Hãy chọn workspace trước khi share.')
    return
  }

  if (!props.canShareWorkspace) {
    toast.warning('Không có quyền share', 'Chỉ Owner hoặc Manager mới được share workspace.')
    return
  }

  isWorkspaceShareOpen.value = true
}

function selectPageTab(pageId: Guid) {
  if (activePageId.value === pageId) return

  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false
  workspaceNavigation.selectPageTab(pageId)
}

function closePageTab(pageId: Guid) {
  isNotificationMenuOpen.value = false
  isMoreMenuOpen.value = false
  workspaceNavigation.closePageTab(pageId)
}

function openWorkspaceSettings() {
  isMoreMenuOpen.value = false
  isNotificationMenuOpen.value = false

  if (!currentWorkspaceId.value || !canManageWorkspaceSettings.value) return

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

  if (!canReadActivityLog.value) return

  emit('open-activity-log')
}

function openDeleteWorkspaceConfirm() {
  const workspace = workspaceNavigation.workspace.value

  if (!workspace || !canDeleteWorkspace.value) {
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
    isWorkspaceShareOpen.value = false
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

.app-page-tabs {
  min-width: 0;
  flex: 1 1 auto;
  align-self: stretch;
  display: flex;
  align-items: flex-end;
}

.app-page-tab-strip {
  min-width: 0;
  width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
  display: flex;
  align-items: flex-end;
  gap: 2px;
  padding-top: 7px;
  scrollbar-width: thin;
  scrollbar-color: #3a3a3a transparent;
}

.app-page-tab-strip::-webkit-scrollbar {
  height: 8px;
}

.app-page-tab-strip::-webkit-scrollbar-track {
  background: transparent;
}

.app-page-tab-strip::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.app-page-tab {
  min-width: 132px;
  max-width: 224px;
  height: 34px;
  border: 1px solid #2d2d2d;
  border-bottom: 0;
  border-radius: 8px 8px 0 0;
  display: flex;
  align-items: center;
  flex: 0 1 192px;
  color: #a3a3a3;
  background: #171717;
}

.app-page-tab:hover {
  color: #f1f1f1;
  background: #202020;
}

.app-page-tab.active {
  color: #f1f1f1;
  background: #262626;
  border-color: #3a3a3a;
}

.app-page-tab-main {
  min-width: 0;
  height: 100%;
  border: 0;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  flex: 1 1 auto;
  padding: 0 7px 0 10px;
  color: inherit;
  background: transparent;
  font: inherit;
  text-align: left;
}

.app-page-tab-icon {
  width: 18px;
  flex: 0 0 18px;
  overflow: hidden;
  font-size: 14px;
  line-height: 1;
  text-align: center;
}

.app-page-tab-title {
  min-width: 0;
  overflow: hidden;
  font-size: 12.5px;
  font-weight: 620;
  line-height: 1.2;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.app-page-tab-close {
  width: 24px;
  height: 24px;
  margin-right: 4px;
  border: 0;
  border-radius: 5px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 24px;
  color: #858585;
  background: transparent;
}

.app-page-tab-close:hover {
  color: #f1f1f1;
  background: #333;
}

.app-page-tab-close .material-symbols-outlined {
  font-size: 16px;
}

.app-page-tabs-empty {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 7px;
  align-self: center;
  color: #8a8a8a;
  font-size: 13px;
  font-weight: 500;
}

.app-page-tabs-empty span:last-child {
  min-width: 0;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.app-page-tabs-empty .material-symbols-outlined {
  font-size: 17px;
}

.app-top-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.app-task-jump,
.app-ai-reminder-btn,
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

.app-task-jump,
.app-ai-reminder-btn {
  min-height: 32px;
  gap: 6px;
  padding: 0 9px;
  font-size: 13px;
  font-weight: 600;
}

.app-ai-reminder-btn {
  position: relative;
  padding-right: 11px;
}

.app-task-jump:hover,
.app-ai-reminder-btn:hover,
.app-icon-btn:hover {
  color: #f1f1f1;
  background: #242424;
}

.app-task-jump:disabled,
.app-ai-reminder-btn:disabled,
.app-icon-btn:disabled {
  opacity: 0.38;
  cursor: not-allowed;
}

.app-task-jump:disabled:hover,
.app-ai-reminder-btn:disabled:hover,
.app-icon-btn:disabled:hover {
  color: #a3a3a3;
  background: transparent;
}

.app-task-jump .material-symbols-outlined,
.app-ai-reminder-btn .material-symbols-outlined,
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

.app-ai-reminder-badge {
  min-width: 17px;
  height: 17px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  padding: 0 5px;
  color: #191919;
  background: #d8d8d4;
  font-size: 10px;
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
.app-ai-reminder-btn:focus-visible,
.app-icon-btn:focus-visible,
.app-page-tab-main:focus-visible,
.app-page-tab-close:focus-visible,
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

  .app-page-tab {
    min-width: 112px;
    flex-basis: 156px;
  }

  .app-task-jump,
  .app-ai-reminder-btn {
    width: 32px;
    padding: 0;
  }

  .app-task-jump:not(:hover),
  .app-ai-reminder-btn:not(:hover) {
    font-size: 0;
  }

  .app-task-jump .material-symbols-outlined,
  .app-ai-reminder-btn .material-symbols-outlined {
    font-size: 18px;
  }

  .app-ai-reminder-badge {
    position: absolute;
    top: 2px;
    right: -2px;
    min-width: 15px;
    height: 15px;
    padding: 0 4px;
    font-size: 9px;
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

/* Final Notion top-nav polish */
.app-top-nav {
  min-height: 46px;
  gap: 12px;
  padding: 0 12px;
  background: rgba(15, 15, 15, 0.96);
  border-bottom-color: #242424;
}

.app-page-tabs {
  align-items: center;
}

.app-page-tab-strip {
  align-items: center;
  gap: 3px;
  padding-top: 0;
}

.app-page-tab {
  min-width: 126px;
  max-width: 210px;
  height: 30px;
  border: 0;
  border-radius: 6px;
  flex-basis: 178px;
  color: #a8a8a3;
  background: transparent;
  transition:
    background-color 140ms ease,
    color 140ms ease,
    opacity 140ms ease;
}

.app-page-tab:hover {
  color: #ededeb;
  background: rgba(255, 255, 255, 0.055);
}

.app-page-tab.active {
  color: #ededeb;
  border-color: transparent;
  background: #343330;
  box-shadow: inset 0 -1px 0 rgba(255, 255, 255, 0.09);
}

.app-page-tab-main {
  gap: 6px;
  padding: 0 6px 0 8px;
}

.app-page-tab-icon {
  width: 17px;
  flex-basis: 17px;
  font-size: 13px;
  opacity: 0.9;
}

.app-page-tab-title {
  font-size: 12.5px;
  font-weight: 500;
}

.app-page-tab-close {
  width: 22px;
  height: 22px;
  margin-right: 3px;
  border-radius: 4px;
  color: #85837d;
  opacity: 0;
  transition:
    opacity 140ms ease,
    color 140ms ease,
    background-color 140ms ease;
}

.app-page-tab:hover .app-page-tab-close,
.app-page-tab.active .app-page-tab-close {
  opacity: 1;
}

.app-page-tab-close:hover {
  color: #ededeb;
  background: rgba(255, 255, 255, 0.075);
}

.app-task-jump,
.app-ai-reminder-btn,
.app-icon-btn {
  border-radius: 5px;
  color: #a8a8a3;
}

.app-task-jump:hover,
.app-ai-reminder-btn:hover,
.app-icon-btn:hover {
  color: #ededeb;
  background: rgba(255, 255, 255, 0.065);
}

.app-notification-badge,
.app-ai-reminder-badge {
  color: #191919;
  background: #d8d8d4;
}

.app-realtime-dot.connected {
  background: #9a9a94;
}

.app-notification-menu,
.app-more-menu {
  border-color: #30302d;
  border-radius: 8px;
  background: #202020;
  box-shadow:
    0 18px 48px rgba(0, 0, 0, 0.38),
    0 0 0 1px rgba(255, 255, 255, 0.025);
}

.app-more-menu {
  width: min(286px, calc(100vw - 24px));
}

.app-more-item {
  border-radius: 5px;
  padding: 8px;
  color: #d4d4d0;
}

.app-more-item:hover:not(:disabled) {
  color: #f1f1ef;
  background: rgba(255, 255, 255, 0.065);
}

.app-more-item.primary {
  background: transparent;
}

.app-more-item.primary:hover {
  background: rgba(255, 255, 255, 0.065);
}

.app-more-item.danger {
  color: #d9a1a1;
}

.app-more-item.danger:hover:not(:disabled) {
  color: #f0b8b8;
  background: rgba(255, 255, 255, 0.06);
}

.app-more-item > .material-symbols-outlined {
  color: #8f8d86;
}

.app-more-separator {
  margin: 5px 4px;
  background: #30302d;
}

</style>



