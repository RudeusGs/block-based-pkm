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
          v-show="quickAccessVisible"
          class="lunar-quick-groups"
        >
          <div
            v-if="quickAccessError"
            class="lunar-error compact"
          >
            <p>{{ quickAccessError }}</p>
          </div>

          <section
            v-if="favoritePages.length"
            class="lunar-quick-group"
          >
            <div class="lunar-quick-title">
              <i class="bi bi-star-fill"></i>
              <span>Favorites</span>
            </div>

            <div
              v-for="page in favoritePages"
              :key="`favorite-${page.id}`"
              class="lunar-quick-row"
              :class="{ active: page.id === selectedPageId }"
              role="button"
              tabindex="0"
              @click.stop="emit('selectQuickPage', page)"
              @keydown.enter.stop="emit('selectQuickPage', page)"
            >
              <span class="lunar-page-icon">{{ page.icon || '◇' }}</span>
              <span class="lunar-row-text">{{ page.title }}</span>
              <button
                type="button"
                class="lunar-quick-action"
                title="Bỏ favorite"
                @click.stop="emit('toggleFavoritePage', page)"
              >
                <i class="bi bi-star-fill"></i>
              </button>
            </div>
          </section>

          <section
            v-if="recentPages.length"
            class="lunar-quick-group"
          >
            <div class="lunar-quick-title">
              <i class="bi bi-clock-history"></i>
              <span>Recent</span>
            </div>

            <button
              v-for="page in recentPages"
              :key="`recent-${page.id}`"
              type="button"
              class="lunar-quick-row"
              :class="{ active: page.id === selectedPageId }"
              @click.stop="emit('selectQuickPage', page)"
            >
              <span class="lunar-page-icon">{{ page.icon || '◇' }}</span>
              <span class="lunar-row-text">{{ page.title }}</span>
              <span class="lunar-quick-meta">{{ page.workspaceName }}</span>
            </button>
          </section>

          <section class="lunar-quick-group lunar-trash-group">
            <div class="lunar-quick-title">
              <i class="bi bi-archive"></i>
              <span>Trash</span>
              <small v-if="isLoadingTrash">Syncing…</small>
            </div>

            <div
              v-if="isLoadingTrash && !archivedPages.length"
              class="lunar-empty compact"
            >
              Đang tải thùng rác…
            </div>

            <div
              v-else-if="trashError"
              class="lunar-error compact"
            >
              <p>{{ trashError }}</p>
            </div>

            <div
              v-else-if="!archivedPages.length"
              class="lunar-empty compact lunar-trash-empty"
            >
              Trash đang trống. Page đã xóa sẽ nằm ở đây.
            </div>

            <template v-else>
              <button
                v-for="page in archivedPages"
                :key="`trash-${page.id}`"
                type="button"
                class="lunar-quick-row muted"
                @click.stop="emit('restorePage', page)"
              >
                <span class="lunar-page-icon">{{ page.icon || '◇' }}</span>
                <span class="lunar-row-text">{{ page.title }}</span>
                <span class="lunar-quick-action restore">
                  <i class="bi bi-arrow-counterclockwise"></i>
                </span>
              </button>
            </template>
          </section>
        </div>
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
                @click.stop="emit('selectWorkspace', workspace)"
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

                <span
                  class="lunar-visibility-chip"
                  :class="visibilityClass(workspace.visibility)"
                  :title="visibilityLabel(workspace.visibility)"
                >
                  {{ visibilityShortLabel(workspace.visibility) }}
                </span>
              </button>

              <button
                v-if="canCreatePages(workspace)"
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
                  {{ canCreatePages(workspace) ? 'Chưa có page.' : 'Chưa có page để xem.' }}
                </div>

                <SidebarPageTree
                  v-else
                  :pages="getWorkspacePages(workspace.id)"
                  :selected-page-id="selectedPageId"
                  :opened-page-ids="openedPageIds"
                  :can-create-child="canCreatePages(workspace)"
                  :can-manage-pages="canManagePages(workspace)"
                  :is-favorite-page="isFavoritePage"
                  @select-page="emit('selectPage', $event)"
                  @create-child="emit('createPage', workspace, $event.id)"
                  @toggle-page="emit('togglePage', $event.id)"
                  @page-settings="emit('pageSettings', $event)"
                  @share-page="emit('sharePage', $event)"
                  @delete-page="emit('deletePage', $event)"
                  @duplicate-page="emit('duplicatePage', $event)"
                  @toggle-favorite-page="emit('toggleFavoritePage', $event)"
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
import { computed } from 'vue'
import SidebarPageTree from './SidebarPageTree.vue'
import type { Guid } from '@/api/models/common.model'
import type { PageQuickAccessResponse, PageResponse } from '@/api/models/page.model'
import type { PageTreeItem } from '@/components/sidebar-left/types/sidebar.types'
import type { WorkspaceSidebarItem } from '@/modules/workspaces/composables/useMyWorkspaces'

