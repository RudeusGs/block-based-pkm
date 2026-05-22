import { computed, ref } from 'vue'
import { meController } from '@/api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type {
  WorkspaceListItemResponse,
  WorkspaceResponse,
} from '@/api/models/workspace.model'

export interface WorkspaceSidebarItem {
  id: string
  name: string
  description: string | null
  ownerId: string
  currentUserRole: string | null
}

function mapListItemToSidebarItem(
  item: WorkspaceListItemResponse
): WorkspaceSidebarItem {
  return {
    id: item.id,
    name: item.name,
    description: item.description,
    ownerId: item.ownerId,
    currentUserRole: item.currentUserRole,
  }
}

function mapWorkspaceToSidebarItem(
  workspace: WorkspaceResponse
): WorkspaceSidebarItem {
  return {
    id: workspace.id,
    name: workspace.name,
    description: workspace.description,
    ownerId: workspace.ownerId,
    currentUserRole: workspace.currentUserRole,
  }
}

export function useMyWorkspaces() {
  const workspaces = ref<WorkspaceSidebarItem[]>([])
  const isLoadingWorkspaces = ref(false)
  const workspaceListError = ref<string | null>(null)

  const hasWorkspaces = computed(() => workspaces.value.length > 0)

  async function fetchMyWorkspaces() {
    if (isLoadingWorkspaces.value) return

    isLoadingWorkspaces.value = true
    workspaceListError.value = null

    try {
      const result = await meController.listMyWorkspaces({
        pageNumber: 1,
        pageSize: 50,
      })

      if (!result.isSuccess || !result.data) {
        workspaceListError.value = getApiResultErrorMessage(
          result,
          'Không thể tải danh sách workspace.'
        )

        return
      }

      workspaces.value = result.data.items.map(mapListItemToSidebarItem)
    } catch (error) {
      workspaceListError.value = getApiErrorMessage(
        error,
        'Không thể tải danh sách workspace.'
      )
    } finally {
      isLoadingWorkspaces.value = false
    }
  }

  function prependWorkspace(workspace: WorkspaceResponse) {
    const item = mapWorkspaceToSidebarItem(workspace)

    const existed = workspaces.value.some(
      (workspaceItem) => workspaceItem.id === item.id
    )

    if (existed) return

    workspaces.value = [item, ...workspaces.value]
  }

  function removeWorkspace(workspaceId: string) {
    workspaces.value = workspaces.value.filter(
      (workspace) => workspace.id !== workspaceId
    )
  }

  function clearWorkspaceListError() {
    workspaceListError.value = null
  }

  return {
    workspaces,
    hasWorkspaces,
    isLoadingWorkspaces,
    workspaceListError,
    fetchMyWorkspaces,
    prependWorkspace,
    removeWorkspace,
    clearWorkspaceListError,
  }
}


