<template>
  <section
    ref="sectionRef"
    class="work-tasks-section mt-5 mb-5"
  >
    <div class="task-section-toolbar d-flex flex-column flex-lg-row justify-content-between align-items-lg-end gap-3 pb-3 border-bottom border-soft mb-3">
      <div>
        <h2 class="section-title d-flex align-items-center gap-2 mb-1">
          <span class="material-symbols-outlined">table_chart</span>
          Tasks
        </h2>

        <p class="section-subtitle d-flex flex-wrap align-items-center gap-2 mb-0 small text-on-surface-variant">
          <span>{{ tasks.length }} items</span>
          <span class="tiny-separator"></span>
          <span>{{ pageId ? 'Page database' : 'Select a page' }}</span>

          <template v-if="pageId && !canManageTasks">
            <span class="tiny-separator"></span>
            <span>{{ taskPermissionLabel }}</span>
          </template>

          <template v-if="realtimeError">
            <span class="tiny-separator"></span>
            <span class="task-realtime-warning">Sync issue: {{ realtimeError }}</span>
          </template>
        </p>
      </div>

      <div class="d-flex flex-wrap align-items-center gap-2">
        <button
          class="task-action-btn btn btn-sm d-flex align-items-center gap-1"
          type="button"
          :disabled="!pageId || isLoadingTasks"
          title="Refresh tasks"
          @click="refreshTasks"
        >
          <span class="material-symbols-outlined">refresh</span>
          Sync
        </button>

        <button
          v-if="canManageTasks"
          class="new-task-btn btn btn-sm d-flex align-items-center gap-1 ms-lg-2"
          type="button"
          :disabled="!pageId"
          @click="openCreateTaskPanel"
        >
          <span class="material-symbols-outlined">add</span>
          New
        </button>
      </div>
    </div>

    <div class="task-summary d-flex flex-wrap gap-3 small fw-medium text-on-surface-variant mb-3">
      <span class="d-inline-flex align-items-center gap-2">
        <span class="summary-dot summary-todo"></span>
        {{ taskSummary.todo }} To Do
      </span>

      <span class="d-inline-flex align-items-center gap-2">
        <span class="summary-dot summary-doing"></span>
        {{ taskSummary.doing }} Doing
      </span>

      <span class="d-inline-flex align-items-center gap-2">
        <span class="summary-dot summary-done"></span>
        {{ taskSummary.done }} Done
      </span>
    </div>

    <div class="task-database rounded-3 overflow-hidden border">
      <div
        v-if="!pageId"
        class="task-empty-state"
      >
        <span class="material-symbols-outlined">select</span>
        <h3>Chọn page trước đã</h3>
        <p>Task mới sẽ được gắn với page đang mở, nên cần chọn page ở sidebar trước nha.</p>
      </div>

      <div
        v-else-if="isLoadingTasks"
        class="task-loading-state"
      >
        <span></span>
        <span></span>
        <span></span>
      </div>

      <div
        v-else-if="tasksError"
        class="task-error-state"
      >
        <i class="bi bi-exclamation-triangle"></i>
        <div>
          <strong>Không tải được task.</strong>
          <p>{{ tasksError }}</p>
        </div>

        <button
          type="button"
          @click="refreshTasks"
        >
          Thử lại
        </button>
      </div>

      <template v-else>
        <div
          v-if="tasks.length"
          class="table-responsive"
        >
          <table class="table task-table align-middle mb-0">
            <thead>
              <tr>
                <th class="fw-normal ps-4">Task</th>
                <th class="fw-normal">Status</th>
                <th class="fw-normal">Priority</th>
                <th class="fw-normal">Due Date</th>
                <th class="fw-normal">Assignees</th>
              </tr>
            </thead>

            <tbody>
              <tr
                v-for="task in tasks"
                :key="task.id"
                class="task-row"
                :class="{
                  'task-completed': task.status === 'done',
                  'task-row-selected': selectedTask?.id === task.id,
                }"
                tabindex="0"
                @click="openTaskDetail(task.id)"
                @keydown.enter="openTaskDetail(task.id)"
              >
                <td class="ps-4 task-name-cell">
                  <div class="d-flex align-items-center gap-2 fw-semibold text-on-surface">
                    <span class="material-symbols-outlined drag-row-icon">
                      drag_indicator
                    </span>

                    <span :class="{ 'text-decoration-line-through': task.status === 'done' }">
                      {{ task.title }}
                    </span>
                  </div>

                  <div class="task-description small mt-1 ms-4 text-truncate">
                    {{ task.description || 'No description' }}
                  </div>
                </td>

                <td>
                  <span
                    class="status-pill d-inline-flex align-items-center gap-2 rounded-pill px-2 py-1"
                    :class="statusClass(task.status)"
                  >
                    <span class="status-pill-dot"></span>
                    {{ statusLabel(task.status) }}
                  </span>
                </td>

                <td>
                  <span
                    class="priority-pill d-inline-flex align-items-center gap-2 rounded-pill px-2 py-1"
                    :class="priorityClass(task.priority)"
                  >
                    <span class="priority-pill-dot"></span>
                    {{ priorityLabel(task.priority) }}
                  </span>
                </td>

                <td>
                  <span
                    class="task-date small"
                    :class="{ 'date-overdue': task.overdue }"
                  >
                    {{ task.dueDateLabel }}
                  </span>
                </td>

                <td>
                  <div
                    v-if="task.assignees.length"
                    class="task-assignee-group"
                  >
                    <div class="task-avatar-stack">
                      <img
                        v-for="assignee in task.assignees.slice(0, 3)"
                        :key="assignee.userId"
                        class="avatar rounded-circle"
                        :class="{ 'avatar-muted': task.status === 'done' }"
                        :src="assignee.avatarUrl"
                        :alt="assignee.name"
                        :title="assignee.name"
                      />
                    </div>

                    <span>{{ assigneeSummary(task) }}</span>
                  </div>

                  <span
                    v-else
                    class="task-unassigned-label"
                  >
                    Unassigned
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div
          v-else
          class="task-empty-state"
        >
          <span class="material-symbols-outlined">playlist_add_check</span>
          <h3>Page này chưa có task</h3>
          <p>{{ emptyTaskDescription }}</p>
        </div>

        <button
          v-if="canManageTasks"
          class="add-row-btn btn w-100 text-start rounded-0 d-flex align-items-center gap-2 px-4 py-2"
          type="button"
          :disabled="!pageId"
          @click="openCreateTaskPanel"
        >
          <span class="material-symbols-outlined">add</span>
          Add a row
        </button>
      </template>
    </div>

    <CreateTaskPanel
      v-if="canManageTasks"
      v-model="isCreateTaskPanelOpen"
      :workspace-id="workspaceId"
      :page-id="pageId"
      :workspace-name="workspaceName"
      :page-title="pageTitle"
      :members="assignableMembers"
      :can-manage-assignees="canManageTaskAssignees"
      :is-loading-members="isLoadingMembers"
      :members-error="membersError"
      @created="handleTaskCreated"
      @refresh-members="refreshMembers"
    />

    <TaskDetailDrawer
      :open="isTaskDetailOpen"
      :task="selectedTask"
      :comments="selectedTaskComments"
      :members="members"
      :can-manage-assignees="canManageTaskAssignees"
      :can-change-status="selectedTaskCanChangeStatus"
      :page-title="pageTitle"
      :is-loading-comments="isLoadingComments"
      :comments-error="commentsError"
      :is-adding-comment="isAddingComment"
      :is-mutating-task="isMutatingTask"
      :task-action-error="taskActionError"
      @close="closeTaskDetail"
      @add-comment="addTaskComment"
      @add-reply="addTaskComment"
      @assign-member="assignTaskMember"
      @unassign-member="unassignTaskMember"
      @change-status="changeTaskStatus"
    />
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { taskController } from '@/api/services/task.api'
import { taskCommentController } from '@/api/services/task-comment.api'
import { workspaceController } from '@/api/services/workspace.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { useTaskRealtime } from '@/modules/task/composables/useTaskRealtime'
import CreateTaskPanel from './CreateTaskPanel.vue'
import TaskDetailDrawer from './TaskDetailDrawer.vue'
import type { Guid } from '@/api/models/common.model'
import type { TaskCommentResponse } from '@/api/models/task-comment.model'
import type {
  WorkTaskPriorityRequest,
  WorkTaskResponse,
  WorkTaskStatusRequest,
} from '@/api/models/task.model'
import type { WorkspaceMemberResponse } from '@/api/models/workspace.model'
import type {
  CreateTaskCreatedPayload,
  NormalizedTaskPriority,
  NormalizedTaskStatus,
  TaskAssigneeView,
  TaskCommentView,
  TaskMemberOption,
  WorkTaskView,
} from './task.types'
import './css/WorkTasksSection.css'

