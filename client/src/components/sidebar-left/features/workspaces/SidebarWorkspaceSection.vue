<template>
  <section class="lunar-section">
    <div class="lunar-section-head">
      <button
        type="button"
        class="lunar-section-toggle"
        :aria-expanded="isOpen"
        @click.stop="emit('toggle')"
      >
        <i
          class="bi bi-caret-right-fill lunar-caret"
          :class="{ open: isOpen }"
        ></i>

        <span>Workspaces</span>
      </button>

      <div class="lunar-section-actions">
        <button
          type="button"
          class="lunar-section-action"
          :class="{ refreshing: isRefreshingTree || isLoadingWorkspaces }"
          :disabled="isRefreshingTree || isLoadingWorkspaces"
          title="Đồng bộ workspace/page"
          @click.stop="emit('refreshTree')"
        >
          <i
            class="bi"
            :class="
              isRefreshingTree || isLoadingWorkspaces
                ? 'bi-arrow-repeat lunar-spin-icon'
                : 'bi-arrow-clockwise'
            "
          ></i>
        </button>

        <button
          type="button"
          class="lunar-section-action"
          title="Tạo workspace"
          @click.stop="emit('createWorkspace')"
        >
          <i class="bi bi-plus-lg"></i>
        </button>
      </div>
    </div>

    <Transition name="lunar-expand">
      <div
        v-show="isOpen"
        class="lunar-workspace-list"
      >
        <div
          v-if="isLoadingWorkspaces"
          class="lunar-empty"
        >
          Đang tải workspace…
        </div>

        <div
          v-else-if="workspaceListError"
          class="lunar-error"
        >
          <p>{{ workspaceListError }}</p>

          <button
            type="button"
            @click.stop="emit('retryWorkspaces')"
          >
            Thử lại
          </button>
        </div>

        <div
          v-else-if="!hasWorkspaces"
          class="lunar-empty"
        >
          Chưa có workspace. Nhấn <strong>+</strong> để tạo mới.
        </div>

        <template v-else>
          <div
            v-for="workspace in workspaces"
            :key="workspace.id"
            class="lunar-workspace-node"
          >
            <div
              class="lunar-workspace-row"
              :class="{ active: workspace.id === selectedWorkspaceId }"
            >
              <button
                type="button"
                class="lunar-tree-toggle"
                :aria-expanded="isWorkspaceBranchOpen(workspace.id)"
                @click.stop="emit('toggleWorkspace', workspace)"
              >
                <i
                  class="bi bi-caret-right-fill"
                  :class="{ open: isWorkspaceBranchOpen(workspace.id) }"
                ></i>
              </button>

              <button
                type="button"
                class="lunar-workspace-title"
                @click.stop="emit('toggleWorkspace', workspace)"
              >
                <span class="lunar-workspace-orb">
                  {{ workspace.name.charAt(0).toUpperCase() || 'W' }}
                </span>

                <span class="lunar-row-text">
                  {{ workspace.name }}
                </span>

                <span
                  v-if="workspace.currentUserRole"
                  class="lunar-role-chip"
                >
                  {{ workspace.currentUserRole }}
                </span>
              </button>

              <button
                type="button"
                class="lunar-row-action"
                title="Tạo page"
                @click.stop="emit('createPage', workspace, null)"
              >
                <i class="bi bi-plus-lg"></i>
              </button>
            </div>

            <Transition name="lunar-expand">
              <div
                v-show="isWorkspaceBranchOpen(workspace.id)"
                class="lunar-pages-branch"
              >
                <div
                  v-if="isLoadingPages(workspace.id)"
                  class="lunar-empty compact"
                >
                  Đang tải page…
                </div>

                <div
                  v-else-if="getPageListError(workspace.id)"
                  class="lunar-error compact"
                >
                  <p>{{ getPageListError(workspace.id) }}</p>

                  <button
                    type="button"
                    @click.stop="emit('retryPages', workspace.id)"
                  >
                    Tải lại page
                  </button>
                </div>

                <div
                  v-else-if="!getWorkspacePages(workspace.id).length"
                  class="lunar-empty compact"
                >
                  Chưa có page.
                </div>

                <SidebarPageTree
                  v-else
                  :pages="getWorkspacePages(workspace.id)"
                  :selected-page-id="selectedPageId"
                  :opened-page-ids="openedPageIds"
                  @select-page="emit('selectPage', $event)"
                  @create-child="emit('createPage', workspace, $event.id)"
                  @toggle-page="emit('togglePage', $event.id)"
                  @page-settings="emit('pageSettings', $event)"
                  @share-page="emit('sharePage', $event)"
                  @delete-page="emit('deletePage', $event)"
                />
              </div>
            </Transition>
          </div>
        </template>
      </div>
    </Transition>
  </section>
</template>

<script setup lang="ts">
import SidebarPageTree from './SidebarPageTree.vue'
import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'
import type { PageTreeItem } from '@/components/sidebar-left/types/sidebar.types'
import type { WorkspaceSidebarItem } from '@/modules/workspaces/composables/useMyWorkspaces'

defineProps<{
  isOpen: boolean
  workspaces: WorkspaceSidebarItem[]
  hasWorkspaces: boolean
  isLoadingWorkspaces: boolean
  isRefreshingTree: boolean
  workspaceListError: string | null
  selectedWorkspaceId: Guid | null
  selectedPageId: Guid | null
  openedPageIds: Set<Guid>
  isWorkspaceBranchOpen: (workspaceId: Guid) => boolean
  isLoadingPages: (workspaceId: Guid) => boolean
  getWorkspacePages: (workspaceId: Guid) => PageTreeItem[]
  getPageListError: (workspaceId: Guid) => string | null
}>()

const emit = defineEmits<{
  toggle: []
  createWorkspace: []
  refreshTree: []
  retryWorkspaces: []
  toggleWorkspace: [workspace: WorkspaceSidebarItem]
  createPage: [workspace: WorkspaceSidebarItem, parentPageId: Guid | null]
  retryPages: [workspaceId: Guid]
  selectPage: [page: PageResponse]
  togglePage: [pageId: Guid]
  pageSettings: [page: PageTreeItem]
  sharePage: [page: PageTreeItem]
  deletePage: [page: PageTreeItem]
}>()
</script>
