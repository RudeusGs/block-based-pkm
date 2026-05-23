<template>
  <aside
    class="sidebar-left lunar-sidebar"
    :class="{
      collapsed: shell.isCollapsed.value,
      'sidebar-clickable': shell.isCollapsed.value,
    }"
    @click="handleSidebarClick"
  >
    <SidebarRail
      v-if="shell.isCollapsed.value"
      :display-name="account.profileDisplayName.value"
      :avatar-url="account.profileAvatarUrl.value"
      :initial="account.profileInitial.value"
      :open-task-count="myTasks.openTaskCount.value"
      :recommendation-count="recommendations.pendingRecommendationCount.value"
      @expand="expandSidebar"
      @open-my-tasks="openCollapsedPanel('myTasks')"
      @open-recommendations="openCollapsedPanel('recommendations')"
      @open-settings="openSettingsModal"
    />

    <template v-else>
      <div class="lunar-shell">
        <SidebarAccountHeader
          :display-name="account.profileDisplayName.value"
          :subtitle="account.profileSubtitle.value"
          :avatar-url="account.profileAvatarUrl.value"
          :initial="account.profileInitial.value"
          :workspace-role="workspaceTree.selectedWorkspaceRole.value"
          :is-loading="account.isLoadingProfile.value"
          @collapse="shell.collapseSidebar"
          @open-settings="openSettingsModal"
          @open-my-tasks="openPanel('myTasks')"
          @logout="account.logout"
        />

        <nav class="lunar-nav" aria-label="Sidebar navigation">
          <div class="lunar-primary-nav">
            <button
              type="button"
              class="lunar-nav-row"
              :class="{ active: shell.activePanel.value === 'myTasks' }"
              @click.stop="openPanel('myTasks')"
            >
              <span class="lunar-nav-icon">
                <i class="bi bi-check2-square"></i>
              </span>

              <span class="lunar-nav-label">My Tasks</span>

              <span
                v-if="myTasks.openTaskCount.value > 0"
                class="lunar-count-badge"
              >
                {{ myTasks.openTaskCount.value }}
              </span>
            </button>

            <button
              type="button"
              class="lunar-nav-row"
              :class="{ active: shell.activePanel.value === 'recommendations' }"
              @click.stop="openPanel('recommendations')"
            >
              <span class="lunar-nav-icon">
                <i class="bi bi-stars"></i>
              </span>

              <span class="lunar-nav-label">AI Suggestions</span>

              <span
                v-if="recommendations.pendingRecommendationCount.value > 0"
                class="lunar-count-badge"
              >
                {{ recommendations.pendingRecommendationCount.value }}
              </span>
            </button>
          </div>

          <SidebarWorkspaceSection
            :is-open="workspaceTree.isWorkspacesOpen.value"
            :workspaces="workspaceTree.workspaces.value"
            :has-workspaces="workspaceTree.hasWorkspaces.value"
            :is-loading-workspaces="workspaceTree.isLoadingWorkspaces.value"
            :is-refreshing-tree="workspaceTree.isRefreshingSidebarTree.value"
            :workspace-list-error="workspaceTree.workspaceListError.value"
            :selected-workspace-id="workspaceTree.selectedWorkspaceId.value"
            :selected-page-id="workspaceTree.selectedPageId.value"
            :opened-page-ids="workspaceTree.openedPageIds.value"
            :is-workspace-branch-open="workspaceTree.isWorkspaceBranchOpen"
            :is-loading-pages="workspaceTree.isLoadingPages"
            :get-workspace-pages="workspaceTree.getWorkspacePages"
            :get-page-list-error="workspaceTree.getPageListError"
            @toggle="workspaceTree.toggleWorkspaces"
            @create-workspace="workspaceTree.openCreateWorkspaceModal"
            @refresh-tree="refreshSidebarTree"
            @retry-workspaces="workspaceTree.retryLoadWorkspaces"
            @toggle-workspace="workspaceTree.toggleWorkspaceBranch"
            @create-page="workspaceTree.openCreatePageModal"
            @retry-pages="workspaceTree.retryLoadPages"
            @select-page="workspaceTree.selectPage"
            @toggle-page="workspaceTree.togglePageBranch"
            @page-settings="openPageSettings"
            @share-page="sharePage"
            @delete-page="workspaceTree.requestDeletePage"
          />
        </nav>

        <footer class="lunar-footer">
          <button
            type="button"
            class="lunar-footer-row"
            :class="{ active: isSettingsModalOpen }"
            @click.stop="openSettingsModal"
          >
            <span class="lunar-footer-icon">
              <i class="bi bi-sliders2"></i>
            </span>

            <span class="lunar-footer-label">
              Settings
            </span>
          </button>
        </footer>
      </div>
    </template>

    <SidebarMyTasksPanel
      :open="shell.activePanel.value === 'myTasks'"
      :tasks="myTasks.myTasks.value"
      :total-count="myTasks.myTaskTotalCount.value"
      :is-loading="myTasks.isLoadingMyTasks.value"
      :error="myTasks.myTaskError.value"
      @close="shell.closePanel"
      @refresh="refreshMyTasks"
    />

    <SidebarRecommendationsDrawer
      :open="shell.activePanel.value === 'recommendations'"
      :workspace-id="workspaceTree.selectedWorkspaceId.value"
      workspace-name="Tất cả workspace"
      page-name="Tự động tổng hợp theo task"
      :recommendations="recommendations.taskRecommendations.value"
      :is-loading="recommendations.isLoadingTaskRecommendations.value"
      :is-generating="recommendations.isGeneratingTaskRecommendations.value"
      :error="recommendations.taskRecommendationError.value"
      :realtime-error="recommendations.realtimeError.value"
      @close="shell.closePanel"
      @generate="generateRecommendations"
      @retry="refreshRecommendations"
      @accept="acceptRecommendation"
      @reject="rejectRecommendation"
    />

    <SidebarSettingsPanel
      v-if="isSettingsModalOpen"
      :workspace-id="workspaceTree.selectedWorkspaceId.value"
      :profile-form="settings.profileForm"
      :password-form="settings.passwordForm"
      :task-preference-form="settings.taskPreferenceForm"
      :preferred-day-options="settings.preferredDayOptions"
      :is-loading-profile-settings="settings.isLoadingProfileSettings.value"
      :is-saving-profile-settings="settings.isSavingProfileSettings.value"
      :is-uploading-avatar-image="settings.isUploadingAvatarImage.value"
      :profile-settings-error="settings.profileSettingsError.value"
      :profile-settings-success="settings.profileSettingsSuccess.value"
      :is-changing-password="settings.isChangingPassword.value"
      :password-settings-error="settings.passwordSettingsError.value"
      :password-settings-success="settings.passwordSettingsSuccess.value"
      :is-loading-task-preference="settings.isLoadingTaskPreference.value"
      :is-saving-task-preference="settings.isSavingTaskPreference.value"
      :task-preference-error="settings.taskPreferenceError.value"
      :task-preference-success="settings.taskPreferenceSuccess.value"
      @close="closeSettingsModal"
      @save-profile="saveProfileSettings"
      @upload-avatar-image="uploadAvatarImage"
      @change-password="settings.changePassword"
      @toggle-preferred-day="settings.togglePreferredDay"
      @save-task-preference="saveTaskPreference"
    />

    <CreateWorkspaceModal
      v-model="workspaceTree.isCreateWorkspaceModalOpen.value"
      @created="workspaceTree.handleWorkspaceCreated"
    />

    <CreatePageModal
      v-model="workspaceTree.isCreatePageModalOpen.value"
      :workspace-id="workspaceTree.createPageWorkspaceId.value"
      :workspace-name="workspaceTree.createPageWorkspaceName.value"
      :parent-page-id="workspaceTree.createPageParentPageId.value"
      @created="workspaceTree.handlePageCreated"
    />

    <ConfirmActionModal
      :open="workspaceTree.isDeletePageConfirmOpen.value"
      title="Xóa page này?"
      :message="workspaceTree.deletePageConfirmMessage.value"
      description="Page bị xóa sẽ biến mất khỏi sidebar. Nếu backend đang soft-delete thì vẫn có thể restore bằng API riêng sau này."
      confirm-label="Xóa page"
      submitting-label="Đang xóa..."
      :is-submitting="workspaceTree.isDeletingPage.value"
      :error="workspaceTree.deletePageError.value"
      @close="workspaceTree.closeDeletePageConfirm"
      @confirm="confirmDeletePage"
    />
  </aside>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import CreateWorkspaceModal from '@/components/workspace/CreateWorkspaceModal.vue'
