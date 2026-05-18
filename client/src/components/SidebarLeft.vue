<template>
  <aside
    class="sidebar-left d-flex flex-column py-4 px-3 border-end border-outline-variant transition-300"
    :class="{ collapsed: isCollapsed, 'sidebar-clickable': isCollapsed }"
    @click="expandSidebar"
  >
    <div
      class="sidebar-header d-flex align-items-center mb-4 px-2"
      :class="isCollapsed ? 'justify-content-center' : 'justify-content-between'"
    >
      <div class="d-flex align-items-center overflow-hidden">
        <div
          class="logo-box rounded-3 bg-dark d-flex align-items-center justify-content-center text-white flex-shrink-0"
        >
          <span class="material-symbols-outlined fill-1">grid_view</span>
        </div>

        <div
          v-if="!isCollapsed"
          class="d-flex flex-column overflow-hidden ms-3 fade-in"
        >
          <span class="fs-6 fw-bold text-light text-nowrap">Block-based</span>
          <span
            class="text-uppercase text-on-surface-variant tracking-widest"
            style="font-size: 10px"
          >
            Pro Workspace
          </span>
        </div>
      </div>

      <button
        v-if="!isCollapsed"
        type="button"
        class="btn-text-toggle fade-in flex-shrink-0"
        @click.stop="collapseSidebar"
      >
        &lt;&lt;
      </button>
    </div>

    <div class="mb-4 px-1">
      <div
        class="search-wrapper d-flex align-items-center rounded-4 border border-outline-variant transition-200"
        :class="isCollapsed ? 'justify-content-center py-2' : 'px-3 py-2 gap-2'"
      >
        <span class="material-symbols-outlined text-on-surface-variant fs-6 flex-shrink-0">
          search
        </span>

        <input
          v-if="!isCollapsed"
          type="text"
          class="search-input bg-transparent border-0 flex-grow-1 text-white fs-7 fade-in"
          placeholder="Search..."
        />
      </div>
    </div>

    <nav class="flex-grow-1 overflow-x-hidden overflow-y-auto scrollbar-hide">
      <div class="px-1 pb-2">
        <div class="accordion-projects position-relative">
          <div
            class="d-flex align-items-center nav-link-custom group-item position-relative"
            :class="{ 'justify-content-center ps-0': isCollapsed }"
          >
            <button
              type="button"
              class="border-0 d-flex align-items-center bg-transparent text-inherit w-100"
              :class="isCollapsed ? 'justify-content-center p-0' : 'gap-2 py-2 ps-3'"
              @click.stop="toggleWorkspaces"
            >
              <span class="material-symbols-outlined flex-shrink-0 text-white folder-icon">
                workspaces
              </span>

              <span
                v-if="!isCollapsed"
                class="fade-in text-start flex-grow-1 ms-1 fw-medium text-white"
              >
                Workspaces
              </span>

              <span
                v-if="!isCollapsed"
                class="material-symbols-outlined fs-7 transition-200 me-2 text-on-surface-variant"
                :class="{ 'rotate-minus-90': !isWorkspacesOpen }"
              >
                expand_more
              </span>
            </button>

            <button
              v-if="!isCollapsed"
              type="button"
              class="btn-add-small fade-in ms-auto me-2"
              title="Tạo workspace"
              @click.stop="openCreateWorkspaceModal"
            >
              <span class="material-symbols-outlined">add</span>
            </button>
          </div>

          <Transition name="expand">
            <div
              v-show="!isCollapsed && isWorkspacesOpen"
              class="project-tree-container ms-4 ps-2 mt-1 border-start"
            >
              <div
                v-if="isLoadingWorkspaces"
                class="workspace-list-hint py-2 ps-2 fs-7 text-on-surface-variant"
              >
                Đang tải…
              </div>

              <div
                v-else-if="workspaceListError"
                class="workspace-list-error py-2 ps-2"
              >
                <div class="fs-7 text-danger mb-2">
                  {{ workspaceListError }}
                </div>

                <button
                  type="button"
                  class="btn btn-sm btn-outline-light py-0 px-2 fs-7"
                  @click.stop="retryLoadWorkspaces"
                >
                  Thử lại
                </button>
              </div>

              <div
                v-else-if="!hasWorkspaces"
                class="workspace-list-hint py-2 ps-2 fs-7 text-on-surface-variant"
              >
                Chưa có workspace. Nhấn <span class="text-white">+</span> để tạo mới.
              </div>

              <template v-else>
                <div
                  v-for="ws in workspaces"
                  :key="ws.id"
                  class="workspace-tree-node"
                >
                  <div class="d-flex align-items-center pe-2 group-item">
                    <button
                      type="button"
                      class="workspace-branch-toggle"
                      @click.stop="toggleWorkspaceBranch(ws)"
                    >
                      <span
                        class="material-symbols-outlined"
                        :class="{ 'rotate-90': isWorkspaceBranchOpen(ws.id) }"
                      >
                        chevron_right
                      </span>
                    </button>

                    <button
                      type="button"
                      class="project-child-link border-0 d-flex align-items-center py-1 px-0 bg-transparent fs-7 flex-grow-1 text-start w-100"
                      :class="{ 'workspace-row-active': ws.id === selectedWorkspaceId }"
                      @click.stop="toggleWorkspaceBranch(ws)"
                    >
                      <span class="tree-dot me-2 flex-shrink-0"></span>
                      <span class="flex-grow-1 text-truncate">{{ ws.name }}</span>
                    </button>

                    <button
                      type="button"
                      class="btn-add-child"
                      title="Tạo page"
                      @click.stop="openCreatePageModal(ws)"
                    >
                      <span class="material-symbols-outlined">add</span>
                    </button>
                  </div>

                  <Transition name="expand">
                    <div
                      v-show="isWorkspaceBranchOpen(ws.id)"
                      class="workspace-pages-branch ms-3 ps-2 border-start"
                    >
                      <div
                        v-if="isLoadingPages(ws.id)"
                        class="workspace-list-hint py-1 ps-2 fs-7 text-on-surface-variant"
                      >
                        Đang tải page…
                      </div>

                      <div
                        v-else-if="getPageListError(ws.id)"
                        class="workspace-list-error py-1 ps-2"
                      >
                        <div class="fs-7 text-danger mb-1">
                          {{ getPageListError(ws.id) }}
                        </div>

                        <button
                          type="button"
                          class="btn btn-sm btn-outline-light py-0 px-2 fs-7"
                          @click.stop="retryLoadPages(ws.id)"
                        >
                          Tải lại page
                        </button>
                      </div>

                      <div
                        v-else-if="!getWorkspacePages(ws.id).length"
                        class="workspace-list-hint py-1 ps-2 fs-7 text-on-surface-variant"
                      >
                        Chưa có page.
                      </div>

                      <SidebarPageTree
                        v-else
                        :pages="getWorkspacePages(ws.id)"
                        :selected-page-id="selectedPageId"
                        @select-page="selectPage"
                        @create-child="handleCreateChildPage(ws, $event)"
                      />
                    </div>
                  </Transition>
                </div>
              </template>
            </div>
          </Transition>
        </div>

        <div
          class="d-flex align-items-center nav-link-custom position-relative"
          :class="[isCollapsed ? 'justify-content-center ps-0 mt-4' : 'mt-1']"
        >
          <button
            type="button"
            class="border-0 d-flex align-items-center bg-transparent text-inherit w-100"
            :class="isCollapsed ? 'justify-content-center p-0' : 'gap-2 py-2 ps-3'"
          >
            <span class="material-symbols-outlined flex-shrink-0 text-white folder-icon">
              task_alt
            </span>

            <span
              v-if="!isCollapsed"
              class="fade-in text-start flex-grow-1 ms-1 fw-medium text-white"
            >
              My Tasks
            </span>
          </button>
        </div>
      </div>
    </nav>

    <div class="mt-auto px-1 pb-2 pt-3 border-top border-outline-variant">
      <div class="d-flex flex-column gap-1">
        <a
          href="#"
          class="nav-link-custom sidebar-profile-link d-flex align-items-center rounded-3"
          :class="isCollapsed ? 'justify-content-center py-2' : 'gap-3 py-2 ps-3 pe-2'"
          title="Profile"
        >
          <span class="sidebar-profile-avatar flex-shrink-0">
            <img
              v-if="profileAvatarUrl"
              :src="profileAvatarUrl"
              :alt="profileDisplayName"
              class="sidebar-profile-avatar-img"
            />

            <span
              v-else
              class="sidebar-profile-avatar-fallback"
            >
              {{ profileInitial }}
            </span>
          </span>

          <span
            v-if="!isCollapsed"
            class="sidebar-profile-meta fade-in min-w-0"
          >
            <span class="sidebar-profile-name text-truncate">
              {{ isLoadingProfile ? 'Đang tải...' : profileDisplayName }}
            </span>

            <span class="sidebar-profile-subtitle text-truncate">
              {{ profileSubtitle }}
            </span>
          </span>
        </a>

        <a
          href="#"
          class="nav-link-custom d-flex align-items-center rounded-3"
          :class="isCollapsed ? 'justify-content-center py-2' : 'gap-3 py-2 ps-3'"
        >
          <span class="material-symbols-outlined flex-shrink-0">settings</span>
          <span v-if="!isCollapsed" class="fade-in">Settings</span>
        </a>
      </div>
    </div>

    <CreateWorkspaceModal
      v-model="isCreateWorkspaceModalOpen"
      @created="handleWorkspaceCreated"
    />

    <CreatePageModal
      v-model="isCreatePageModalOpen"
      :workspace-id="createPageWorkspaceId"
      :workspace-name="createPageWorkspaceName"
      :parent-page-id="createPageParentPageId"
      @created="handlePageCreated"
    />
  </aside>
