import { ref } from 'vue'
import { workspaceController } from '@/api/services/workspace.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type {
  UpdateWorkspaceRequest,
  WorkspaceResponse,
} from '@/api/models/workspace.model'

export function useUpdateWorkspace() {
  const isLoadingWorkspace = ref(false)
  const isUpdatingWorkspace = ref(false)
  const workspaceSettingsError = ref<string | null>(null)

  async function getWorkspace(workspaceId: Guid): Promise<WorkspaceResponse | null> {
    if (isLoadingWorkspace.value) return null

    isLoadingWorkspace.value = true
    workspaceSettingsError.value = null

    try {
      const result = await workspaceController.getById(workspaceId)

      if (!result.isSuccess || !result.data) {
        workspaceSettingsError.value = getApiResultErrorMessage(
          result,
          'Không thể tải không gian.'
        )
        return null
      }

      return result.data
    } catch (error) {
      workspaceSettingsError.value = getApiErrorMessage(
        error,
        'Không thể tải không gian.'
      )
      return null
    } finally {
      isLoadingWorkspace.value = false
    }
  }

  async function updateWorkspace(
    workspaceId: Guid,
    payload: UpdateWorkspaceRequest
  ): Promise<WorkspaceResponse | null> {
    if (isUpdatingWorkspace.value) return null

    isUpdatingWorkspace.value = true
    workspaceSettingsError.value = null

    try {
      const result = await workspaceController.update(workspaceId, payload)

      if (!result.isSuccess || !result.data) {
        workspaceSettingsError.value = getApiResultErrorMessage(
          result,
          'Không thể cập nhật không gian.'
        )
        return null
      }

      return result.data
    } catch (error) {
      workspaceSettingsError.value = getApiErrorMessage(
        error,
        'Không thể cập nhật không gian.'
      )
      return null
    } finally {
      isUpdatingWorkspace.value = false
    }
  }

  function clearWorkspaceSettingsError() {
    workspaceSettingsError.value = null
  }

  return {
    isLoadingWorkspace,
    isUpdatingWorkspace,
    workspaceSettingsError,
    getWorkspace,
    updateWorkspace,
    clearWorkspaceSettingsError,
  }
}
