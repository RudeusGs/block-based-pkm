<template>
  <div class="workspace-shell min-vh-100 d-flex text-on-surface">
    <SidebarLeft ref="sidebarLeftRef" />

    <section class="workspace-page-center flex-grow-1 min-vh-100 text-on-surface">
      <AppTopNav
        :can-share-workspace="workspaceMembers.canManageMembers.value"
        @jump-to-tasks="scrollToTasks"
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
            :workspace-name="workspaceNavigation.workspaceName.value"
            :page-title="workspaceNavigation.pageName.value"
          />

          <section class="related-pages-section mt-5 pt-4 border-top border-soft">
            <h3 class="related-title d-flex align-items-center gap-2 mb-3 text-on-surface-variant">
              <span class="material-symbols-outlined">account_tree</span>
              Related Pages
            </h3>

            <div class="row g-3">
              <div
                v-for="subpage in subpages"
                :key="subpage.id"
                class="col-12 col-md-6"
              >
                <a
                  class="related-card d-flex align-items-center gap-3 p-3 rounded-3 border text-decoration-none"
                  href="#"
                >
                  <span class="related-icon d-inline-flex align-items-center justify-content-center rounded-3">
                    {{ subpage.icon }}
                  </span>

                  <span class="flex-grow-1 min-w-0">
                    <span class="related-card-title d-block fw-semibold">
                      {{ subpage.title }}
                    </span>

                    <span class="small text-outline d-block mt-1">
                      Updated {{ subpage.updatedDate }}
                    </span>
                  </span>
                </a>
              </div>
            </div>
          </section>
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
      :is-mutating-member="workspaceMembers.isMutatingMember.value"
      :mutating-member-id="workspaceMembers.mutatingMemberId.value"
      :member-action-error="workspaceMembers.memberActionError.value"
      @close="workspaceMembers.close"
      @refresh="workspaceMembers.refresh"
      @change-role="workspaceMembers.changeMemberRole"
      @remove-member="workspaceMembers.removeMember"
    />

    <WorkspaceActivityLogPanel
      :open="isActivityLogOpen"
      :workspace-id="currentWorkspaceId"
      :workspace-name="workspaceNavigation.workspaceName.value"
      @close="closeActivityLog"
    />

    <SocialHubPanel
      :open="isSocialHubOpen"
      @close="closeSocialHub"
      @open-chat="openMessenger"
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
import { computed, ref } from 'vue'
import SidebarLeft from '@/components/sidebar-left/SidebarLeft.vue'
import AppTopNav from '@/components/layout/AppTopNav.vue'
import WorkspaceMembersSidebar from '@/components/layout/WorkspaceMembersSidebar.vue'
import PageEditor from '@/components/editor/PageEditor.vue'
import WorkTasksSection from '@/components/task/WorkTasksSection.vue'
import WorkspaceActivityLogPanel from '@/components/activity/WorkspaceActivityLogPanel.vue'
import SocialHubPanel from '@/components/social/SocialHubPanel.vue'
import MessengerPanel from '@/components/messaging/MessengerPanel.vue'
import { useWorkspaceNavigation } from '@/modules/navigation/composables/useWorkspaceNavigation'
import { useWorkspaceMembersSidebar } from '@/modules/workspaces/composables/useWorkspaceMembersSidebar'
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

function openActivityLog() {
  isActivityLogOpen.value = true
}

function closeActivityLog() {
  isActivityLogOpen.value = false
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

function handleSharedWorkspaceOpened(workspace: WorkspaceResponse) {
  workspaceNavigation.setWorkspace({
    id: workspace.id,
    name: workspace.name,
  })

  workspaceNavigation.setPage(null)
  void sidebarLeftRef.value?.handleWorkspaceJoined?.(workspace)
  closeMessenger()
}

function scrollToTasks() {
  taskSectionRef.value?.scrollIntoView?.({
    behavior: 'smooth',
    block: 'start',
  })
}
</script>

<style scoped>
@import "@/views/css/AppLayout.css";
</style>



