import { ref } from 'vue'
import { workspaceController } from '@/api/services/workspace.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'

export interface DeleteWorkspaceTarget {
  id: Guid
  name: string
}

export function useDeleteWorkspace() {
  const workspaceToDelete = ref<DeleteWorkspaceTarget | null>(null)
  const isDeleteWorkspaceConfirmOpen = ref(false)
  const isDeletingWorkspace = ref(false)
  const deleteWorkspaceError = ref<string | null>(null)

  function requestDeleteWorkspace(workspace: DeleteWorkspaceTarget | null) {
    if (!workspace) return

    workspaceToDelete.value = workspace
    deleteWorkspaceError.value = null
    isDeleteWorkspaceConfirmOpen.value = true
  }

  function closeDeleteWorkspaceConfirm() {
    if (isDeletingWorkspace.value) return

    isDeleteWorkspaceConfirmOpen.value = false
    deleteWorkspaceError.value = null
    workspaceToDelete.value = null
  }

  async function confirmDeleteWorkspace() {
    const target = workspaceToDelete.value

    if (!target || isDeletingWorkspace.value) {
      return null
    }

    isDeletingWorkspace.value = true
    deleteWorkspaceError.value = null

    try {
      const result = await workspaceController.delete(target.id)

      if (!result.isSuccess) {
        deleteWorkspaceError.value = getApiResultErrorMessage(
          result,
          'Không thể xóa workspace này.'
        )

        return null
      }

      isDeleteWorkspaceConfirmOpen.value = false
      workspaceToDelete.value = null

      return target
    } catch (error) {
      deleteWorkspaceError.value = getApiErrorMessage(
        error,
        'Không thể xóa workspace này.'
      )

      return null
    } finally {
      isDeletingWorkspace.value = false
    }
  }

  return {
    workspaceToDelete,
    isDeleteWorkspaceConfirmOpen,
    isDeletingWorkspace,
    deleteWorkspaceError,
    requestDeleteWorkspace,
    closeDeleteWorkspaceConfirm,
    confirmDeleteWorkspace,
  }
}
