import { computed, ref, watch } from 'vue'
import { pageController } from '@/api/services/page.api'
import { useMyWorkspaces } from '@/modules/workspaces/composables/useMyWorkspaces'
import { useWorkspaceNavigation } from '@/modules/navigation/composables/useWorkspaceNavigation'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'
import type {
  PageTreeItem,
  SidebarWorkspaceLike,
} from '@/components/sidebar-left/types/sidebar.types'
import {
  buildPageTree,
  findFirstPageInTree,
  insertPageIntoTree,
  removePageFromTree,
} from '@/components/sidebar-left/utils/page-tree.util'

interface FetchWorkspacePagesOptions {
  syncSelection?: boolean
  preferredPageId?: Guid | null
}

export function useSidebarWorkspaceTree() {
  const workspaceNavigation = useWorkspaceNavigation()

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
  const isRefreshingSidebarTree = ref(false)

  const pageTreesByWorkspaceId = ref<Record<Guid, PageTreeItem[]>>({})
  const pageListErrorsByWorkspaceId = ref<Record<Guid, string | null>>({})

  const pageToDelete = ref<PageTreeItem | null>(null)
  const isDeletePageConfirmOpen = ref(false)
  const isDeletingPage = ref(false)
  const deletePageError = ref<string | null>(null)

  const deletePageConfirmMessage = computed(() => {
    const page = pageToDelete.value

    if (!page) {
      return 'Bạn có chắc muốn xóa page này không?'
    }

    return `Bạn sắp xóa page "${page.title}" và toàn bộ subpage bên trong nếu có. Hành động này không thể hoàn tác.`
  })

  const {
    workspaces,
    hasWorkspaces,
    isLoadingWorkspaces,
    workspaceListError,
    fetchMyWorkspaces,
    prependWorkspace,
    removeWorkspace,
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
    selectedWorkspace,
    (workspace) => {
      if (!workspace) {
        workspaceNavigation.setWorkspace(null)
        return
      }

      workspaceNavigation.setWorkspace({
        id: workspace.id,
        name: workspace.name,
      })
    },
    { immediate: true }
  )

  watch(
    workspaces,
    (list) => {
      if (!list.length) {
        selectedWorkspaceId.value = null
        selectedPageId.value = null
        workspaceNavigation.clearNavigation()
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

        workspaceNavigation.setWorkspace({
          id: firstWorkspace.id,
          name: firstWorkspace.name,
        })

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

    workspaceNavigation.setWorkspace({
      id: workspace.id,
      name: workspace.name,
    })

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

    workspaceNavigation.setWorkspace({
      id: workspace.id,
      name: workspace.name,
    })

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

  async function fetchWorkspacePages(
    workspaceId: Guid,
    options: FetchWorkspacePagesOptions = {}
  ) {
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

      const shouldSyncSelection = options.syncSelection ?? true

      if (shouldSyncSelection) {
        const preferredPageId = options.preferredPageId ?? selectedPageId.value
        const preferredPage =
          result.data.items.find((page) => page.id === preferredPageId) ?? null

        const nextPage = preferredPage ?? result.data.items[0] ?? null

        selectedPageId.value = nextPage?.id ?? null
        workspaceNavigation.setPage(nextPage)
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

  async function refreshSidebarTree() {
    if (isRefreshingSidebarTree.value) return

    isRefreshingSidebarTree.value = true

    const previousOpenedWorkspaceIds = new Set(openedWorkspaceIds.value)
    const previousSelectedWorkspaceId = selectedWorkspaceId.value
    const previousSelectedPageId = selectedPageId.value

    try {
      clearWorkspaceListError()
      await fetchMyWorkspaces()

      const existingWorkspaceIds = new Set(
        workspaces.value.map((workspace) => workspace.id)
      )
      const firstWorkspace = workspaces.value[0] ?? null
      const nextSelectedWorkspaceId =
        previousSelectedWorkspaceId &&
        existingWorkspaceIds.has(previousSelectedWorkspaceId)
          ? previousSelectedWorkspaceId
          : firstWorkspace?.id ?? null

      const nextOpenedWorkspaceIds = new Set<Guid>()

      previousOpenedWorkspaceIds.forEach((workspaceId) => {
        if (existingWorkspaceIds.has(workspaceId)) {
          nextOpenedWorkspaceIds.add(workspaceId)
        }
      })

      if (nextSelectedWorkspaceId) {
        nextOpenedWorkspaceIds.add(nextSelectedWorkspaceId)
      }

      openedWorkspaceIds.value = nextOpenedWorkspaceIds

      if (!nextSelectedWorkspaceId) {
        selectedWorkspaceId.value = null
        selectedPageId.value = null
        workspaceNavigation.clearNavigation()
        return
      }

      selectedWorkspaceId.value = nextSelectedWorkspaceId

      const nextSelectedWorkspace =
        workspaces.value.find(
          (workspace) => workspace.id === nextSelectedWorkspaceId
        ) ?? null

      if (nextSelectedWorkspace) {
        workspaceNavigation.setWorkspace({
          id: nextSelectedWorkspace.id,
          name: nextSelectedWorkspace.name,
        })
      }

      await Promise.all(
        Array.from(nextOpenedWorkspaceIds).map((workspaceId) =>
          fetchWorkspacePages(workspaceId, {
            preferredPageId:
              workspaceId === nextSelectedWorkspaceId
                ? previousSelectedPageId
                : null,
            syncSelection: workspaceId === nextSelectedWorkspaceId,
          })
        )
      )
    } finally {
      isRefreshingSidebarTree.value = false
    }
  }

  function selectPage(page: PageResponse) {
    selectedPageId.value = page.id
    selectedWorkspaceId.value = page.workspaceId
    workspaceNavigation.setPage(page)
  }

  function handleWorkspaceCreated(workspace: WorkspaceResponse) {
    prependWorkspace(workspace)
    isWorkspacesOpen.value = true
    selectedWorkspaceId.value = workspace.id

    workspaceNavigation.setWorkspace({
      id: workspace.id,
      name: workspace.name,
    })

    workspaceNavigation.setPage(null)

    openWorkspaceBranch(workspace.id)
  }

  function handlePageCreated(page: PageResponse) {
    selectedWorkspaceId.value = page.workspaceId
    selectedPageId.value = page.id

    workspaceNavigation.setPage(page)

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

  function requestDeletePage(page: PageTreeItem) {
    pageToDelete.value = page
    deletePageError.value = null
    isDeletePageConfirmOpen.value = true
  }

  function closeDeletePageConfirm() {
    if (isDeletingPage.value) return

    isDeletePageConfirmOpen.value = false
    deletePageError.value = null
    pageToDelete.value = null
  }

  async function confirmDeletePage() {
    const target = pageToDelete.value

    if (!target || isDeletingPage.value) {
      return null
    }

    isDeletingPage.value = true
    deletePageError.value = null

    try {
      const result = await pageController.delete(target.id)

      if (!result.isSuccess) {
        deletePageError.value = getApiResultErrorMessage(
          result,
          'Không thể xóa page này.'
        )

        return null
      }

      const currentTree = pageTreesByWorkspaceId.value[target.workspaceId] || []
      const {
        tree: nextTree,
        removedIds,
        removedPage,
      } = removePageFromTree(currentTree, target.id)

      pageTreesByWorkspaceId.value = {
        ...pageTreesByWorkspaceId.value,
        [target.workspaceId]: nextTree,
      }

      const nextOpenedPageIds = new Set(openedPageIds.value)
      removedIds.forEach((pageId) => nextOpenedPageIds.delete(pageId))
      openedPageIds.value = nextOpenedPageIds

      const selectedPageWasRemoved =
        Boolean(selectedPageId.value) &&
        removedIds.includes(selectedPageId.value as Guid)

      if (selectedPageWasRemoved) {
        const nextPage = findFirstPageInTree(nextTree)

        selectedPageId.value = nextPage?.id ?? null
        workspaceNavigation.setPage(nextPage)
      }

      isDeletePageConfirmOpen.value = false
      deletePageError.value = null
      pageToDelete.value = null

      return removedPage ?? target
    } catch (error) {
      deletePageError.value = getApiErrorMessage(
        error,
        'Không thể xóa page này.'
      )

      return null
    } finally {
      isDeletingPage.value = false
    }
  }

  function handleWorkspaceDeleted(workspaceId: Guid) {
    const isSelectedWorkspace = selectedWorkspaceId.value === workspaceId

    removeWorkspace(workspaceId)

    const {
      [workspaceId]: _deletedTree,
      ...remainingPageTrees
    } = pageTreesByWorkspaceId.value

    const {
      [workspaceId]: _deletedError,
      ...remainingPageErrors
    } = pageListErrorsByWorkspaceId.value

    pageTreesByWorkspaceId.value = remainingPageTrees
    pageListErrorsByWorkspaceId.value = remainingPageErrors

    const nextOpenedWorkspaceIds = new Set(openedWorkspaceIds.value)
    nextOpenedWorkspaceIds.delete(workspaceId)
    openedWorkspaceIds.value = nextOpenedWorkspaceIds

    if (!isSelectedWorkspace) {
      return
    }

    const nextWorkspace = workspaces.value[0] ?? null

    selectedPageId.value = null

    if (!nextWorkspace) {
      selectedWorkspaceId.value = null
      workspaceNavigation.clearNavigation()
      return
    }

    selectedWorkspaceId.value = nextWorkspace.id

    workspaceNavigation.setWorkspace({
      id: nextWorkspace.id,
      name: nextWorkspace.name,
    })

    workspaceNavigation.setPage(null)
    openWorkspaceBranch(nextWorkspace.id)
    void fetchWorkspacePages(nextWorkspace.id)
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
    isRefreshingSidebarTree,
    workspaceListError,

    openedPageIds,

    pageToDelete,
    isDeletePageConfirmOpen,
    isDeletingPage,
    deletePageError,
    deletePageConfirmMessage,

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
    refreshSidebarTree,
    selectPage,
    handleWorkspaceCreated,
    handlePageCreated,
    requestDeletePage,
    closeDeletePageConfirm,
    confirmDeletePage,
    handleWorkspaceDeleted,
  }
}
