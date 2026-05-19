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
      @expand="expandSidebar"
      @open-my-tasks="openCollapsedPanel('myTasks')"
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
          </div>

          <Transition name="lunar-expand">
            <SidebarMyTasksPanel
              v-if="shell.activePanel.value === 'myTasks'"
              :tasks="myTasks.myTasks.value"
              :total-count="myTasks.myTaskTotalCount.value"
              :is-loading="myTasks.isLoadingMyTasks.value"
              :error="myTasks.myTaskError.value"
              @refresh="refreshMyTasks"
            />
          </Transition>

          <SidebarWorkspaceSection
            :is-open="workspaceTree.isWorkspacesOpen.value"
            :workspaces="workspaceTree.workspaces.value"
            :has-workspaces="workspaceTree.hasWorkspaces.value"
            :is-loading-workspaces="workspaceTree.isLoadingWorkspaces.value"
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
            @retry-workspaces="workspaceTree.retryLoadWorkspaces"
            @toggle-workspace="workspaceTree.toggleWorkspaceBranch"
            @create-page="workspaceTree.openCreatePageModal"
            @retry-pages="workspaceTree.retryLoadPages"
            @select-page="workspaceTree.selectPage"
            @toggle-page="workspaceTree.togglePageBranch"
          />

          <SidebarRecommendationsCard
            class="lunar-recommendations-section"
            :workspace-id="workspaceTree.selectedWorkspaceId.value"
            :recommendations="recommendations.taskRecommendations.value"
            :is-loading="recommendations.isLoadingTaskRecommendations.value"
            :is-generating="recommendations.isGeneratingTaskRecommendations.value"
            :error="recommendations.taskRecommendationError.value"
            @generate="generateRecommendations"
            @retry="refreshRecommendations"
            @accept="acceptRecommendation"
            @reject="recommendations.rejectRecommendation"
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

    <SidebarSettingsPanel
      v-if="isSettingsModalOpen"
      :workspace-id="workspaceTree.selectedWorkspaceId.value"
      :profile-form="settings.profileForm"
      :password-form="settings.passwordForm"
      :task-preference-form="settings.taskPreferenceForm"
      :preferred-day-options="settings.preferredDayOptions"
      :is-loading-profile-settings="settings.isLoadingProfileSettings.value"
      :is-saving-profile-settings="settings.isSavingProfileSettings.value"
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
  </aside>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import CreateWorkspaceModal from '@/components/workspace/CreateWorkspaceModal.vue'
import CreatePageModal from '@/components/page/CreatePageModal.vue'
import type { Guid } from '@/api/models/common.model'

import SidebarRail from './features/rail/SidebarRail.vue'
import SidebarAccountHeader from './features/account/SidebarAccountHeader.vue'
import SidebarRecommendationsCard from './features/recommendations/SidebarRecommendationsCard.vue'
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
const recommendations = useSidebarRecommendations()
const myTasks = useSidebarMyTasks()
const settings = useSidebarSettings()

const isSettingsModalOpen = ref(false)

watch(
  workspaceTree.selectedWorkspaceId,
  (workspaceId) => {
    void recommendations.fetchPendingRecommendations(workspaceId)
    void myTasks.fetchMyTasks(workspaceId)

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
    void myTasks.fetchMyTasks(workspaceTree.selectedWorkspaceId.value)
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
  void recommendations.fetchPendingRecommendations(
    workspaceTree.selectedWorkspaceId.value
  )
}

function generateRecommendations() {
  void recommendations.generateRecommendations(
    workspaceTree.selectedWorkspaceId.value,
    workspaceTree.selectedPageId.value
  )
}

async function acceptRecommendation(recommendationId: Guid) {
  const accepted = await recommendations.acceptRecommendation(recommendationId)

  if (accepted) {
    void myTasks.fetchMyTasks(workspaceTree.selectedWorkspaceId.value)
  }
}

function refreshMyTasks() {
  void myTasks.fetchMyTasks(workspaceTree.selectedWorkspaceId.value)
}

async function saveProfileSettings() {
  const saved = await settings.saveProfileSettings()

  if (saved) {
    await account.fetchMyProfile()
  }
}

async function saveTaskPreference() {
  const saved = await settings.saveTaskPreference(
    workspaceTree.selectedWorkspaceId.value
  )

  if (saved) {
    void recommendations.fetchPendingRecommendations(
      workspaceTree.selectedWorkspaceId.value
    )
  }
}

onMounted(() => {
  void account.fetchMyProfile()
  void workspaceTree.fetchMyWorkspaces()
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

:deep(.lunar-ai-card) {
  margin: 8px 0 0;
  padding: 4px 0 8px;
  border: 0;
  border-top: 1px solid #2b2b2b;
  border-radius: 0;
  background: transparent;
}

:deep(.lunar-ai-icon),
:deep(.lunar-ai-item),
:deep(.lunar-ai-item-meta span) {
  border: 0;
  background: transparent;
}

:deep(.lunar-ai-icon) {
  width: 20px;
  color: #777;
}

:deep(.lunar-ai-head) {
  padding: 0 4px 4px 7px;
}

:deep(.lunar-ai-list) {
  gap: 1px;
}

:deep(.lunar-ai-item) {
  border-radius: 6px;
  padding: 6px 7px 6px 34px;
}

:deep(.lunar-ai-item:hover) {
  background: #242424;
}

:deep(.lunar-ai-empty),
:deep(.lunar-ai-error) {
  padding-left: 34px;
}

:deep(.lunar-footer) {
  padding-top: 7px;
  border-top: 1px solid #2b2b2b;
}
</style>