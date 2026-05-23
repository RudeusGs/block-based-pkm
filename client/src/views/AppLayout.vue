<template>
  <div class="workspace-shell min-vh-100 d-flex text-on-surface">
    <SidebarLeft ref="sidebarLeftRef" />

    <section class="workspace-page-center flex-grow-1 min-vh-100 text-on-surface">
      <AppTopNav
        :can-share-workspace="workspaceMembers.canManageMembers.value"
        :can-invite-members="workspaceMembers.canManageMembers.value"
        :can-manage-workspace-settings="workspaceMembers.canUpdateWorkspace.value"
        :can-delete-workspace="workspaceMembers.canDeleteWorkspace.value"
        :can-read-activity-log="workspaceMembers.canReadActivityLog.value"
        :ai-reminder-count="taskAiReminders.badgeCount.value"
        :ai-reminder-label="taskAiReminders.badgeLabel.value"
        @jump-to-tasks="scrollToTasks"
        @open-ai-reminders="openAiReminders"
        @open-members="workspaceMembers.open"
        @open-activity-log="openActivityLog"
        @open-social="openSocialHub"
        @open-messenger="openMessenger"
        @workspace-deleted="handleWorkspaceDeleted"
        @workspace-updated="handleWorkspaceUpdated"
      />

      <main class="page-scroll pb-5">
        <div class="page-container container position-relative px-4 pt-4 pb-5">
          <PageEditor
            :page-id="currentPageId"
            :can-edit="workspaceMembers.canEditDocument.value"
            :page-title="workspaceNavigation.pageName.value"
            :page-icon="workspaceNavigation.pageIcon.value"
            :page-cover-image="workspaceNavigation.pageCoverImage.value"
            :page-revision="workspaceNavigation.pageRevision.value"
            :workspace-name="workspaceNavigation.workspaceName.value"
            @cover-uploaded="handlePageCoverUploaded"
          />

          <WorkTasksSection
            ref="taskSectionRef"
            :workspace-id="currentWorkspaceId"
            :page-id="currentPageId"
            :can-manage-tasks="workspaceMembers.canManageTasks.value"
            :can-manage-assignees="workspaceMembers.canManageTasks.value"
            :can-comment-tasks="workspaceMembers.canCommentTasks.value"
            :workspace-name="workspaceNavigation.workspaceName.value"
            :page-title="workspaceNavigation.pageName.value"
          />
        </div>
      </main>
    </section>

    <WorkspaceMembersSidebar
      :open="workspaceMembers.isOpen.value"
      :workspace-name="workspaceNavigation.workspaceName.value"
      :members="workspaceMembers.members.value"
      :online-members="workspaceMembers.onlineMembers.value"
      :offline-members="workspaceMembers.offlineMembers.value"
      :member-count-label="workspaceMembers.memberCountLabel.value"
      :is-loading="workspaceMembers.isLoading.value"
      :error="workspaceMembers.error.value"
      :can-manage-members="workspaceMembers.canManageMembers.value"
      :can-transfer-ownership="workspaceMembers.canTransferOwnership.value"
      :is-mutating-member="workspaceMembers.isMutatingMember.value"
      :mutating-member-id="workspaceMembers.mutatingMemberId.value"
      :member-action-error="workspaceMembers.memberActionError.value"
      @close="workspaceMembers.close"
      @refresh="workspaceMembers.refresh"
      @change-role="workspaceMembers.changeMemberRole"
      @remove-member="workspaceMembers.removeMember"
      @transfer-ownership="transferWorkspaceOwnership"
    />
    
    <WorkspaceActivityLogPanel
      :open="isActivityLogOpen"
      :workspace-id="currentWorkspaceId"
      :workspace-name="workspaceNavigation.workspaceName.value"
      :can-read-audit="workspaceMembers.canReadActivityLog.value"
      @close="closeActivityLog"
    />

    <TaskAiReminderPanel
      :open="isAiReminderPanelOpen"
      :reminders="taskAiReminders.reminders.value"
      :summary="taskAiReminders.summary.value"
      :is-loading="taskAiReminders.isLoading.value"
      :error="taskAiReminders.error.value"
      :generated-at="taskAiReminders.generatedAt.value"
      @close="closeAiReminders"
      @refresh="taskAiReminders.refresh"
      @jump-to-tasks="scrollToTasks"
    />

    <SocialHubPanel
      :open="isSocialHubOpen"
      @close="closeSocialHub"
      @open-chat="openMessenger"
      @workspace-opened="handleSocialWorkspaceOpened"
    />

    <MessengerPanel
      :open="isMessengerOpen"
      :start-user-id="messengerStartUserId"
      @close="closeMessenger"
      @started="messengerStartUserId = null"
      @workspace-opened="handleSharedWorkspaceOpened"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import SidebarLeft from '@/components/sidebar-left/SidebarLeft.vue'
