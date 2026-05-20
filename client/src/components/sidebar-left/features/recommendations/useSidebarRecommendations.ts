import { computed, ref } from 'vue'
import { recommendationController } from '@/api/services/recommendation.api'
import { workspaceController } from '@/api/services/workspace.api'
import { pageController } from '@/api/services/page.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { realtimeClient } from '@/realtime/realtime.client'
import type { Guid } from '@/api/models/common.model'
import type { TaskRecommendationResponse } from '@/api/models/recommendation.model'
import type { RealtimeEnvelope } from '@/realtime/realtime.types'

export interface TaskRecommendationViewModel
  extends TaskRecommendationResponse {
  workspaceName: string
  pageName: string
  locationLabel: string
}

type GeneratedPayload =
  | TaskRecommendationResponse[]
  | {
      recommendations?: TaskRecommendationResponse[]
      Recommendations?: TaskRecommendationResponse[]
    }

const taskRecommendations = ref<TaskRecommendationViewModel[]>([])
const isLoadingTaskRecommendations = ref(false)
const isGeneratingTaskRecommendations = ref(false)
const taskRecommendationError = ref<string | null>(null)
const realtimeError = ref<string | null>(null)

const workspaceNameCache = new Map<Guid, string>()
const pageNameCache = new Map<Guid, string>()

let isRealtimeBound = false
let unbindRealtimeHandlers: Array<() => void> = []

function normalizeStatus(value: string | null | undefined) {
  return String(value ?? '').trim().toLowerCase()
}

function isPendingRecommendation(item: TaskRecommendationResponse) {
  return normalizeStatus(item.status) === 'pending'
}

function sortRecommendations(items: TaskRecommendationViewModel[]) {
  return [...items].sort((a, b) => {
    const scoreDiff = (b.score ?? 0) - (a.score ?? 0)

    if (scoreDiff !== 0) return scoreDiff

    return (
      new Date(b.createdDate).getTime() -
      new Date(a.createdDate).getTime()
    )
  })
}

function fallbackWorkspaceName(workspaceId: Guid) {
  return `Workspace ${workspaceId.slice(0, 8)}`
}

function fallbackPageName(pageId: Guid | null) {
  return pageId ? `Page ${pageId.slice(0, 8)}` : 'Không gắn page'
}

async function resolveWorkspaceName(workspaceId: Guid) {
  const cached = workspaceNameCache.get(workspaceId)
  if (cached) return cached

  try {
    const result = await workspaceController.getById(workspaceId)
    const name =
      result.isSuccess && result.data?.name
        ? result.data.name
        : fallbackWorkspaceName(workspaceId)

    workspaceNameCache.set(workspaceId, name)
    return name
  } catch {
    const name = fallbackWorkspaceName(workspaceId)
    workspaceNameCache.set(workspaceId, name)
    return name
  }
}

async function resolvePageName(pageId: Guid | null) {
  if (!pageId) return 'Không gắn page'

  const cached = pageNameCache.get(pageId)
  if (cached) return cached

  try {
    const result = await pageController.getById(pageId)
    const name =
      result.isSuccess && result.data?.title
        ? result.data.title
        : fallbackPageName(pageId)

    pageNameCache.set(pageId, name)
    return name
  } catch {
    const name = fallbackPageName(pageId)
    pageNameCache.set(pageId, name)
    return name
  }
}

async function toViewModel(
  item: TaskRecommendationResponse
): Promise<TaskRecommendationViewModel> {
  const [workspaceName, pageName] = await Promise.all([
    resolveWorkspaceName(item.workspaceId),
    resolvePageName(item.pageId),
  ])

  return {
    ...item,
    workspaceName,
    pageName,
    locationLabel: item.pageId
      ? `${workspaceName} / ${pageName}`
      : workspaceName,
  }
}

async function hydrateRecommendations(items: TaskRecommendationResponse[]) {
  const pendingItems = items.filter(isPendingRecommendation)
  const viewModels = await Promise.all(pendingItems.map(toViewModel))

  taskRecommendations.value = sortRecommendations(viewModels)
}

async function upsertRecommendation(item: TaskRecommendationResponse) {
  if (!isPendingRecommendation(item)) {
    removeRecommendation(item.id)
    return
  }

  const hydrated = await toViewModel(item)
  const index = taskRecommendations.value.findIndex((x) => x.id === item.id)

  if (index >= 0) {
    const nextItems = [...taskRecommendations.value]
    nextItems[index] = hydrated
    taskRecommendations.value = sortRecommendations(nextItems)
    return
  }

  taskRecommendations.value = sortRecommendations([
    hydrated,
    ...taskRecommendations.value,
  ])
}

function removeRecommendation(recommendationId: Guid) {
  taskRecommendations.value = taskRecommendations.value.filter(
    (item) => item.id !== recommendationId
  )
}

