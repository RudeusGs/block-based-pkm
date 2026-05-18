import { onMounted, ref, watch } from 'vue'
import { pageController } from '@/api/services/page.api'
import { useMyWorkspaces } from '@/modules/workspaces/composables/useMyWorkspaces'
import { useMyProfile } from '@/modules/account/composables/useMyProfile'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'

import type { PageTreeItem, SidebarWorkspaceLike } from '../types/sidebar.types'
import { buildPageTree, insertPageIntoTree } from '../utils/page-tree.util'

export function useSidebarLeft() {
  const isCollapsed = ref(false)
  const isWorkspacesOpen = ref(true)

  const isCreateWorkspaceModalOpen = ref(false)
  const isCreatePageModalOpen = ref(false)

  const selectedWorkspaceId = ref<Guid | null>(null)
  const selectedPageId = ref<Guid | null>(null)

  const createPageWorkspaceId = ref<Guid | null>(null)
  const createPageWorkspaceName = ref<string | null>(null)
  const createPageParentPageId = ref<Guid | null>(null)

  const openedWorkspaceIds = ref<Set<Guid>>(new Set())
  const openedPageIds = ref<Set<Guid>>(new Set())
  const loadingPageWorkspaceIds = ref<Set<Guid>>(new Set())

  const pageTreesByWorkspaceId = ref<Record<Guid, PageTreeItem[]>>({})
  const pageListErrorsByWorkspaceId = ref<Record<Guid, string | null>>({})

  const {
    workspaces,
    hasWorkspaces,
    isLoadingWorkspaces,
    workspaceListError,
    fetchMyWorkspaces,
    prependWorkspace,
    clearWorkspaceListError,
  } = useMyWorkspaces()

  const {
    profileDisplayName,
    profileSubtitle,
    profileAvatarUrl,
    profileInitial,
    isLoadingProfile,
    fetchMyProfile,
  } = useMyProfile()

  watch(
    workspaces,
    (list) => {
      if (!list.length) {
        selectedWorkspaceId.value = null
        return
      }

      const currentWorkspaceId = selectedWorkspaceId.value
      const firstWorkspace = list[0]

      if (
        firstWorkspace &&
        (!currentWorkspaceId ||
          !list.some((workspace) => workspace.id === currentWorkspaceId))
      ) {
        selectedWorkspaceId.value = firstWorkspace.id
        openWorkspaceBranch(firstWorkspace.id)
        void fetchWorkspacePages(firstWorkspace.id)
      }
    },
    { immediate: true }
  )

  function expandSidebar() {
    if (isCollapsed.value) {
      isCollapsed.value = false
    }
  }

  function collapseSidebar() {
    isCollapsed.value = true
  }

  function toggleWorkspaces() {
    if (isCollapsed.value) {
      isCollapsed.value = false
      isWorkspacesOpen.value = true
      return
    }

    isWorkspacesOpen.value = !isWorkspacesOpen.value
  }

  function openCreateWorkspaceModal() {
    if (isCollapsed.value) {
      isCollapsed.value = false
    }

    isWorkspacesOpen.value = true
    isCreateWorkspaceModalOpen.value = true
  }

  function openCreatePageModal(
    workspace: SidebarWorkspaceLike,
    parentPageId: Guid | null = null
  ) {
    if (isCollapsed.value) {
      isCollapsed.value = false
    }

    selectedWorkspaceId.value = workspace.id
    createPageWorkspaceId.value = workspace.id
    createPageWorkspaceName.value = workspace.name
    createPageParentPageId.value = parentPageId
    isCreatePageModalOpen.value = true
  }

  function isWorkspaceBranchOpen(workspaceId: Guid) {
    return openedWorkspaceIds.value.has(workspaceId)
  }

  function openWorkspaceBranch(workspaceId: Guid) {
    const next = new Set(openedWorkspaceIds.value)
    next.add(workspaceId)
    openedWorkspaceIds.value = next
  }

  function closeWorkspaceBranch(workspaceId: Guid) {
    const next = new Set(openedWorkspaceIds.value)
    next.delete(workspaceId)
    openedWorkspaceIds.value = next
  }

  function toggleWorkspaceBranch(workspace: SidebarWorkspaceLike) {
    selectedWorkspaceId.value = workspace.id

    if (isWorkspaceBranchOpen(workspace.id)) {
      closeWorkspaceBranch(workspace.id)
      return
    }

    openWorkspaceBranch(workspace.id)
    void fetchWorkspacePages(workspace.id)
  }

  function isPageBranchOpen(pageId: Guid) {
    return openedPageIds.value.has(pageId)
  }

  function openPageBranch(pageId: Guid) {
    const next = new Set(openedPageIds.value)
    next.add(pageId)
    openedPageIds.value = next
  }

  function closePageBranch(pageId: Guid) {
    const next = new Set(openedPageIds.value)
    next.delete(pageId)
    openedPageIds.value = next
  }

  function togglePageBranch(pageId: Guid) {
    if (isPageBranchOpen(pageId)) {
      closePageBranch(pageId)
      return
    }
    openPageBranch(pageId)
  }

  function isLoadingPages(workspaceId: Guid) {
    return loadingPageWorkspaceIds.value.has(workspaceId)
  }

  function getWorkspacePages(workspaceId: Guid) {
    return pageTreesByWorkspaceId.value[workspaceId] || []
  }

  function getPageListError(workspaceId: Guid) {
    return pageListErrorsByWorkspaceId.value[workspaceId] || null
  }

  async function fetchWorkspacePages(workspaceId: Guid) {
    if (loadingPageWorkspaceIds.value.has(workspaceId)) return

    const loadingSet = new Set(loadingPageWorkspaceIds.value)
    loadingSet.add(workspaceId)
    loadingPageWorkspaceIds.value = loadingSet

    pageListErrorsByWorkspaceId.value = {
      ...pageListErrorsByWorkspaceId.value,
      [workspaceId]: null,
    }

    try {
      const result = await pageController.listByWorkspace(workspaceId, {
        pageNumber: 1,
        pageSize: 100,
      })

      if (!result.isSuccess || !result.data) {
        pageListErrorsByWorkspaceId.value = {
          ...pageListErrorsByWorkspaceId.value,
          [workspaceId]: getApiResultErrorMessage(
            result,
            'Không thể tải danh sách page.'
          ),
        }

        return
      }

      pageTreesByWorkspaceId.value = {
        ...pageTreesByWorkspaceId.value,
        [workspaceId]: buildPageTree(result.data.items),
      }
    } catch (error) {
      pageListErrorsByWorkspaceId.value = {
        ...pageListErrorsByWorkspaceId.value,
        [workspaceId]: getApiErrorMessage(
          error,
          'Không thể tải danh sách page.'
        ),
      }
    } finally {
      const nextLoadingSet = new Set(loadingPageWorkspaceIds.value)
      nextLoadingSet.delete(workspaceId)
      loadingPageWorkspaceIds.value = nextLoadingSet
    }
  }

  function retryLoadPages(workspaceId: Guid) {
    void fetchWorkspacePages(workspaceId)
  }

  function selectWorkspace(id: Guid) {
    selectedWorkspaceId.value = id
  }

  function selectPage(page: PageResponse) {
    selectedPageId.value = page.id
    selectedWorkspaceId.value = page.workspaceId
  }

  function handleWorkspaceCreated(workspace: WorkspaceResponse) {
    prependWorkspace(workspace)
    isWorkspacesOpen.value = true
    selectedWorkspaceId.value = workspace.id
    openWorkspaceBranch(workspace.id)
  }

  function handlePageCreated(page: PageResponse) {
    selectedWorkspaceId.value = page.workspaceId
    selectedPageId.value = page.id
    openWorkspaceBranch(page.workspaceId)

    if (page.parentPageId) {
      openPageBranch(page.parentPageId)
    }

    const currentTree = pageTreesByWorkspaceId.value[page.workspaceId] || []

    pageTreesByWorkspaceId.value = {
      ...pageTreesByWorkspaceId.value,
      [page.workspaceId]: insertPageIntoTree(currentTree, page),
    }
  }

  function retryLoadWorkspaces() {
    clearWorkspaceListError()
    void fetchMyWorkspaces()
  }

  onMounted(() => {
    void fetchMyWorkspaces()
    void fetchMyProfile()
  })

  return {
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
    openedPageIds,
    isPageBranchOpen,
    openPageBranch,
    closePageBranch,
    togglePageBranch,
    isLoadingPages,
    getWorkspacePages,
    getPageListError,
    retryLoadPages,
    selectWorkspace,
    selectPage,
    handleWorkspaceCreated,
    handlePageCreated,
    retryLoadWorkspaces,
  }
}