import AppTopNav from '@/components/layout/AppTopNav.vue'
import WorkspaceMembersSidebar from '@/components/layout/WorkspaceMembersSidebar.vue'
import PageEditor from '@/components/editor/PageEditor.vue'
import WorkTasksSection from '@/components/task/WorkTasksSection.vue'
import TaskAiReminderPanel from '@/components/task/TaskAiReminderPanel.vue'
import WorkspaceActivityLogPanel from '@/components/activity/WorkspaceActivityLogPanel.vue'
import SocialHubPanel from '@/components/social/SocialHubPanel.vue'
import MessengerPanel from '@/components/messaging/MessengerPanel.vue'
import { useWorkspaceNavigation } from '@/modules/navigation/composables/useWorkspaceNavigation'
import {
  useWorkspaceMembersSidebar,
  type WorkspaceMemberListItem,
} from '@/modules/workspaces/composables/useWorkspaceMembersSidebar'
import { useTaskAiReminders } from '@/modules/task/composables/useTaskAiReminders'
import type { Guid } from '@/api/models/common.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'

type SidebarLeftExpose = {
  handleWorkspaceDeleted?: (workspaceId: Guid) => void
  handleWorkspaceUpdated?: (workspace: WorkspaceResponse) => void
  handleWorkspaceJoined?: (workspace: WorkspaceResponse) => void | Promise<void>
}

type WorkTasksSectionExpose = {
  scrollIntoView?: (options?: ScrollIntoViewOptions) => void
}

const taskSectionRef = ref<WorkTasksSectionExpose | null>(null)
const sidebarLeftRef = ref<SidebarLeftExpose | null>(null)
const isActivityLogOpen = ref(false)
const isAiReminderPanelOpen = ref(false)
const isSocialHubOpen = ref(false)
const isMessengerOpen = ref(false)
const messengerStartUserId = ref<Guid | null>(null)

const workspaceNavigation = useWorkspaceNavigation()

const currentWorkspaceId = computed(() => {
  return workspaceNavigation.workspace.value?.id ?? null
})

const currentPageId = computed(() => {
  return workspaceNavigation.page.value?.id ?? null
})

const workspaceMembers = useWorkspaceMembersSidebar(currentWorkspaceId)
const taskAiReminders = useTaskAiReminders(currentWorkspaceId)

const subpages = [
  {
    id: 'sub-1',
    icon: '📊',
    title: 'Market Research',
    updatedDate: 'yesterday',
  },
  {
    id: 'sub-2',
    icon: '👥',
    title: 'Resource Allocation',
    updatedDate: '3 days ago',
  },
]

function handlePageCoverUploaded(payload: {
  pageId: Guid
  coverImage: string
  currentRevision: number
}) {
  workspaceNavigation.setPageCoverImage(
    payload.pageId,
    payload.coverImage,
    payload.currentRevision
  )
}

function handleWorkspaceDeleted(workspaceId: Guid) {
  sidebarLeftRef.value?.handleWorkspaceDeleted?.(workspaceId)
  workspaceMembers.close()
  closeActivityLog()
  closeAiReminders()
}

function handleWorkspaceUpdated(workspace: WorkspaceResponse) {
  sidebarLeftRef.value?.handleWorkspaceUpdated?.(workspace)

  if (currentWorkspaceId.value === workspace.id) {
    workspaceNavigation.setWorkspace({
      id: workspace.id,
      name: workspace.name,
    })
  }
}

async function transferWorkspaceOwnership(member: WorkspaceMemberListItem) {
  const workspace = await workspaceMembers.transferOwnership(member)

  if (!workspace) return

  handleWorkspaceUpdated(workspace)
}

function openActivityLog() {
  if (!workspaceMembers.canReadActivityLog.value) {
    isActivityLogOpen.value = false
    return
  }

  isActivityLogOpen.value = true
}

function closeActivityLog() {
  isActivityLogOpen.value = false
}

function openAiReminders() {
  isAiReminderPanelOpen.value = true
}

function closeAiReminders() {
  isAiReminderPanelOpen.value = false
}

function openSocialHub() {
  isSocialHubOpen.value = true
}

function closeSocialHub() {
  isSocialHubOpen.value = false
}

function openMessenger(userId?: Guid | null) {
  messengerStartUserId.value = userId ?? null
  isMessengerOpen.value = true
}

function closeMessenger() {
  isMessengerOpen.value = false
  messengerStartUserId.value = null
}

function openWorkspaceFromExternalSource(workspace: WorkspaceResponse) {
  workspaceNavigation.setWorkspace({
    id: workspace.id,
    name: workspace.name,
  })

  workspaceNavigation.setPage(null)
  void sidebarLeftRef.value?.handleWorkspaceJoined?.(workspace)
}

function handleSharedWorkspaceOpened(workspace: WorkspaceResponse) {
  openWorkspaceFromExternalSource(workspace)
  closeMessenger()
}

function handleSocialWorkspaceOpened(workspace: WorkspaceResponse) {
  openWorkspaceFromExternalSource(workspace)
  closeSocialHub()
}

function scrollToTasks() {
  taskSectionRef.value?.scrollIntoView?.({
    behavior: 'smooth',
    block: 'start',
  })
}


watch(
  workspaceMembers.canReadActivityLog,
  (canRead) => {
    if (!canRead) {
      closeActivityLog()
    }
  }
)

onMounted(() => {
  taskAiReminders.startAutoRefresh()
})

onBeforeUnmount(() => {
  taskAiReminders.stopAutoRefresh()
})
</script>

<style scoped>
@import "@/views/css/AppLayout.css";
</style>