import CreatePageModal from '@/components/page/CreatePageModal.vue'
import ConfirmActionModal from '@/components/shared/ConfirmActionModal.vue'
import { useWorkspaceNavigation } from '@/modules/navigation/composables/useWorkspaceNavigation'
import { useToast } from '@/components/composables/useToast'
import type { Guid } from '@/api/models/common.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'
import type { PageTreeItem } from './types/sidebar.types'

import SidebarRail from './features/rail/SidebarRail.vue'
import SidebarAccountHeader from './features/account/SidebarAccountHeader.vue'
import SidebarRecommendationsDrawer from './features/recommendations/SidebarRecommendationsDrawer.vue'
import SidebarMyTasksPanel from './features/my-tasks/SidebarMyTasksPanel.vue'
import SidebarSettingsPanel from './features/settings/SidebarSettingsPanel.vue'
import SidebarWorkspaceSection from './features/workspaces/SidebarWorkspaceSection.vue'

import { useSidebarShell, type SidebarPanel } from './composables/useSidebarShell'
import { useSidebarAccount } from './features/account/useSidebarAccount'
import { useSidebarWorkspaceTree } from './features/workspaces/useSidebarWorkspaceTree'
import { useSidebarRecommendations } from './features/recommendations/useSidebarRecommendations'
import { useSidebarMyTasks } from './features/my-tasks/useSidebarMyTasks'
import { useSidebarSettings } from './features/settings/useSidebarSettings'

