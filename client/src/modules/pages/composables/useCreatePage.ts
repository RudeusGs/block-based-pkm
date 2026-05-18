import { ref } from 'vue'
import { pageController } from '@/api/services/page.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type {
  CreatePageRequest,
  PageResponse,
} from '@/api/models/page.model'

export function useCreatePage() {
  const isCreatingPage = ref(false)
  const createPageError = ref<string | null>(null)

  async function createPage(
    workspaceId: Guid,
    payload: CreatePageRequest
  ): Promise<PageResponse | null> {
    if (isCreatingPage.value) return null

    isCreatingPage.value = true
    createPageError.value = null

    try {
      const result = await pageController.create(workspaceId, payload)

      if (!result.isSuccess || !result.data) {
        createPageError.value = getApiResultErrorMessage(
          result,
          'Không thể tạo page.'
        )

        return null
      }

      return result.data
    } catch (error) {
      createPageError.value = getApiErrorMessage(
        error,
        'Không thể tạo page. Vui lòng thử lại.'
      )

      return null
    } finally {
      isCreatingPage.value = false
    }
  }

  function clearCreatePageError() {
    createPageError.value = null
  }

  return {
    isCreatingPage,
    createPageError,
    createPage,
    clearCreatePageError,
  }
}