import { onBeforeUnmount, watch, type ComputedRef } from 'vue'
import { realtimeClient } from '@/realtime/realtime.client'
import type { Guid } from '@/api/models/common.model'
import type { WorkTaskResponse } from '@/api/models/task.model'
import type { TaskCommentResponse } from '@/api/models/task-comment.model'
import type { RealtimeEnvelope } from '@/realtime/realtime.types'

interface UseTaskRealtimeOptions {
  workspaceId: ComputedRef<Guid | null>
  pageId: ComputedRef<Guid | null>
  onTaskChanged: (task: WorkTaskResponse) => void
  onTaskDeleted: (taskId: Guid) => void
  onCommentChanged: (comment: TaskCommentResponse) => void
  onCommentDeleted: (comment: TaskCommentResponse) => void
  onRealtimeError?: (message: string | null) => void
}

const TASK_CHANGED_EVENTS = [
  'TaskCreated',
  'TaskUpdated',
  'TaskAssigned',
  'TaskUnassigned',
  'TaskStatusChanged',
  'TaskAssignedFromRecommendation',
  'TaskCompletedFromRecommendation',
] as const

const TASK_DELETED_EVENTS = ['TaskDeleted'] as const

const COMMENT_CHANGED_EVENTS = [
  'TaskCommentCreated',
  'TaskCommentUpdated',
  'TaskCommentRestored',
] as const

const COMMENT_DELETED_EVENTS = ['TaskCommentDeleted'] as const

type AnyRecord = Record<string, unknown>

function isRecord(value: unknown): value is AnyRecord {
  return Boolean(value) && typeof value === 'object'
}

function pick<T = unknown>(source: AnyRecord, ...keys: string[]): T | undefined {
  for (const key of keys) {
    if (key in source) return source[key] as T
  }

  return undefined
}

function stringValue(value: unknown): string {
  return typeof value === 'string' ? value : value == null ? '' : String(value)
}

function nullableStringValue(value: unknown): string | null {
  const text = stringValue(value).trim()
  return text ? text : null
}

function normalizeAssignees(raw: unknown): WorkTaskResponse['assignees'] {
  if (!Array.isArray(raw)) return []

  return raw
    .map((item) => {
      if (!isRecord(item)) return null

      const userId = nullableStringValue(pick(item, 'userId', 'UserId'))
      return userId ? { userId } : null
    })
    .filter((item): item is { userId: Guid } => Boolean(item))
}

function payloadTaskCandidate(payload: unknown): unknown {
  if (!isRecord(payload)) return payload

  return pick(payload, 'task', 'Task') ?? payload
}

function payloadCommentCandidate(payload: unknown): unknown {
  if (!isRecord(payload)) return payload

  return pick(payload, 'comment', 'Comment') ?? payload
}

function normalizeTask(payload: unknown): WorkTaskResponse | null {
  const candidate = payloadTaskCandidate(payload)
  if (!isRecord(candidate)) return null

  const id = nullableStringValue(pick(candidate, 'id', 'Id'))
  const workspaceId = nullableStringValue(pick(candidate, 'workspaceId', 'WorkspaceId'))
  const title = nullableStringValue(pick(candidate, 'title', 'Title'))
  const status = nullableStringValue(pick(candidate, 'status', 'Status'))
  const priority = nullableStringValue(pick(candidate, 'priority', 'Priority'))
  const createdById = nullableStringValue(pick(candidate, 'createdById', 'CreatedById'))

  if (!id || !workspaceId || !title || !status || !priority || !createdById) {
    return null
  }

  return {
    id,
    workspaceId,
    pageId: nullableStringValue(pick(candidate, 'pageId', 'PageId')),
    title,
    description: nullableStringValue(pick(candidate, 'description', 'Description')),
    status,
    priority,
    dueDate: nullableStringValue(pick(candidate, 'dueDate', 'DueDate')),
    createdById,
    lastModifiedById: nullableStringValue(
      pick(candidate, 'lastModifiedById', 'LastModifiedById')
    ),
    createdDate:
      nullableStringValue(pick(candidate, 'createdDate', 'CreatedDate')) ??
      new Date().toISOString(),
    updatedDate: nullableStringValue(pick(candidate, 'updatedDate', 'UpdatedDate')),
    assignees: normalizeAssignees(pick(candidate, 'assignees', 'Assignees')),
  }
}

function normalizeComment(payload: unknown): TaskCommentResponse | null {
  const candidate = payloadCommentCandidate(payload)
  if (!isRecord(candidate)) return null

  const id = nullableStringValue(pick(candidate, 'id', 'Id'))
  const taskId = nullableStringValue(pick(candidate, 'taskId', 'TaskId'))
  const userId = nullableStringValue(pick(candidate, 'userId', 'UserId'))
  const content = stringValue(pick(candidate, 'content', 'Content'))
  const createdDate = nullableStringValue(pick(candidate, 'createdDate', 'CreatedDate'))

  if (!id || !taskId || !userId || !createdDate) return null

  return {
    id,
    taskId,
    userId,
    parentId: nullableStringValue(pick(candidate, 'parentId', 'ParentId')),
    content,
    isDeleted: Boolean(pick(candidate, 'isDeleted', 'IsDeleted')),
    createdDate,
    updatedDate: nullableStringValue(pick(candidate, 'updatedDate', 'UpdatedDate')),
    deletedDate: nullableStringValue(pick(candidate, 'deletedDate', 'DeletedDate')),
  }
}

