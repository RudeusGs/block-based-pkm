import { ref } from 'vue'
import { taskController } from '@/api/services/task.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type {
  CreateWorkTaskRequest,
  WorkTaskResponse,
} from '@/api/models/task.model'

export function useCreateTask() {
  const isCreatingTask = ref(false)
  const createTaskError = ref<string | null>(null)

  async function createTask(
    pageId: Guid,
    payload: CreateWorkTaskRequest
  ): Promise<WorkTaskResponse | null> {
    if (isCreatingTask.value) return null

    isCreatingTask.value = true
    createTaskError.value = null

    try {
      const result = await taskController.create(pageId, payload)

      if (!result.isSuccess || !result.data) {
        createTaskError.value = getApiResultErrorMessage(
          result,
          'Không thể tạo task.'
        )

        return null
      }

      return result.data
    } catch (error) {
      createTaskError.value = getApiErrorMessage(
        error,
        'Không thể tạo task. Vui lòng thử lại.'
      )

      return null
    } finally {
      isCreatingTask.value = false
    }
  }

  function clearCreateTaskError() {
    createTaskError.value = null
  }

  return {
    isCreatingTask,
    createTaskError,
    createTask,
    clearCreateTaskError,
  }
}