const props = defineProps<{
  workspaceId: Guid | null
  pageId: Guid | null
  workspaceName?: string | null
  pageTitle?: string | null
}>()

const sectionRef = ref<HTMLElement | null>(null)
const rawTasks = ref<WorkTaskResponse[]>([])
const isLoadingTasks = ref(false)
const tasksError = ref<string | null>(null)
const members = ref<TaskMemberOption[]>([])
const isLoadingMembers = ref(false)
const membersError = ref<string | null>(null)
const realtimeError = ref<string | null>(null)
const selectedTaskId = ref<Guid | null>(null)
const isTaskDetailOpen = ref(false)
const isCreateTaskPanelOpen = ref(false)
const rawCommentsByTaskId = ref<Record<Guid, TaskCommentResponse[]>>({})
const loadedCommentTaskIds = ref<Set<Guid>>(new Set())
const isLoadingComments = ref(false)
const commentsError = ref<string | null>(null)
const isAddingComment = ref(false)
const isMutatingTask = ref(false)
const taskActionError = ref<string | null>(null)

let taskRequestId = 0
let memberRequestId = 0
let commentRequestId = 0

const workspaceIdRef = computed(() => props.workspaceId)
const pageIdRef = computed(() => props.pageId)

const memberById = computed(() => {
  return new Map(members.value.map((member) => [member.userId, member]))
})