function taskIdFromEnvelope(envelope: RealtimeEnvelope, payload: unknown) {
  if (envelope.taskId) return envelope.taskId
  if (!isRecord(payload)) return null

  return nullableStringValue(pick(payload, 'taskId', 'TaskId'))
}

export function useTaskRealtime(options: UseTaskRealtimeOptions) {
  let joinedWorkspaceId: Guid | null = null
  let joinedPageId: Guid | null = null
  let connectRequestId = 0

  const unsubscribeFns = [
    ...TASK_CHANGED_EVENTS.map((eventName) =>
      realtimeClient.on(eventName, (envelope) => {
        const task = normalizeTask(envelope.payload)
        if (!task) return

        if (options.workspaceId.value && task.workspaceId !== options.workspaceId.value) {
          return
        }

        if (options.pageId.value && task.pageId && task.pageId !== options.pageId.value) {
          return
        }

        options.onTaskChanged(task)
      })
    ),

    ...TASK_DELETED_EVENTS.map((eventName) =>
      realtimeClient.on(eventName, (envelope) => {
        const taskId = taskIdFromEnvelope(envelope, envelope.payload)
        if (!taskId) return

        if (options.workspaceId.value && envelope.workspaceId && envelope.workspaceId !== options.workspaceId.value) {
          return
        }

        if (options.pageId.value && envelope.pageId && envelope.pageId !== options.pageId.value) {
          return
        }

        options.onTaskDeleted(taskId)
      })
    ),

    ...COMMENT_CHANGED_EVENTS.map((eventName) =>
      realtimeClient.on(eventName, (envelope) => {
        const comment = normalizeComment(envelope.payload)
        if (!comment) return

        if (options.workspaceId.value && envelope.workspaceId && envelope.workspaceId !== options.workspaceId.value) {
          return
        }

        if (options.pageId.value && envelope.pageId && envelope.pageId !== options.pageId.value) {
          return
        }

        options.onCommentChanged(comment)
      })
    ),

    ...COMMENT_DELETED_EVENTS.map((eventName) =>
      realtimeClient.on(eventName, (envelope) => {
        const comment = normalizeComment(envelope.payload)
        if (!comment) return

        if (options.workspaceId.value && envelope.workspaceId && envelope.workspaceId !== options.workspaceId.value) {
          return
        }

        if (options.pageId.value && envelope.pageId && envelope.pageId !== options.pageId.value) {
          return
        }

        options.onCommentDeleted(comment)
      })
    ),
  ]

  async function leaveJoinedWorkspace(targetWorkspaceId = joinedWorkspaceId) {
    if (!targetWorkspaceId) return

    try {
      await realtimeClient.leaveWorkspace(targetWorkspaceId)
    } catch (error) {
      console.warn('[TaskRealtime] leave workspace failed', error)
    } finally {
      if (joinedWorkspaceId === targetWorkspaceId) joinedWorkspaceId = null
    }
  }

  async function leaveJoinedPage(targetPageId = joinedPageId) {
    if (!targetPageId) return

    try {
      await realtimeClient.leavePage(targetPageId)
    } catch (error) {
      console.warn('[TaskRealtime] leave page failed', error)
    } finally {
      if (joinedPageId === targetPageId) joinedPageId = null
    }
  }

  async function connect(force = false) {
    const workspaceId = options.workspaceId.value
    const pageId = options.pageId.value
    const requestId = ++connectRequestId

    if (!workspaceId && !pageId) {
      await leaveJoinedPage()
      await leaveJoinedWorkspace()
      return
    }

    try {
      await realtimeClient.start()

      if (requestId !== connectRequestId) return

      options.onRealtimeError?.(null)

      if (workspaceId && (force || joinedWorkspaceId !== workspaceId)) {
        if (joinedWorkspaceId && joinedWorkspaceId !== workspaceId) {
          await leaveJoinedWorkspace(joinedWorkspaceId)
        }

        await realtimeClient.joinWorkspace(workspaceId)
        joinedWorkspaceId = workspaceId
      }

      if (pageId && (force || joinedPageId !== pageId)) {
        if (joinedPageId && joinedPageId !== pageId) {
          await leaveJoinedPage(joinedPageId)
        }

        await realtimeClient.joinPage(pageId)
        joinedPageId = pageId
      }
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Không kết nối được thời gian thực cho công việc.'
      options.onRealtimeError?.(message)
    }
  }

  watch(
    [options.workspaceId, options.pageId],
    (nextValues, previousValues) => {
      const nextWorkspaceId = nextValues[0] ?? null
      const nextPageId = nextValues[1] ?? null
      const previousWorkspaceId = previousValues?.[0] ?? null
      const previousPageId = previousValues?.[1] ?? null

      if (previousPageId && previousPageId !== nextPageId) {
        void leaveJoinedPage(previousPageId)
      }

      if (previousWorkspaceId && previousWorkspaceId !== nextWorkspaceId) {
        void leaveJoinedWorkspace(previousWorkspaceId)
      }

      void connect(true)
    },
    { immediate: true }
  )

  watch(realtimeClient.status, (status) => {
    if (status === 'connected') {
      void connect(true)
    }
  })

  onBeforeUnmount(() => {
    connectRequestId++

    for (const unsubscribe of unsubscribeFns) {
      unsubscribe()
    }

    void leaveJoinedPage()
    void leaveJoinedWorkspace()
  })

  return {
    connect,
  }
}
