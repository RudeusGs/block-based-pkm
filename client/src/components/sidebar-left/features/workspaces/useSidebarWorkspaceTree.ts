import { computed, ref, watch } from 'vue'
import { pageController } from '@/api/services/page.api'
import { useMyWorkspaces } from '@/modules/workspaces/composables/useMyWorkspaces'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'
import type { PageTreeItem, SidebarWorkspaceLike } from '@/components/sidebar-left/types/sidebar.types'
import { buildPageTree, insertPageIntoTree } from '@/components/sidebar-left/utils/page-tree.util'

export function useSidebarWorkspaceTree() {
  const isWorkspacesOpen = ref(true)

  const selectedWorkspaceId = ref<Guid | null>(null)
  const selectedPageId = ref<Guid | null>(null)

  const isCreateWorkspaceModalOpen = ref(false)
  const isCreatePageModalOpen = ref(false)

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

  const selectedWorkspace = computed(() => {
    return (
      workspaces.value.find(
        (workspace) => workspace.id === selectedWorkspaceId.value
      ) || null
    )
  })

  const selectedWorkspaceName = computed(() => {
    return selectedWorkspace.value?.name || 'No workspace'
  })

  const selectedWorkspaceRole = computed(() => {
    return selectedWorkspace.value?.currentUserRole || null
  })

  const selectedWorkspaceInitial = computed(() => {
    return selectedWorkspaceName.value.charAt(0).toUpperCase() || 'W'
  })

  watch(
    workspaces,
    (list) => {
      if (!list.length) {
        selectedWorkspaceId.value = null
        selectedPageId.value = null
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

  function toggleWorkspaces() {
    isWorkspacesOpen.value = !isWorkspacesOpen.value
  }

  function openCreateWorkspaceModal() {
    isWorkspacesOpen.value = true
    isCreateWorkspaceModalOpen.value = true
  }

  function openCreatePageModal(
    workspace: SidebarWorkspaceLike,
    parentPageId: Guid | null = null
  ) {
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

  function retryLoadWorkspaces() {
    clearWorkspaceListError()
    void fetchMyWorkspaces()
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

  return {
    isWorkspacesOpen,
    selectedWorkspaceId,
    selectedPageId,
    selectedWorkspace,
    selectedWorkspaceName,
    selectedWorkspaceRole,
    selectedWorkspaceInitial,

    isCreateWorkspaceModalOpen,
    isCreatePageModalOpen,
    createPageWorkspaceId,
    createPageWorkspaceName,
    createPageParentPageId,

    workspaces,
    hasWorkspaces,
    isLoadingWorkspaces,
    workspaceListError,

    openedPageIds,

    fetchMyWorkspaces,
    toggleWorkspaces,
    openCreateWorkspaceModal,
    openCreatePageModal,
    toggleWorkspaceBranch,
    isWorkspaceBranchOpen,
    togglePageBranch,
    isLoadingPages,
    getWorkspacePages,
    getPageListError,
    retryLoadPages,
    retryLoadWorkspaces,
    selectPage,
    handleWorkspaceCreated,
    handlePageCreated,
  }
}