const currentWorkspaceMember = computed(() => {
  return members.value.find((member) => member.isCurrentUser) ?? null
})

const canManageTaskAssignees = computed(() => {
  const currentMember = currentWorkspaceMember.value
  if (!currentMember) return false

  const role = currentMember.role.trim().toLowerCase()

  return currentMember.isOwner || role === 'owner' || role === 'manager'
})

const canManageTasks = computed(() => canManageTaskAssignees.value)

const selectedTaskCanChangeStatus = computed(() => {
  return canChangeStatusForRawTask(selectedRawTask.value)
})

const taskPermissionLabel = computed(() => {
  if (isLoadingMembers.value) return 'Checking permissions...'
  if (membersError.value) return 'Permission unknown'

  return canManageTasks.value ? 'Can edit' : 'View only'
})

const emptyTaskDescription = computed(() => {
  if (canManageTasks.value) {
    return 'Bấm New để tạo task đầu tiên, assign nhiều người ngay nếu cần.'
  }

  return 'Bạn có thể xem task và comment. Việc tạo task mới chỉ dành cho owner hoặc manager.'
})

const assignableMembers = computed(() => {
  if (!canManageTaskAssignees.value) return []

  return members.value.filter((member) => !member.isCurrentUser)
})

const tasks = computed(() => {
  return rawTasks.value.map(mapTaskResponse).sort(compareTaskViews)
})

const selectedRawTask = computed(() => {
  if (!selectedTaskId.value) return null

  return rawTasks.value.find((task) => task.id === selectedTaskId.value) ?? null
})

const selectedTask = computed(() => {
  if (!selectedTaskId.value) return null

  return tasks.value.find((task) => task.id === selectedTaskId.value) ?? null
})

const selectedTaskComments = computed(() => {
  if (!selectedTaskId.value) return []

  return mapCommentsForTask(selectedTaskId.value)
})

const taskSummary = computed(() => {
  return tasks.value.reduce(
    (summary, task) => {
      summary[task.status] += 1
      return summary
    },
    {
      todo: 0,
      doing: 0,
      done: 0,
    }
  )
})

useTaskRealtime({
  workspaceId: workspaceIdRef,
  pageId: pageIdRef,
  onTaskChanged: upsertTask,
  onTaskDeleted: removeTask,
  onCommentChanged: upsertComment,
  onCommentDeleted: removeComment,
  onRealtimeError: (message) => {
    realtimeError.value = message
  },
})

watch(
  () => props.workspaceId,
  (workspaceId) => {
    members.value = []
    membersError.value = null

    if (workspaceId) {
      void fetchMembers(workspaceId)
    }
  },
  { immediate: true }
)

watch(
  canManageTasks,
  (canManage) => {
    if (!canManage) {
      isCreateTaskPanelOpen.value = false
    }
  }
)