import './css/SidebarLeft.css'

type NonNullSidebarPanel = Exclude<SidebarPanel, null>

const shell = useSidebarShell()
const account = useSidebarAccount()
const workspaceTree = useSidebarWorkspaceTree()
const workspaceNavigation = useWorkspaceNavigation()
const recommendations = useSidebarRecommendations()
const myTasks = useSidebarMyTasks()
const settings = useSidebarSettings()
const toast = useToast()

const isSettingsModalOpen = ref(false)

watch(
  workspaceTree.selectedWorkspaceId,
  (workspaceId) => {
    void recommendations.fetchPendingRecommendations()

    if (isSettingsModalOpen.value) {
      void settings.fetchTaskPreference(workspaceId)
    }
  }
)

function handleSidebarClick() {
  if (!shell.isCollapsed.value) return

  shell.expandSidebar()
}

function expandSidebar() {
  shell.expandSidebar()
}

function loadPanelData(panel: SidebarPanel) {
  if (panel === 'myTasks') {
    void myTasks.fetchMyTasks()
  }

  if (panel === 'recommendations') {
    void recommendations.fetchPendingRecommendations()
  }
}

function openPanel(panel: SidebarPanel) {
  if (panel === 'settings') {
    openSettingsModal()
    return
  }

  shell.openPanel(panel)
  loadPanelData(shell.activePanel.value)
}

function openCollapsedPanel(panel: NonNullSidebarPanel) {
  if (panel === 'settings') {
    openSettingsModal()
    return
  }

  shell.expandSidebar()

  if (shell.activePanel.value !== panel) {
    shell.openPanel(panel)
  }

  loadPanelData(panel)
}

function openSettingsModal() {
  if (shell.isCollapsed.value) {
    shell.expandSidebar()
  }

  shell.activePanel.value = null
  isSettingsModalOpen.value = true

  void settings.fetchProfileSettings()
  void settings.fetchTaskPreference(workspaceTree.selectedWorkspaceId.value)
}

function closeSettingsModal() {
  isSettingsModalOpen.value = false
}

function refreshRecommendations() {
  void recommendations.fetchPendingRecommendations()
}

async function generateRecommendations() {
  const generated = await recommendations.generateRecommendations(
    workspaceTree.selectedWorkspaceId.value,
    workspaceTree.selectedPageId.value
  )

  if (generated) {
    void recommendations.fetchPendingRecommendations()
  }
}

async function acceptRecommendation(recommendationId: Guid) {
  const accepted = await recommendations.acceptRecommendation(recommendationId)

  if (accepted) {
    void myTasks.fetchMyTasks()
    void recommendations.fetchPendingRecommendations()
  }
}

async function rejectRecommendation(recommendationId: Guid) {
  const rejected = await recommendations.rejectRecommendation(recommendationId)

  if (rejected) {
    void recommendations.fetchPendingRecommendations()
  }
}

function refreshMyTasks() {
  void myTasks.fetchMyTasks()
}

async function refreshSidebarTree() {
  await workspaceTree.refreshSidebarTree()

  if (!workspaceTree.workspaceListError.value) {
    toast.success(
      'Đã đồng bộ sidebar',
      'Danh sách workspace và page đã được tải lại.'
    )
  }
}

function notifyProfileUpdated() {
  window.dispatchEvent(new CustomEvent('pkm:profile-updated'))
}

async function saveProfileSettings() {
  const saved = await settings.saveProfileSettings()

  if (saved) {
    await account.fetchMyProfile()
    notifyProfileUpdated()
  }
}

async function uploadAvatarImage(file: File) {
  const uploaded = await settings.uploadAvatarImage(file)

  if (uploaded) {
    await account.fetchMyProfile()
    notifyProfileUpdated()
  }
}