const props = defineProps<{
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
  favoritePages: PageQuickAccessResponse[]
  recentPages: PageQuickAccessResponse[]
  archivedPages: PageResponse[]
  isLoadingQuickAccess: boolean
  isLoadingTrash: boolean
  quickAccessError: string | null
  trashError: string | null
  isFavoritePage: (pageId: Guid) => boolean
}>()

const emit = defineEmits<{
  toggle: []
  createWorkspace: []
  refreshTree: []
  retryWorkspaces: []
  toggleWorkspace: [workspace: WorkspaceSidebarItem]
  selectWorkspace: [workspace: WorkspaceSidebarItem]
  createPage: [workspace: WorkspaceSidebarItem, parentPageId: Guid | null]
  retryPages: [workspaceId: Guid]
  selectPage: [page: PageResponse]
  togglePage: [pageId: Guid]
  pageSettings: [page: PageTreeItem]
  sharePage: [page: PageTreeItem]
  deletePage: [page: PageTreeItem]
  duplicatePage: [page: PageTreeItem | PageQuickAccessResponse]
  toggleFavoritePage: [page: PageTreeItem | PageQuickAccessResponse]
  selectQuickPage: [page: PageQuickAccessResponse]
  restorePage: [page: PageResponse]
}>()

function normalizeRole(workspace: WorkspaceSidebarItem) {
  return workspace.currentUserRole?.trim().toLowerCase() ?? ''
}

function canCreatePages(workspace: WorkspaceSidebarItem) {
  const role = normalizeRole(workspace)

  return role === 'owner' || role === 'manager' || role === 'member'
}

function canManagePages(workspace: WorkspaceSidebarItem) {
  const role = normalizeRole(workspace)

  return role === 'owner' || role === 'manager'
}

function normalizeVisibility(value: string | null | undefined) {
  return value?.trim().toLowerCase() === 'public' ? 'public' : 'private'
}

function visibilityLabel(value: string | null | undefined) {
  return normalizeVisibility(value) === 'public'
    ? 'Public workspace: người có tài khoản đều có thể xem'
    : 'Private workspace: chỉ member trong workspace xem được'
}

function visibilityShortLabel(value: string | null | undefined) {
  return normalizeVisibility(value) === 'public' ? 'Public' : 'Private'
}

function visibilityClass(value: string | null | undefined) {
  return normalizeVisibility(value) === 'public' ? 'public' : 'private'
}

const quickAccessVisible = computed(() => {
  return (
    props.hasWorkspaces ||
    Boolean(props.selectedWorkspaceId) ||
    props.isLoadingQuickAccess ||
    Boolean(props.quickAccessError) ||
    props.favoritePages.length > 0 ||
    props.recentPages.length > 0 ||
    props.archivedPages.length > 0 ||
    props.isLoadingTrash ||
    Boolean(props.trashError)
  )
})
</script>

<style scoped>
.lunar-visibility-chip {
  min-width: 0;
  max-width: 54px;
  overflow: hidden;
  border-radius: 999px;
  padding: 1px 5px;
  color: #858585;
  background: transparent;
  font-size: 10px;
  font-weight: 600;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.lunar-visibility-chip.public {
  color: #d6d6d6;
  background: rgba(255, 255, 255, 0.08);
}

.lunar-visibility-chip.private {
  color: #8f8f8f;
}


.lunar-quick-groups {
  display: grid;
  gap: 6px;
  margin-bottom: 8px;
}

.lunar-quick-group {
  display: grid;
  gap: 2px;
}

.lunar-quick-title {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 5px 7px 3px;
  color: #858585;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.02em;
  text-transform: uppercase;
}

.lunar-quick-title small {
  margin-left: auto;
  color: #696969;
  font-size: 9.5px;
  font-weight: 700;
  letter-spacing: 0.03em;
}

.lunar-trash-group {
  min-height: 62px;
}

.lunar-trash-empty {
  min-height: 30px;
  display: flex;
  align-items: center;
}

.lunar-quick-row {
  width: 100%;
  min-height: 30px;
  border: 0;
  border-radius: 6px;
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 5px 7px;
  color: #cfcfcf;
  background: transparent;
  text-align: left;
}

.lunar-quick-row:hover,
.lunar-quick-row.active {
  background: #242424;
  color: #f1f1f1;
}

.lunar-quick-row.muted {
  color: #9f9f9f;
}

.lunar-quick-action {
  margin-left: auto;
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  color: #9d8f5d;
  background: transparent;
}

.lunar-quick-action:hover {
  background: rgba(255, 255, 255, 0.08);
}

.lunar-quick-action.restore {
  color: #8fc79b;
}

.lunar-quick-meta {
  max-width: 78px;
  overflow: hidden;
  margin-left: auto;
  color: #777;
  font-size: 10px;
  white-space: nowrap;
  text-overflow: ellipsis;
}

</style>
