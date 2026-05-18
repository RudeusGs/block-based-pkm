import { ref } from 'vue'
import { workspaceController } from '@/api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type {
  CreateWorkspaceRequest,
  WorkspaceResponse,
} from '@/api/models/workspace.model'

export function useCreateWorkspace() {
  const isCreatingWorkspace = ref(false)
  const createWorkspaceError = ref<string | null>(null)

  async function createWorkspace(
    payload: CreateWorkspaceRequest
  ): Promise<WorkspaceResponse | null> {
    if (isCreatingWorkspace.value) return null

    isCreatingWorkspace.value = true
    createWorkspaceError.value = null

    try {
      const result = await workspaceController.create(payload)

      if (!result.isSuccess || !result.data) {
        createWorkspaceError.value = getApiResultErrorMessage(
          result,
          'Không thể tạo workspace.'
        )

        return null
      }

      return result.data
    } catch (error) {
      createWorkspaceError.value = getApiErrorMessage(
        error,
        'Không thể tạo workspace. Vui lòng thử lại.'
      )

      return null
    } finally {
      isCreatingWorkspace.value = false
    }
  }

  function clearCreateWorkspaceError() {
    createWorkspaceError.value = null
  }

  return {
    isCreatingWorkspace,
    createWorkspaceError,
    createWorkspace,
    clearCreateWorkspaceError,
  }
}