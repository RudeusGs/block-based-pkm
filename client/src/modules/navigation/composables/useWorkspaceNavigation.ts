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

export interface WorkspaceNavigationPageTab extends WorkspaceNavigationPage {
  workspaceName: string
}

const state = reactive<{
  workspace: WorkspaceNavigationWorkspace | null
  page: WorkspaceNavigationPage | null
  pageTabs: WorkspaceNavigationPageTab[]
}>({
  workspace: null,
  page: null,
  pageTabs: [],
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

function normalizePage(page: PageResponse | WorkspaceNavigationPage): WorkspaceNavigationPage {
  return {
    id: page.id,
    workspaceId: page.workspaceId,
    title: page.title,
    icon: 'icon' in page ? page.icon : null,
    coverImage: 'coverImage' in page ? page.coverImage : null,
    currentRevision: 'currentRevision' in page ? page.currentRevision : null,
  }
}

function upsertPageTab(page: WorkspaceNavigationPage) {
  const workspaceName =
    state.workspace?.id === page.workspaceId
      ? state.workspace.name
      : state.pageTabs.find((tab) => tab.workspaceId === page.workspaceId)?.workspaceName ??
        'Không gian'

  const tab: WorkspaceNavigationPageTab = {
    ...page,
    workspaceName,
  }

  const existingIndex = state.pageTabs.findIndex((item) => item.id === page.id)

  if (existingIndex >= 0) {
    state.pageTabs.splice(existingIndex, 1, tab)
    return
  }

  state.pageTabs = [...state.pageTabs, tab].slice(-8)
}

function setPage(page: PageResponse | WorkspaceNavigationPage | null) {
  if (!page) {
    state.page = null
    return
  }

  const normalized = normalizePage(page)

  state.page = normalized
  upsertPageTab(normalized)
}

function selectPageTab(pageId: Guid) {
  const tab = state.pageTabs.find((item) => item.id === pageId)
  if (!tab) return

  state.workspace = {
    id: tab.workspaceId,
    name: tab.workspaceName,
  }

  state.page = {
    id: tab.id,
    workspaceId: tab.workspaceId,
    title: tab.title,
    icon: tab.icon,
    coverImage: tab.coverImage,
    currentRevision: tab.currentRevision,
  }
}

function closePageTab(pageId: Guid) {
  const closingActivePage = state.page?.id === pageId
  state.pageTabs = state.pageTabs.filter((tab) => tab.id !== pageId)

  if (!closingActivePage) return

  const fallback = state.pageTabs[state.pageTabs.length - 1] ?? null

  if (!fallback) {
    state.page = null
    return
  }

  selectPageTab(fallback.id)
}

function setPageCoverImage(
  pageId: Guid,
  coverImage: string | null,
  currentRevision?: number | null
) {
  if (state.page && state.page.id === pageId) {
    state.page = {
      ...state.page,
      coverImage,
      currentRevision: currentRevision ?? state.page.currentRevision ?? null,
    }
  }

  state.pageTabs = state.pageTabs.map((tab) => {
    if (tab.id !== pageId) return tab

    return {
      ...tab,
      coverImage,
      currentRevision: currentRevision ?? tab.currentRevision ?? null,
    }
  })
}

function clearNavigation() {
  state.workspace = null
  state.page = null
  state.pageTabs = []
}

export function useWorkspaceNavigation() {
  return {
    workspace: computed(() => state.workspace),
    page: computed(() => state.page),
    pageTabs: computed(() => state.pageTabs),

    workspaceName: computed(() => state.workspace?.name || 'Chưa chọn không gian'),
    pageName: computed(() => state.page?.title || 'Chưa chọn trang'),
    pageIcon: computed(() => state.page?.icon || '📄'),
    pageCoverImage: computed(() => state.page?.coverImage || null),
    pageRevision: computed(() => state.page?.currentRevision ?? null),

    setWorkspace,
    setPage,
    selectPageTab,
    closePageTab,
    setPageCoverImage,
    clearNavigation,
  }
}