watch(
  () => props.pageId,
  (pageId) => {
    selectedTaskId.value = null
    isTaskDetailOpen.value = false
    isCreateTaskPanelOpen.value = false
    rawTasks.value = []
    tasksError.value = null
    commentsError.value = null
    taskActionError.value = null

    if (pageId) {
      void fetchTasks(pageId)
    }
  },
  { immediate: true }
)

watch(
  [selectedTaskId, isTaskDetailOpen],
  ([taskId, isOpen]) => {
    if (!taskId || !isOpen) return

    if (!loadedCommentTaskIds.value.has(taskId)) {
      void fetchTaskComments(taskId)
    }
  }
)

function scrollIntoView(options?: ScrollIntoViewOptions) {
  sectionRef.value?.scrollIntoView(
    options ?? {
      behavior: 'smooth',
      block: 'start',
    }
  )
}

defineExpose({
  scrollIntoView,
})

function memberDisplayName(member: WorkspaceMemberResponse) {
  return member.fullName?.trim() || member.userName?.trim() || member.email
}

function memberInitials(name: string) {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')

  return initials || '?'
}

function avatarUrlForName(name: string) {
  const safeName = encodeURIComponent(name.trim() || 'User')

  return `https://ui-avatars.com/api/?name=${safeName}&background=242424&color=ededed&bold=true`
}

function mapMember(member: WorkspaceMemberResponse): TaskMemberOption {
  const displayName = memberDisplayName(member)

  return {
    userId: member.userId,
    displayName,
    email: member.email,
    role: member.role,
    avatarUrl: member.avatarUrl || avatarUrlForName(displayName),
    initials: memberInitials(displayName),
    isCurrentUser: member.isCurrentUser,
    isOwner: member.isOwner,
  }
}

async function fetchMembers(targetWorkspaceId = props.workspaceId) {
  if (!targetWorkspaceId) return

  const currentRequestId = ++memberRequestId

  isLoadingMembers.value = true
  membersError.value = null

  try {
    const result = await workspaceController.listMembers(targetWorkspaceId)

    if (currentRequestId !== memberRequestId) return

    if (!result.isSuccess || !result.data) {
      membersError.value = getApiResultErrorMessage(
        result,
        'Không thể tải danh sách member.'
      )
      members.value = []
      return
    }

    members.value = result.data
      .map(mapMember)
      .sort((firstMember, secondMember) => {
        if (firstMember.isCurrentUser !== secondMember.isCurrentUser) {
          return firstMember.isCurrentUser ? -1 : 1
        }

        return firstMember.displayName.localeCompare(secondMember.displayName)
      })
  } catch (error) {
    if (currentRequestId !== memberRequestId) return

    membersError.value = getApiErrorMessage(
      error,
      'Không thể tải danh sách member.'
    )
    members.value = []
  } finally {
    if (currentRequestId === memberRequestId) {
      isLoadingMembers.value = false
    }
  }
}

async function fetchTasks(targetPageId = props.pageId) {
  if (!targetPageId) return

  const currentRequestId = ++taskRequestId

  isLoadingTasks.value = true
  tasksError.value = null

  try {
    const result = await taskController.listByPage(targetPageId, {
      pageNumber: 1,
      pageSize: 100,
      includeCompleted: true,
    })

    if (currentRequestId !== taskRequestId) return

    if (!result.isSuccess || !result.data) {
      tasksError.value = getApiResultErrorMessage(
        result,
        'Không thể tải danh sách task.'
      )
      rawTasks.value = []
      return
    }

    rawTasks.value = result.data.items
  } catch (error) {
    if (currentRequestId !== taskRequestId) return

    tasksError.value = getApiErrorMessage(
      error,
      'Không thể tải danh sách task.'
    )
    rawTasks.value = []
  } finally {
    if (currentRequestId === taskRequestId) {
      isLoadingTasks.value = false
    }
  }
}

async function fetchTaskComments(taskId: Guid) {
  const currentRequestId = ++commentRequestId

  isLoadingComments.value = true
  commentsError.value = null

  try {
    const result = await taskCommentController.listByTask(taskId, {
      pageNumber: 1,
      pageSize: 100,
      includeDeleted: false,
    })

    if (currentRequestId !== commentRequestId) return

    if (!result.isSuccess || !result.data) {
      commentsError.value = getApiResultErrorMessage(
        result,
        'Không tải được comment của task.'
      )
      return
    }

    rawCommentsByTaskId.value = {
      ...rawCommentsByTaskId.value,
      [taskId]: result.data.items,
    }

    const nextLoadedTaskIds = new Set(loadedCommentTaskIds.value)
    nextLoadedTaskIds.add(taskId)
    loadedCommentTaskIds.value = nextLoadedTaskIds
  } catch (error) {
    if (currentRequestId !== commentRequestId) return

    commentsError.value = getApiErrorMessage(
      error,
      'Không tải được comment của task.'
    )
  } finally {
    if (currentRequestId === commentRequestId) {
      isLoadingComments.value = false
    }
  }
}