</template>

<script setup lang="ts">
import CreateWorkspaceModal from '@/components/workspace/CreateWorkspaceModal.vue'
import CreatePageModal from '@/components/page/CreatePageModal.vue'
import SidebarPageTree from '@/components/layout/SidebarPageTree.vue'
import { useSidebarLeft } from './composables/useSidebarLeft'
import type { PageTreeItem } from './composables/useSidebarLeft'
import type { WorkspaceSidebarItem } from '@/modules/workspaces/composables/useMyWorkspaces'

const {
  isCollapsed,
  isWorkspacesOpen,
  isCreateWorkspaceModalOpen,
  isCreatePageModalOpen,
  selectedWorkspaceId,
  selectedPageId,
  createPageWorkspaceId,
  createPageWorkspaceName,
  createPageParentPageId,

  workspaces,
  hasWorkspaces,
  isLoadingWorkspaces,
  workspaceListError,

  profileDisplayName,
  profileSubtitle,
  profileAvatarUrl,
  profileInitial,
  isLoadingProfile,

  expandSidebar,
  collapseSidebar,
  toggleWorkspaces,
  openCreateWorkspaceModal,
  openCreatePageModal,
  toggleWorkspaceBranch,
  isWorkspaceBranchOpen,
  isLoadingPages,
  getWorkspacePages,
  getPageListError,
  retryLoadPages,
  selectPage,
  handleWorkspaceCreated,
  handlePageCreated,
  retryLoadWorkspaces,
} = useSidebarLeft()

function handleCreateChildPage(
  workspace: WorkspaceSidebarItem,
  page: PageTreeItem
) {
  openCreatePageModal(workspace, page.id)
}
</script>

<style scoped src="./css/SidebarLeft.css"></style>