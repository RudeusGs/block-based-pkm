<template>
  <aside
    class="sidebar-left lunar-sidebar"
    :class="{ collapsed: isCollapsed, 'sidebar-clickable': isCollapsed }"
    @click="expandSidebar"
  >
    <template v-if="isCollapsed">
      <div class="lunar-rail">
        <button
          type="button"
          class="lunar-rail-avatar"
          title="Mở sidebar"
        >
          <img
            v-if="profileAvatarUrl"
            :src="profileAvatarUrl"
            :alt="profileDisplayName"
          />

          <span v-else>
            {{ profileInitial }}
          </span>
        </button>

        <button
          type="button"
          class="lunar-rail-btn"
          title="AI gợi ý task"
        >
          <i class="bi bi-magic"></i>
        </button>

        <button
          type="button"
          class="lunar-rail-btn"
          title="Workspaces"
        >
          <i class="bi bi-folder2-open"></i>
        </button>

        <button
          type="button"
          class="lunar-rail-btn"
          title="My Tasks"
        >
          <i class="bi bi-check2-square"></i>
        </button>

        <button
          type="button"
          class="lunar-rail-btn mt-auto"
          title="Settings"
        >
          <i class="bi bi-sliders2"></i>
        </button>
      </div>
    </template>

    <template v-else>
      <div class="lunar-shell">
        <header class="lunar-header">
          <button
            type="button"
            class="lunar-account-card"
            title="Workspace switcher"
            @click.stop
          >
            <span class="lunar-account-avatar">
              <img
                v-if="profileAvatarUrl"
                :src="profileAvatarUrl"
                :alt="profileDisplayName"
              />

              <span v-else>
                {{ profileInitial }}
              </span>
            </span>

            <span class="lunar-account-meta">
              <span class="lunar-account-name">
                {{ isLoadingProfile ? 'Đang tải...' : profileDisplayName }}
              </span>

              <span class="lunar-account-subtitle">
                {{ profileSubtitle }}
              </span>
            </span>

            <span class="lunar-account-badge">
              Pro
            </span>

            <i class="bi bi-chevron-down lunar-account-chevron"></i>
          </button>

          <button
            type="button"
            class="lunar-icon-btn"
            title="Thu gọn sidebar"
            @click.stop="collapseSidebar"
          >
            <i class="bi bi-layout-sidebar-inset"></i>
          </button>
        </header>

        <section class="lunar-ai-card">
          <div class="lunar-ai-head">
            <div class="lunar-ai-title-wrap">

              <div class="lunar-ai-title-text">
                <span class="lunar-ai-title">
                  AI gợi ý task
                </span>

                <span class="lunar-ai-subtitle">
                  Dựa trên workspace/page đang chọn
                </span>
              </div>
            </div>

            <button
              type="button"
              class="lunar-ai-generate-btn"
              :disabled="!selectedWorkspaceId || isGeneratingTaskRecommendations"
              title="Tạo gợi ý task"
              @click.stop="generateTaskRecommendations"
            >
              <span
                v-if="isGeneratingTaskRecommendations"
                class="lunar-ai-spinner"
              ></span>

              <i
                v-else
                class="bi bi-arrow-clockwise"
              ></i>
            </button>
          </div>

          <div
            v-if="!selectedWorkspaceId"
            class="lunar-ai-empty"
          >
            Chọn workspace để AI gợi ý task.
          </div>

          <div
            v-else-if="taskRecommendationError"
            class="lunar-ai-error"
          >
            <p>
              {{ taskRecommendationError }}
            </p>

            <button
              type="button"
              @click.stop="retryLoadTaskRecommendations"
            >
              Tải lại
            </button>
          </div>

          <div
            v-else-if="isLoadingTaskRecommendations"
            class="lunar-ai-empty"
          >
            Đang tải gợi ý…
          </div>

          <div
            v-else-if="!hasTaskRecommendations"
            class="lunar-ai-empty"
          >
            Chưa có gợi ý. Nhấn nút làm mới để AI tạo task phù hợp.
          </div>

          <div
            v-else
            class="lunar-ai-list"
          >
            <article
              v-for="recommendation in taskRecommendations"
              :key="recommendation.id"
              class="lunar-ai-item"
            >
              <div class="lunar-ai-item-main">
                <div class="lunar-ai-item-title">
                  {{ recommendation.taskTitle }}
                </div>

                <div
                  v-if="recommendation.reason"
                  class="lunar-ai-item-reason"
                >
                  {{ recommendation.reason }}
                </div>

                <div class="lunar-ai-item-meta">
                  <span>
                    {{ recommendation.taskPriority }}
                  </span>

                  <span>
                    Score {{ recommendation.score }}
                  </span>
                </div>
              </div>

              <div class="lunar-ai-actions">
                <button
                  type="button"
                  title="Chấp nhận gợi ý"
                  @click.stop="acceptTaskRecommendation(recommendation.id)"
                >
                  <i class="bi bi-check-lg"></i>
                </button>

                <button
                  type="button"
                  title="Từ chối gợi ý"
                  @click.stop="rejectTaskRecommendation(recommendation.id)"
                >
                  <i class="bi bi-x-lg"></i>
                </button>
              </div>
            </article>
          </div>
        </section>

        <nav class="lunar-nav">
          <div class="lunar-primary-nav">
            <button
              type="button"
              class="lunar-nav-row"
              @click.stop
            >
              <span class="lunar-nav-icon">
                <i class="bi bi-clock-history"></i>
              </span>

              <span class="lunar-nav-label">
                Updates
              </span>
            </button>

            <button
              type="button"
              class="lunar-nav-row"
              @click.stop
            >
              <span class="lunar-nav-icon">
                <i class="bi bi-check2-square"></i>
              </span>

              <span class="lunar-nav-label">
                My Tasks
              </span>
            </button>
          </div>

          <section class="lunar-section">
            <div class="lunar-section-head">
              <button
                type="button"
                class="lunar-section-toggle"
                :aria-expanded="isWorkspacesOpen"
                @click.stop="toggleWorkspaces"
              >
                <i
                  class="bi bi-caret-right-fill lunar-caret"
                  :class="{ open: isWorkspacesOpen }"
                ></i>

                <span>
                  Workspaces
                </span>
              </button>

              <button
                type="button"
                class="lunar-section-action"
                title="Tạo workspace"
                @click.stop="openCreateWorkspaceModal"
              >
                <i class="bi bi-plus-lg"></i>
              </button>
            </div>

            <Transition name="lunar-expand">
              <div
                v-show="isWorkspacesOpen"
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
                  <p>
                    {{ workspaceListError }}
                  </p>

                  <button
                    type="button"
                    @click.stop="retryLoadWorkspaces"
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
                    v-for="ws in workspaces"
                    :key="ws.id"
                    class="lunar-workspace-node"
                  >
                    <div
                      class="lunar-workspace-row"
                      :class="{ active: ws.id === selectedWorkspaceId }"
                    >
                      <button
                        type="button"
                        class="lunar-tree-toggle"
                        :aria-expanded="isWorkspaceBranchOpen(ws.id)"
                        @click.stop="toggleWorkspaceBranch(ws)"
                      >
                        <i
                          class="bi bi-caret-right-fill"
                          :class="{ open: isWorkspaceBranchOpen(ws.id) }"
                        ></i>
                      </button>

                      <button
                        type="button"
                        class="lunar-workspace-title"
                        @click.stop="toggleWorkspaceBranch(ws)"
                      >
                        <span class="lunar-workspace-orb">
                          {{ ws.name.charAt(0).toUpperCase() || 'W' }}
                        </span>

                        <span class="lunar-row-text">
                          {{ ws.name }}
                        </span>
                      </button>

                      <button
                        type="button"
                        class="lunar-row-action"
                        title="Tạo page"
                        @click.stop="openCreatePageModal(ws)"
                      >
                        <i class="bi bi-plus-lg"></i>
                      </button>
                    </div>

                    <Transition name="lunar-expand">
                      <div
                        v-show="isWorkspaceBranchOpen(ws.id)"
                        class="lunar-pages-branch"
                      >
                        <div
                          v-if="isLoadingPages(ws.id)"
                          class="lunar-empty compact"
                        >
                          Đang tải page…
                        </div>

                        <div
                          v-else-if="getPageListError(ws.id)"
                          class="lunar-error compact"
                        >
                          <p>
                            {{ getPageListError(ws.id) }}
                          </p>

                          <button
                            type="button"
                            @click.stop="retryLoadPages(ws.id)"
                          >
                            Tải lại page
                          </button>
                        </div>

                        <div
                          v-else-if="!getWorkspacePages(ws.id).length"
                          class="lunar-empty compact"
                        >
                          Chưa có page.
                        </div>

                        <SidebarPageTree
                          v-else
                          :pages="getWorkspacePages(ws.id)"
                          :selected-page-id="selectedPageId"
                          :opened-page-ids="openedPageIds"
                          @select-page="selectPage"
                          @create-child="handleCreateChildPage(ws, $event)"
                          @toggle-page="togglePageBranch($event.id)"
                        />
                      </div>
                    </Transition>
                  </div>
                </template>
              </div>
            </Transition>
          </section>
        </nav>

        <footer class="lunar-footer">
          <a
            href="#"
            class="lunar-footer-row"
            @click.prevent.stop
          >
            <span class="lunar-footer-icon">
              <i class="bi bi-sliders2"></i>
            </span>

            <span class="lunar-footer-label">
              Settings
            </span>
          </a>
        </footer>
      </div>
    </template>

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
import type { PageTreeItem } from '@/components/types/sidebar.types'
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

  taskRecommendations,
  hasTaskRecommendations,
  isLoadingTaskRecommendations,
  isGeneratingTaskRecommendations,
  taskRecommendationError,

  expandSidebar,
  collapseSidebar,
  toggleWorkspaces,
  openCreateWorkspaceModal,
  openCreatePageModal,
  toggleWorkspaceBranch,
  isWorkspaceBranchOpen,
  openedPageIds,
  togglePageBranch,
  isLoadingPages,
  getWorkspacePages,
  getPageListError,
  retryLoadPages,
  selectPage,
  handleWorkspaceCreated,
  handlePageCreated,
  retryLoadWorkspaces,

  retryLoadTaskRecommendations,
  generateTaskRecommendations,
  acceptTaskRecommendation,
  rejectTaskRecommendation,
} = useSidebarLeft()

function handleCreateChildPage(
  workspace: WorkspaceSidebarItem,
  page: PageTreeItem
) {
  openCreatePageModal(workspace, page.id)
}
</script>

<style scoped src="./css/SidebarLeft.css"></style>