function refreshTasks() {
  void fetchTasks()
}

function refreshMembers() {
  if (!props.workspaceId) return

  void fetchMembers(props.workspaceId)
}

function normalizeStatus(status: string): NormalizedTaskStatus {
  const normalized = status.trim().toLowerCase().replace(/[\s_-]+/g, '')

  if (normalized === 'doing') return 'doing'
  if (normalized === 'done') return 'done'

  return 'todo'
}

function normalizePriority(priority: string): WorkTaskPriorityRequest {
  const normalized = priority.trim().toLowerCase()

  if (normalized === 'high' || normalized === 'medium') return normalized

  return 'low'
}

function formatDueDate(value: string | null) {
  if (!value) return 'No due date'

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}

function formatRelativeTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value

  const diffMs = Date.now() - date.getTime()
  const diffMinutes = Math.max(0, Math.floor(diffMs / 60_000))

  if (diffMinutes < 1) return 'Vừa xong'
  if (diffMinutes < 60) return `${diffMinutes}m ago`

  const diffHours = Math.floor(diffMinutes / 60)
  if (diffHours < 24) return `${diffHours}h ago`

  const diffDays = Math.floor(diffHours / 24)
  if (diffDays < 7) return `${diffDays}d ago`

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}

function isOverdue(value: string | null, status: string) {
  if (!value || normalizeStatus(status) === 'done') return false

  const dueDate = new Date(value)

  if (Number.isNaN(dueDate.getTime())) return false

  return dueDate.getTime() < Date.now()
}

function memberFallback(userId: Guid): TaskMemberOption {
  const displayName = `User ${userId.slice(0, 8)}`

  const fallbackInitials = displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('') || 'U'

  return {
    userId,
    displayName,
    email: '',
    role: 'member',
    avatarUrl: '',
    initials: fallbackInitials,
    isCurrentUser: false,
    isOwner: false,
  }
}

function assigneeViewFromUserId(userId: Guid): TaskAssigneeView {
  const member = memberById.value.get(userId) ?? memberFallback(userId)

  return {
    userId: member.userId,
    name: member.displayName,
    avatarUrl: member.avatarUrl,
    initials: member.initials,
    role: member.role,
  }
}

function mapTaskResponse(task: WorkTaskResponse): WorkTaskView {
  const uniqueAssigneeIds = Array.from(
    new Set((task.assignees ?? []).map((assignee) => assignee.userId))
  )

  return {
    id: task.id,
    workspaceId: task.workspaceId,
    pageId: task.pageId,
    title: task.title,
    description: task.description ?? '',
    status: normalizeStatus(task.status),
    priority: normalizePriority(task.priority),
    dueDateRaw: task.dueDate,
    dueDateLabel: formatDueDate(task.dueDate),
    overdue: isOverdue(task.dueDate, task.status),
    createdById: task.createdById,
    updatedDate: task.updatedDate,
    assignees: uniqueAssigneeIds.map(assigneeViewFromUserId),
  }
}

function priorityWeight(priority: NormalizedTaskPriority) {
  return {
    high: 3,
    medium: 2,
    low: 1,
  }[priority]
}

function statusWeight(status: NormalizedTaskStatus) {
  return {
    doing: 3,
    todo: 2,
    done: 1,
  }[status]
}

function compareTaskViews(left: WorkTaskView, right: WorkTaskView) {
  if (left.status !== right.status) {
    return statusWeight(right.status) - statusWeight(left.status)
  }

  if (left.priority !== right.priority) {
    return priorityWeight(right.priority) - priorityWeight(left.priority)
  }

  const leftTime = left.dueDateRaw ? new Date(left.dueDateRaw).getTime() : Number.MAX_SAFE_INTEGER
  const rightTime = right.dueDateRaw ? new Date(right.dueDateRaw).getTime() : Number.MAX_SAFE_INTEGER

  return leftTime - rightTime
}

