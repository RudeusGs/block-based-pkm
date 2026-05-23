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

function normalizePage(page: PageResponse | WorkspaceNavigationPage) {
  return {
    id: page.id,
    workspaceId: page.workspaceId,
    title: page.title,
    icon: 'icon' in page ? page.icon : null,
    coverImage: 'coverImage' in page ? page.coverImage : null,
    currentRevision: 'currentRevision' in page ? page.currentRevision : null,
  }
}

function resolveWorkspaceName(workspaceId: Guid) {
  if (state.workspace?.id === workspaceId) {
    return state.workspace.name
  }

  const existingTab = state.pageTabs.find((tab) => tab.workspaceId === workspaceId)
  return existingTab?.workspaceName ?? 'Workspace'
}

function upsertPageTab(page: WorkspaceNavigationPage) {
  const tab: WorkspaceNavigationPageTab = {
    ...page,
    workspaceName: resolveWorkspaceName(page.workspaceId),
  }

  const existingIndex = state.pageTabs.findIndex((item) => item.id === page.id)

  if (existingIndex >= 0) {
    const existingTab = state.pageTabs[existingIndex]
    if (!existingTab) return

    state.pageTabs.splice(existingIndex, 1, {
      ...existingTab,
      ...tab,
      workspaceName:
        tab.workspaceName === 'Workspace'
          ? existingTab.workspaceName
          : tab.workspaceName,
    })
    return
  }

  state.pageTabs.push(tab)
}

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

  const nextPage = normalizePage(page)

  state.page = nextPage
  upsertPageTab(nextPage)
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

  const existingIndex = state.pageTabs.findIndex((tab) => tab.id === pageId)

  if (existingIndex >= 0) {
    const existingTab = state.pageTabs[existingIndex]
    if (!existingTab) return

    state.pageTabs.splice(existingIndex, 1, {
      ...existingTab,
      coverImage,
      currentRevision: currentRevision ?? existingTab.currentRevision ?? null,
    })
  }
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
  const closingIndex = state.pageTabs.findIndex((tab) => tab.id === pageId)
  if (closingIndex < 0) return

  const isActiveTab = state.page?.id === pageId
  const nextTabs = state.pageTabs.filter((tab) => tab.id !== pageId)

  state.pageTabs = nextTabs

  if (!isActiveTab) return

  const nextTab = nextTabs[closingIndex] ?? nextTabs[closingIndex - 1] ?? null

  if (!nextTab) {
    state.page = null
    return
  }

  selectPageTab(nextTab.id)
}

function closeWorkspacePageTabs(workspaceId: Guid) {
  const activePageBelongsToWorkspace = state.page?.workspaceId === workspaceId

  state.pageTabs = state.pageTabs.filter((tab) => tab.workspaceId !== workspaceId)

  if (activePageBelongsToWorkspace) {
    const nextTab = state.pageTabs[0] ?? null

    if (nextTab) {
      selectPageTab(nextTab.id)
    } else {
      state.page = null
    }
  }
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

    workspaceName: computed(() => state.workspace?.name || 'No workspace'),
    pageName: computed(() => state.page?.title || 'No page'),
    pageIcon: computed(() => state.page?.icon || '📄'),
    pageCoverImage: computed(() => state.page?.coverImage || null),
    pageRevision: computed(() => state.page?.currentRevision ?? null),

    setWorkspace,
    setPage,
    setPageCoverImage,
    selectPageTab,
    closePageTab,
    closeWorkspacePageTabs,
    clearNavigation,
  }
}