function getDirectRecommendationPayload(payload: unknown) {
  if (!payload || typeof payload !== 'object') return null

  const item = payload as Partial<TaskRecommendationResponse>

  return item.id && item.taskId && item.workspaceId
    ? (item as TaskRecommendationResponse)
    : null
}

function getGeneratedRecommendations(payload: unknown) {
  if (Array.isArray(payload)) {
    return payload as TaskRecommendationResponse[]
  }

  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) return []

  type GeneratedObject = {
    recommendations?: TaskRecommendationResponse[]
    Recommendations?: TaskRecommendationResponse[]
  }

  const value = payload as GeneratedObject

  if (Array.isArray(value.recommendations)) return value.recommendations
  if (Array.isArray(value.Recommendations)) return value.Recommendations

  return []
}

async function handleGenerated(
  envelope: RealtimeEnvelope<GeneratedPayload>
) {
  const items = getGeneratedRecommendations(envelope.payload)

  if (!items.length) {
    await fetchPendingRecommendations()
    return
  }

  for (const item of items) {
    await upsertRecommendation(item)
  }
}

function handleRecommendationRemoved(
  envelope: RealtimeEnvelope<TaskRecommendationResponse>
) {
  const item = getDirectRecommendationPayload(envelope.payload)

  if (item?.id) {
    removeRecommendation(item.id)
    return
  }

  void fetchPendingRecommendations()
}

async function fetchPendingRecommendations(workspaceId?: Guid | null) {
  if (isLoadingTaskRecommendations.value) return

  isLoadingTaskRecommendations.value = true
  taskRecommendationError.value = null

  try {
    const result = await recommendationController.list({
      workspaceId: workspaceId || undefined,
      status: 'pending',
      pageNumber: 1,
      pageSize: 50,
    })

    if (!result.isSuccess || !result.data) {
      taskRecommendationError.value = getApiResultErrorMessage(
        result,
        'Không thể tải gợi ý task.'
      )
      return
    }

    await hydrateRecommendations(result.data.items)
  } catch (error) {
    taskRecommendationError.value = getApiErrorMessage(
      error,
      'Không thể tải gợi ý task.'
    )
  } finally {
    isLoadingTaskRecommendations.value = false
  }
}

export function useSidebarRecommendations() {
  const hasTaskRecommendations = computed(() => {
    return taskRecommendations.value.length > 0
  })

  const pendingRecommendationCount = computed(() => {
    return taskRecommendations.value.length
  })

  async function fetchPendingRecommendations(workspaceId?: Guid | null) {
    if (isLoadingTaskRecommendations.value) return

    isLoadingTaskRecommendations.value = true
    taskRecommendationError.value = null

    try {
      const result = await recommendationController.list({
        workspaceId: workspaceId || undefined,
        status: 'pending',
        pageNumber: 1,
        pageSize: 50,
      })

      if (!result.isSuccess || !result.data) {
        taskRecommendationError.value = getApiResultErrorMessage(
          result,
          'Không thể tải gợi ý task.'
        )
        return
      }

      await hydrateRecommendations(result.data.items)
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

      for (const item of result.data) {
        await upsertRecommendation(item)
      }

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

      removeRecommendation(recommendationId)
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

      removeRecommendation(recommendationId)
      return true
    } catch (error) {
      taskRecommendationError.value = getApiErrorMessage(
        error,
        'Không thể từ chối gợi ý task.'
      )

      return false
    }
  }

  async function startRecommendationRealtime() {
    if (isRealtimeBound) return

    isRealtimeBound = true
    realtimeError.value = null

    unbindRealtimeHandlers = [
      realtimeClient.on('TaskRecommendationsGenerated', handleGenerated),
      realtimeClient.on(
        'TaskRecommendationAccepted',
        handleRecommendationRemoved
      ),
      realtimeClient.on(
        'TaskRecommendationRejected',
        handleRecommendationRemoved
      ),
      realtimeClient.on(
        'TaskRecommendationCompleted',
        handleRecommendationRemoved
      ),
    ]

    try {
      await realtimeClient.start()
    } catch (error) {
      realtimeError.value = getApiErrorMessage(
        error,
        'Không thể kết nối realtime gợi ý task.'
      )
    }
  }

  function stopRecommendationRealtime() {
    for (const unbind of unbindRealtimeHandlers) {
      unbind()
    }

    unbindRealtimeHandlers = []
    isRealtimeBound = false
  }

  return {
    taskRecommendations,
    hasTaskRecommendations,
    pendingRecommendationCount,
    isLoadingTaskRecommendations,
    isGeneratingTaskRecommendations,
    taskRecommendationError,
    realtimeError,
    fetchPendingRecommendations,
    generateRecommendations,
    acceptRecommendation,
    rejectRecommendation,
    startRecommendationRealtime,
    stopRecommendationRealtime,
  }
}