function openCreateTaskPanel() {
  if (!props.pageId || !canManageTasks.value) return

  isCreateTaskPanelOpen.value = true
}

function upsertTask(task: WorkTaskResponse) {
  if (props.pageId && task.pageId && task.pageId !== props.pageId) return

  const existingIndex = rawTasks.value.findIndex((item) => item.id === task.id)

  if (existingIndex === -1) {
    rawTasks.value = [task, ...rawTasks.value]
    return
  }

  rawTasks.value = rawTasks.value.map((item) => (item.id === task.id ? task : item))
}

function removeTask(taskId: Guid) {
  rawTasks.value = rawTasks.value.filter((task) => task.id !== taskId)

  if (selectedTaskId.value === taskId) {
    closeTaskDetail()
    selectedTaskId.value = null
  }
}

function handleTaskCreated(payload: CreateTaskCreatedPayload) {
  const createdTask =
    payload.assignees.length && !payload.task.assignees?.length
      ? {
          ...payload.task,
          assignees: payload.assignees.map((assignee) => ({
            userId: assignee.userId,
          })),
        }
      : payload.task

  upsertTask(createdTask)
  selectedTaskId.value = createdTask.id
  isTaskDetailOpen.value = true
}

function openTaskDetail(taskId: Guid) {
  selectedTaskId.value = taskId
  isTaskDetailOpen.value = true
  commentsError.value = null
  taskActionError.value = null
}

function closeTaskDetail() {
  isTaskDetailOpen.value = false
}

function upsertComment(comment: TaskCommentResponse) {
  const list = rawCommentsByTaskId.value[comment.taskId] ?? []
  const existingIndex = list.findIndex((item) => item.id === comment.id)
  const nextList =
    existingIndex === -1
      ? [...list, comment]
      : list.map((item) => (item.id === comment.id ? comment : item))

  rawCommentsByTaskId.value = {
    ...rawCommentsByTaskId.value,
    [comment.taskId]: nextList.sort(compareComments),
  }
}

function removeComment(comment: TaskCommentResponse) {
  const list = rawCommentsByTaskId.value[comment.taskId] ?? []

  rawCommentsByTaskId.value = {
    ...rawCommentsByTaskId.value,
    [comment.taskId]: list.filter((item) => item.id !== comment.id),
  }
}

function compareComments(left: TaskCommentResponse, right: TaskCommentResponse) {
  return new Date(left.createdDate).getTime() - new Date(right.createdDate).getTime()
}

function mapCommentAuthor(comment: TaskCommentResponse) {
  const member = memberById.value.get(comment.userId) ?? memberFallback(comment.userId)

  return {
    userId: member.userId,
    name: member.isCurrentUser ? 'Bạn' : member.displayName,
    role: member.isCurrentUser ? 'You' : member.role,
    avatarUrl: member.avatarUrl,
    initials: member.initials,
  }
}

function mapCommentView(comment: TaskCommentResponse): TaskCommentView {
  return {
    id: comment.id,
    taskId: comment.taskId,
    parentId: comment.parentId,
    author: mapCommentAuthor(comment),
    content: comment.content,
    createdAt: formatRelativeTime(comment.createdDate),
    isDeleted: comment.isDeleted,
    replies: [],
  }
}

function sortCommentTree(comments: TaskCommentView[]) {
  for (const comment of comments) {
    comment.replies = sortCommentTree(comment.replies)
  }

  return comments
}

function mapCommentsForTask(taskId: Guid) {
  const rawComments = (rawCommentsByTaskId.value[taskId] ?? [])
    .filter((comment) => !comment.isDeleted)
    .sort(compareComments)

  const topLevelComments: TaskCommentView[] = []
  const commentsById = new Map<Guid, TaskCommentView>()

  for (const comment of rawComments) {
    commentsById.set(comment.id, mapCommentView(comment))
  }

  for (const comment of rawComments) {
    const view = commentsById.get(comment.id)
    if (!view) continue

    const parent = comment.parentId ? commentsById.get(comment.parentId) : null

    if (parent) {
      parent.replies.push(view)
      continue
    }

    topLevelComments.push(view)
  }

  return sortCommentTree(topLevelComments)
}

