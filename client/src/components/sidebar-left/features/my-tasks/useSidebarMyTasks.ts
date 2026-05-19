import { computed, ref } from 'vue'
import { meController } from '@/api/services/me.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { WorkTaskResponse } from '@/api/models/task.model'

export function useSidebarMyTasks() {
  const myTasks = ref<WorkTaskResponse[]>([])
  const myTaskTotalCount = ref(0)
  const isLoadingMyTasks = ref(false)
  const myTaskError = ref<string | null>(null)

  const openTaskCount = computed(() => {
    return myTasks.value.filter(
      (task) => task.status.toLowerCase() !== 'done'
    ).length
  })

  async function fetchMyTasks(workspaceId: Guid | null) {
    if (isLoadingMyTasks.value) return

    isLoadingMyTasks.value = true
    myTaskError.value = null

    try {
      const result = await meController.listMyAssignedTasks({
        workspaceId: workspaceId ?? null,
        includeCompleted: false,
        pageNumber: 1,
        pageSize: 8,
      })

      if (!result.isSuccess || !result.data) {
        myTaskError.value = getApiResultErrorMessage(
          result,
          'Không thể tải My Tasks.'
        )
        return
      }

      myTasks.value = result.data.items
      myTaskTotalCount.value = result.data.totalCount
    } catch (error) {
      myTaskError.value = getApiErrorMessage(
        error,
        'Không thể tải My Tasks.'
      )
    } finally {
      isLoadingMyTasks.value = false
    }
  }

  return {
    myTasks,
    myTaskTotalCount,
    openTaskCount,
    isLoadingMyTasks,
    myTaskError,
    fetchMyTasks,
  }
}