async function saveTaskPreference() {
  const saved = await settings.saveTaskPreference(
    workspaceTree.selectedWorkspaceId.value
  )

  if (saved) {
    void recommendations.fetchPendingRecommendations()
  }
}

function openPageSettings(page: PageTreeItem) {
  workspaceTree.selectPage(page)

  toast.info(
    'Cài đặt page',
    `Mình đã chọn page "${page.title}". Màn chỉnh metadata có thể nối tiếp vào action này.`
  )
}

async function sharePage(page: PageTreeItem) {
  workspaceTree.selectPage(page)

  const url = `${window.location.origin}/app?pageId=${encodeURIComponent(
    page.id
  )}`

  try {
    await navigator.clipboard.writeText(url)

    toast.success('Đã copy link page', `Link "${page.title}" đã nằm trong clipboard.`)
  } catch {
    toast.info('Link page', url, 7000)
  }
}

async function confirmDeletePage() {
  const deletedPage = await workspaceTree.confirmDeletePage()

  if (!deletedPage) {
    return
  }

  workspaceNavigation.closePageTab(deletedPage.id)

  toast.success('Đã xóa page', `Page "${deletedPage.title}" đã được xóa khỏi workspace.`)
}

function handleWorkspaceUpdated(workspace: WorkspaceResponse) {
  workspaceTree.handleWorkspaceUpdated(workspace)
}

function handleWorkspaceDeleted(workspaceId: Guid) {
  workspaceTree.handleWorkspaceDeleted(workspaceId)
}

defineExpose({
  handleWorkspaceDeleted,
  handleWorkspaceUpdated,
})

onMounted(() => {
  void account.fetchMyProfile()
  void workspaceTree.fetchMyWorkspaces()
  void myTasks.fetchMyTasks()
  void recommendations.fetchPendingRecommendations()
  void recommendations.startRecommendationRealtime()
})

onBeforeUnmount(() => {
  recommendations.stopRecommendationRealtime()
})
</script>

<style scoped>
.sidebar-left {
  width: 280px;
  min-width: 280px;
  background: #141414;
}

.sidebar-left.collapsed {
  width: 54px;
  min-width: 54px;
}

:deep(.lunar-shell) {
  padding: 8px;
}

:deep(.lunar-header) {
  min-height: 36px;
  margin-bottom: 6px;
}

:deep(.lunar-account-card),
:deep(.lunar-nav-row),
:deep(.lunar-footer-row),
:deep(.lunar-workspace-row),
:deep(.lunar-section-toggle),
:deep(.lunar-page-row) {
  border-color: transparent;
  border-radius: 6px;
  background: transparent;
  box-shadow: none;
}

:deep(.lunar-account-card:hover),
:deep(.lunar-nav-row:hover),
:deep(.lunar-nav-row.active),
:deep(.lunar-footer-row:hover),
:deep(.lunar-footer-row.active),
:deep(.lunar-workspace-row:hover),
:deep(.lunar-workspace-row.active),
:deep(.lunar-section-toggle:hover),
:deep(.lunar-page-row:hover),
:deep(.lunar-page-row.active) {
  border-color: transparent;
  background: #242424;
}

:deep(.lunar-account-avatar),
:deep(.lunar-account-menu-avatar),
:deep(.lunar-workspace-orb),
:deep(.lunar-rail-avatar) {
  border: 0;
  border-radius: 6px;
  background: #2a2a2a;
}

:deep(.lunar-workspace-orb) {
  width: 18px;
  height: 18px;
  font-size: 10px;
}

:deep(.lunar-icon-btn),
:deep(.lunar-section-action),
:deep(.lunar-row-action),
:deep(.lunar-tree-toggle),
:deep(.lunar-page-toggle),
:deep(.lunar-page-action),
:deep(.lunar-rail-btn) {
  border-color: transparent;
  border-radius: 6px;
  background: transparent;
}

:deep(.lunar-icon-btn:hover),
:deep(.lunar-section-action:hover),
:deep(.lunar-row-action:hover),
:deep(.lunar-tree-toggle:hover),
:deep(.lunar-page-toggle:hover),
:deep(.lunar-page-action:hover),
:deep(.lunar-rail-btn:hover) {
  border-color: transparent;
  background: #242424;
}

:deep(.lunar-count-badge),
:deep(.lunar-role-chip),
:deep(.lunar-account-badge) {
  border: 0;
  background: transparent;
  color: #8a8a8a;
}

:deep(.lunar-pages-branch) {
  margin-left: 10px;
  padding-left: 8px;
}

:deep(.lunar-footer) {
  padding-top: 7px;
  border-top: 1px solid #2b2b2b;
}
</style>