async function addTaskComment(content: string, parentId: Guid | null = null) {
  if (!selectedTask.value || !content.trim() || isAddingComment.value) return

  const taskId = selectedTask.value.id
  isAddingComment.value = true
  commentsError.value = null

  try {
    const result = await taskCommentController.create(taskId, {
      content: content.trim(),
      parentId,
    })

    if (!result.isSuccess || !result.data) {
      commentsError.value = getApiResultErrorMessage(
        result,
        'Không gửi được comment.'
      )
      return
    }

    upsertComment(result.data)
  } catch (error) {
    commentsError.value = getApiErrorMessage(error, 'Không gửi được comment.')
  } finally {
    isAddingComment.value = false
  }
}

async function assignTaskMember(userId: Guid) {
  if (
    !selectedRawTask.value ||
    isMutatingTask.value ||
    !canManageTaskAssignees.value
  ) {
    return
  }

  isMutatingTask.value = true
  taskActionError.value = null

  try {
    const result = await taskController.assign(selectedRawTask.value.id, { userId })

    if (!result.isSuccess || !result.data) {
      taskActionError.value = getApiResultErrorMessage(
        result,
        'Không assign được member.'
      )
      return
    }

    upsertTask(result.data)
  } catch (error) {
    taskActionError.value = getApiErrorMessage(error, 'Không assign được member.')
  } finally {
    isMutatingTask.value = false
  }
}

async function unassignTaskMember(userId: Guid) {
  if (
    !selectedRawTask.value ||
    isMutatingTask.value ||
    !canManageTaskAssignees.value
  ) {
    return
  }

  isMutatingTask.value = true
  taskActionError.value = null

  try {
    const result = await taskController.unassign(selectedRawTask.value.id, userId)

    if (!result.isSuccess || !result.data) {
      taskActionError.value = getApiResultErrorMessage(
        result,
        'Không gỡ assignee được.'
      )
      return
    }

    upsertTask(result.data)
  } catch (error) {
    taskActionError.value = getApiErrorMessage(error, 'Không gỡ assignee được.')
  } finally {
    isMutatingTask.value = false
  }
}

function isCurrentUserAssignedToTask(task: WorkTaskResponse) {
  const currentUserId = currentWorkspaceMember.value?.userId
  if (!currentUserId) return false

  return (task.assignees ?? []).some((assignee) => assignee.userId === currentUserId)
}

function canChangeStatusForRawTask(task: WorkTaskResponse | null) {
  if (!task) return false
  if (normalizeStatus(task.status) === 'done') return false

  return canManageTasks.value || isCurrentUserAssignedToTask(task)
}

async function changeTaskStatus(status: WorkTaskStatusRequest) {
  const task = selectedRawTask.value

  if (!task || isMutatingTask.value) return

  if (normalizeStatus(task.status) === 'done') {
    taskActionError.value = 'Task đã Done và trạng thái đã được khóa.'
    return
  }

  if (normalizeStatus(task.status) === status) return

  if (!canChangeStatusForRawTask(task)) {
    taskActionError.value =
      'Bạn cần được assign vào task này hoặc có quyền owner/manager để đổi trạng thái.'
    return
  }

  isMutatingTask.value = true
  taskActionError.value = null

  try {
    const result = await taskController.changeStatus(task.id, {
      status,
    })

    if (!result.isSuccess || !result.data) {
      taskActionError.value = getApiResultErrorMessage(
        result,
        'Không đổi trạng thái task được.'
      )
      return
    }

    upsertTask(result.data)
  } catch (error) {
    taskActionError.value = getApiErrorMessage(
      error,
      'Không đổi trạng thái task được.'
    )
  } finally {
    isMutatingTask.value = false
  }
}

function statusLabel(status: NormalizedTaskStatus) {
  return {
    todo: 'To Do',
    doing: 'Doing',
    done: 'Done',
  }[status]
}

function statusClass(status: NormalizedTaskStatus) {
  return {
    todo: 'status-todo',
    doing: 'status-doing',
    done: 'status-done',
  }[status]
}

function priorityLabel(priority: NormalizedTaskPriority) {
  return {
    low: 'Low',
    medium: 'Medium',
    high: 'High',
  }[priority]
}

function priorityClass(priority: NormalizedTaskPriority) {
  return {
    low: 'priority-low',
    medium: 'priority-medium',
    high: 'priority-high',
  }[priority]
}

function assigneeSummary(task: WorkTaskView) {
  const firstAssignee = task.assignees[0]

  if (!firstAssignee) return 'Unassigned'
  if (task.assignees.length === 1) return firstAssignee.name

  return `${firstAssignee.name} +${task.assignees.length - 1}`
}
</script>
