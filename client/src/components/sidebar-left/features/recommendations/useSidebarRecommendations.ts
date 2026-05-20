import { computed, ref } from 'vue'
import { recommendationController } from '@/api/services/recommendation.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { TaskRecommendationResponse } from '@/api/models/recommendation.model'

export function useSidebarRecommendations() {
  const taskRecommendations = ref<TaskRecommendationResponse[]>([])
  const isLoadingTaskRecommendations = ref(false)
  const isGeneratingTaskRecommendations = ref(false)
  const taskRecommendationError = ref<string | null>(null)

  const hasTaskRecommendations = computed(() => {
    return taskRecommendations.value.length > 0
  })

  const pendingRecommendationCount = computed(() => {
    return taskRecommendations.value.length
  })

  async function fetchPendingRecommendations(workspaceId: Guid | null) {
    if (!workspaceId) {
      taskRecommendations.value = []
      taskRecommendationError.value = null
      return
    }

    if (isLoadingTaskRecommendations.value) return

    isLoadingTaskRecommendations.value = true
    taskRecommendationError.value = null

    try {
      const result = await recommendationController.list({
        workspaceId,
        status: 'pending',
        pageNumber: 1,
        pageSize: 20,
      })

      if (!result.isSuccess || !result.data) {
        taskRecommendationError.value = getApiResultErrorMessage(
          result,
          'Không thể tải gợi ý task.'
        )
        return
      }

      taskRecommendations.value = result.data.items
    } catch (error) {
      taskRecommendationError.value = getApiErrorMessage(
        error,
        'Không thể tải gợi ý task.'
      )
    } finally {
      isLoadingTaskRecommendations.value = false
    }
  }

  async function generateRecommendations(
    workspaceId: Guid | null,
    pageId: Guid | null
  ) {
    if (!workspaceId || isGeneratingTaskRecommendations.value) return false

    isGeneratingTaskRecommendations.value = true
    taskRecommendationError.value = null

    try {
      const result = await recommendationController.generate(workspaceId, {
        pageId,
        force: true,
      })

      if (!result.isSuccess || !result.data) {
        taskRecommendationError.value = getApiResultErrorMessage(
          result,
          'Không thể tạo gợi ý task.'
        )
        return false
      }

      taskRecommendations.value = result.data.filter((item) => {
        return item.status.toLowerCase() === 'pending'
      })

      return true
    } catch (error) {
      taskRecommendationError.value = getApiErrorMessage(
        error,
        'Không thể tạo gợi ý task.'
      )

      return false
    } finally {
      isGeneratingTaskRecommendations.value = false
    }
  }

  async function acceptRecommendation(recommendationId: Guid) {
    try {
      const result = await recommendationController.accept(recommendationId)

      if (!result.isSuccess) {
        taskRecommendationError.value = getApiResultErrorMessage(
          result,
          'Không thể chấp nhận gợi ý task.'
        )
        return false
      }

      taskRecommendations.value = taskRecommendations.value.filter(
        (item) => item.id !== recommendationId
      )

      return true
    } catch (error) {
      taskRecommendationError.value = getApiErrorMessage(
        error,
        'Không thể chấp nhận gợi ý task.'
      )

      return false
    }
  }

  async function rejectRecommendation(recommendationId: Guid) {
    try {
      const result = await recommendationController.reject(recommendationId)

      if (!result.isSuccess) {
        taskRecommendationError.value = getApiResultErrorMessage(
          result,
          'Không thể từ chối gợi ý task.'
        )
        return false
      }

      taskRecommendations.value = taskRecommendations.value.filter(
        (item) => item.id !== recommendationId
      )

      return true
    } catch (error) {
      taskRecommendationError.value = getApiErrorMessage(
        error,
        'Không thể từ chối gợi ý task.'
      )

      return false
    }
  }

  return {
    taskRecommendations,
    hasTaskRecommendations,
    pendingRecommendationCount,
    isLoadingTaskRecommendations,
    isGeneratingTaskRecommendations,
    taskRecommendationError,
    fetchPendingRecommendations,
    generateRecommendations,
    acceptRecommendation,
    rejectRecommendation,
  }
}