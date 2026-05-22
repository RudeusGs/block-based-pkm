import { computed, reactive } from 'vue'
import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'

export interface WorkspaceNavigationWorkspace {
  id: Guid
  name: string
}

export interface WorkspaceNavigationPage {
  id: Guid
  workspaceId: Guid
  title: string
  icon?: string | null
  coverImage?: string | null
  currentRevision?: number | null
}

const state = reactive<{
  workspace: WorkspaceNavigationWorkspace | null
  page: WorkspaceNavigationPage | null
}>({
  workspace: null,
  page: null,
})

function setWorkspace(workspace: WorkspaceNavigationWorkspace | null) {
  state.workspace = workspace

  if (!workspace) {
    state.page = null
    return
  }

  if (state.page && state.page.workspaceId !== workspace.id) {
    state.page = null
  }
}

function setPage(page: PageResponse | WorkspaceNavigationPage | null) {
  if (!page) {
    state.page = null
    return
  }

  state.page = {
    id: page.id,
    workspaceId: page.workspaceId,
    title: page.title,
    icon: 'icon' in page ? page.icon : null,
    coverImage: 'coverImage' in page ? page.coverImage : null,
    currentRevision: 'currentRevision' in page ? page.currentRevision : null,
  }
}

function setPageCoverImage(
  pageId: Guid,
  coverImage: string | null,
  currentRevision?: number | null
) {
  if (!state.page || state.page.id !== pageId) return

  state.page = {
    ...state.page,
    coverImage,
    currentRevision: currentRevision ?? state.page.currentRevision ?? null,
  }
}

function clearNavigation() {
  state.workspace = null
  state.page = null
}

export function useWorkspaceNavigation() {
  return {
    workspace: computed(() => state.workspace),
    page: computed(() => state.page),

    workspaceName: computed(() => state.workspace?.name || 'No workspace'),
    pageName: computed(() => state.page?.title || 'No page'),
    pageIcon: computed(() => state.page?.icon || '📄'),
    pageCoverImage: computed(() => state.page?.coverImage || null),
    pageRevision: computed(() => state.page?.currentRevision ?? null),

    setWorkspace,
    setPage,
    setPageCoverImage,
    clearNavigation,
  }
}


