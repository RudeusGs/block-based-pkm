import { computed, readonly, ref, watch, type ComputedRef, type Ref } from 'vue'
import { meController } from '@/api/services/me.api'
import { taskController } from '@/api/services/task.api'
import { getApiErrorMessage, getApiResultErrorMessage } from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { WorkTaskResponse } from '@/api/models/task.model'

export type AiReminderSeverity = 'critical' | 'warning' | 'info' | 'gentle'

export type AiReminderCategory =
  | 'overdue'
  | 'due-soon'
  | 'due-today'
  | 'due-week'
  | 'high-priority'
  | 'in-progress'
  | 'unscheduled'

export interface AiTaskReminder {
  id: string
  taskId: Guid
  workspaceId: Guid
  pageId: Guid | null
  title: string
  description: string | null
  status: string
  priority: string
  dueDate: string | null
  category: AiReminderCategory
  severity: AiReminderSeverity
  icon: string
  headline: string
  message: string
  actionText: string
  score: number
  timeLabel: string
  source: 'assigned' | 'workspace'
}

export interface AiReminderSummary {
  total: number
  critical: number
  warning: number
  overdue: number
  dueToday: number
  dueSoon: number
  highPriority: number
  unscheduled: number
  workspaceTaskCount: number
  assignedTaskCount: number
}

type WorkspaceIdRef = Ref<Guid | null> | ComputedRef<Guid | null>

const MAX_PAGE_SIZE = 100
const AUTO_REFRESH_INTERVAL_MS = 60_000

function normalizeStatus(status: string | null | undefined) {
  return (status ?? '').trim().toLowerCase()
}

function normalizePriority(priority: string | null | undefined) {
  return (priority ?? '').trim().toLowerCase()
}

function isDone(task: WorkTaskResponse) {
  return normalizeStatus(task.status) === 'done'
}

function isDoing(task: WorkTaskResponse) {
  return normalizeStatus(task.status) === 'doing'
}

function isHighPriority(task: WorkTaskResponse) {
  return normalizePriority(task.priority) === 'high'
}

function safeDate(value: string | null | undefined) {
  if (!value) return null

  const date = new Date(value)

  return Number.isNaN(date.getTime()) ? null : date
}

function diffHours(target: Date, now: Date) {
  return (target.getTime() - now.getTime()) / 3_600_000
}

function daysBetween(left: Date, right: Date) {
  return Math.floor((left.getTime() - right.getTime()) / 86_400_000)
}

function formatDueDistance(dueDate: Date | null, now: Date) {
  if (!dueDate) return 'No due date'

  const hours = diffHours(dueDate, now)
  const absHours = Math.abs(hours)

  if (hours < 0) {
    if (absHours < 1) return 'Vừa quá hạn'
    if (absHours < 24) return `Quá hạn ${Math.ceil(absHours)} giờ`

    return `Quá hạn ${Math.ceil(absHours / 24)} ngày`
  }

  if (hours < 1) return 'Còn dưới 1 giờ'
  if (hours < 24) return `Còn ${Math.ceil(hours)} giờ`

  return `Còn ${Math.ceil(hours / 24)} ngày`
}

function formatDisplayDate(value: string | null) {
  const date = safeDate(value)

  if (!date) return 'chưa có hạn'

  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function buildReminderId(task: WorkTaskResponse, category: AiReminderCategory) {
  return `${category}:${task.id}`
}

function createReminder(
  task: WorkTaskResponse,
  category: AiReminderCategory,
  severity: AiReminderSeverity,
  icon: string,
  headline: string,
  message: string,
  actionText: string,
  score: number,
  timeLabel: string,
  source: AiTaskReminder['source']
): AiTaskReminder {
  return {
    id: buildReminderId(task, category),
    taskId: task.id,
    workspaceId: task.workspaceId,
    pageId: task.pageId,
    title: task.title,
    description: task.description,
    status: normalizeStatus(task.status) || task.status,
    priority: normalizePriority(task.priority) || task.priority,
    dueDate: task.dueDate,
    category,
    severity,
    icon,
    headline,
    message,
    actionText,
    score,
    timeLabel,
    source,
  }
}

function buildTaskReminders(
  task: WorkTaskResponse,
  now: Date,
  source: AiTaskReminder['source']
) {
  if (isDone(task)) return []

  const reminders: AiTaskReminder[] = []
  const dueDate = safeDate(task.dueDate)
  const dueHours = dueDate ? diffHours(dueDate, now) : null
  const dueLabel = formatDueDistance(dueDate, now)
  const dueDisplay = formatDisplayDate(task.dueDate)

  if (dueDate && dueHours !== null && dueHours < 0) {
    const overdueHours = Math.abs(dueHours)
    const score = 100 + Math.min(45, overdueHours)

    reminders.push(
      createReminder(
        task,
        'overdue',
        'critical',
        'priority_high',
        'Task đã quá hạn, xử lý trước nha',
        `Deadline là ${dueDisplay}. AI thấy task này đang trễ nên nên ưu tiên clear hoặc đổi hạn ngay.`,
        'Xử lý / cập nhật hạn',
        score,
        dueLabel,
        source
      )
    )
  } else if (dueDate && dueHours !== null && dueHours <= 4) {
    reminders.push(
      createReminder(
        task,
        'due-soon',
        'critical',
        'timer',
        'Sắp tới hạn cực gần',
        `Task này tới hạn lúc ${dueDisplay}. Nên làm ngay hoặc ping người phụ trách trước khi cháy.`,
        'Làm ngay',
        96,
        dueLabel,
        source
      )
    )
  } else if (dueDate && dueHours !== null && dueHours <= 24) {
    reminders.push(
      createReminder(
        task,
        'due-today',
        'warning',
        'today',
        'Task cần xong trong hôm nay',
        `Hạn là ${dueDisplay}. AI gợi ý đưa task này vào danh sách focus hôm nay.`,
        'Đưa vào hôm nay',
        82,
        dueLabel,
        source
      )
    )
  } else if (dueDate && dueHours !== null && dueHours <= 72) {
    reminders.push(
      createReminder(
        task,
        'due-week',
        'info',
        'event_upcoming',
        'Task sắp đến hạn trong vài ngày tới',
        `Còn không nhiều thời gian trước hạn ${dueDisplay}. Nên chuẩn bị trước để khỏi dồn việc.`,
        'Lên lịch làm',
        62,
        dueLabel,
        source
      )
    )
  }

  if (isHighPriority(task) && !reminders.some((item) => item.category === 'overdue')) {
    reminders.push(
      createReminder(
        task,
        'high-priority',
        dueDate && dueHours !== null && dueHours <= 24 ? 'warning' : 'info',
        'flag',
        'Task priority cao cần để mắt',
        task.dueDate
          ? `Task này có priority cao và hạn ${dueDisplay}. Nên kiểm tra tiến độ thường xuyên.`
          : 'Task này có priority cao nhưng chưa có deadline rõ ràng. Nên thêm hạn để AI nhắc chuẩn hơn.',
        'Kiểm tra priority',
        task.dueDate ? 58 : 54,
        dueLabel,
        source
      )
    )
  }

  if (isDoing(task)) {
    const updatedDate = safeDate(task.updatedDate ?? task.createdDate)
    const inactiveDays = updatedDate ? Math.max(0, -daysBetween(updatedDate, now)) : 0

    if (inactiveDays >= 2) {
      reminders.push(
        createReminder(
          task,
          'in-progress',
          'warning',
          'running_with_errors',
          'Task đang làm nhưng lâu chưa cập nhật',
          `Đã khoảng ${inactiveDays} ngày chưa thấy cập nhật. Nên comment tiến độ hoặc chuyển trạng thái nếu đã xong.`,
          'Cập nhật tiến độ',
          72 + Math.min(16, inactiveDays * 2),
          `${inactiveDays} ngày im lặng`,
          source
        )
      )
    }
  }

  if (!dueDate && isHighPriority(task)) {
    reminders.push(
      createReminder(
        task,
        'unscheduled',
        'gentle',
        'calendar_add_on',
        'Task quan trọng nhưng chưa có hạn',
        'AI không thể nhắc đúng thời điểm nếu task quan trọng mà chưa có due date. Thêm hạn sẽ đỡ quên hơn.',
        'Thêm due date',
        46,
        'Chưa đặt hạn',
        source
      )
    )
  }

  return reminders
}

function reminderWeight(severity: AiReminderSeverity) {
  return {
    critical: 4,
    warning: 3,
    info: 2,
    gentle: 1,
  }[severity]
}

function dedupeTasks(tasks: WorkTaskResponse[]) {
  const map = new Map<Guid, WorkTaskResponse>()

  for (const task of tasks) {
    map.set(task.id, task)
  }

  return Array.from(map.values())
}

function dedupeReminders(reminders: AiTaskReminder[]) {
  const map = new Map<string, AiTaskReminder>()

  for (const reminder of reminders) {
    const existing = map.get(reminder.id)

    if (!existing || reminder.score > existing.score) {
      map.set(reminder.id, reminder)
    }
  }

  return Array.from(map.values())
}

export function useTaskAiReminders(currentWorkspaceId: WorkspaceIdRef) {
  const assignedTasks = ref<WorkTaskResponse[]>([])
  const workspaceTasks = ref<WorkTaskResponse[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const generatedAt = ref<Date | null>(null)

  let refreshRequestId = 0
  let autoRefreshTimer: number | null = null

  const allTasks = computed(() => {
    return dedupeTasks([...assignedTasks.value, ...workspaceTasks.value])
  })

  const reminders = computed(() => {
    const now = generatedAt.value ?? new Date()
    const assignedTaskIds = new Set(assignedTasks.value.map((task) => task.id))

    return dedupeReminders(
      allTasks.value.flatMap((task) => {
        const source = assignedTaskIds.has(task.id) ? 'assigned' : 'workspace'

        return buildTaskReminders(task, now, source)
      })
    ).sort((left, right) => {
      const severityDiff = reminderWeight(right.severity) - reminderWeight(left.severity)

      if (severityDiff !== 0) return severityDiff
      if (right.score !== left.score) return right.score - left.score

      const leftDue = safeDate(left.dueDate)?.getTime() ?? Number.MAX_SAFE_INTEGER
      const rightDue = safeDate(right.dueDate)?.getTime() ?? Number.MAX_SAFE_INTEGER

      return leftDue - rightDue
    })
  })

  const urgentReminders = computed(() => {
    return reminders.value.filter(
      (reminder) => reminder.severity === 'critical' || reminder.severity === 'warning'
    )
  })

  const focusReminders = computed(() => {
    return reminders.value.slice(0, 5)
  })

  const summary = computed<AiReminderSummary>(() => {
    return reminders.value.reduce(
      (result, reminder) => {
        result.total += 1

        if (reminder.severity === 'critical') result.critical += 1
        if (reminder.severity === 'warning') result.warning += 1
        if (reminder.category === 'overdue') result.overdue += 1
        if (reminder.category === 'due-today') result.dueToday += 1
        if (reminder.category === 'due-soon') result.dueSoon += 1
        if (reminder.category === 'high-priority') result.highPriority += 1
        if (reminder.category === 'unscheduled') result.unscheduled += 1

        return result
      },
      {
        total: 0,
        critical: 0,
        warning: 0,
        overdue: 0,
        dueToday: 0,
        dueSoon: 0,
        highPriority: 0,
        unscheduled: 0,
        workspaceTaskCount: workspaceTasks.value.length,
        assignedTaskCount: assignedTasks.value.length,
      }
    )
  })

  const badgeCount = computed(() => {
    return urgentReminders.value.length
  })

  const badgeLabel = computed(() => {
    const count = badgeCount.value

    if (count <= 0) return ''
    if (count > 99) return '99+'

    return String(count)
  })

  async function refresh() {
    const requestId = ++refreshRequestId
    const workspaceId = currentWorkspaceId.value

    isLoading.value = true
    error.value = null

    const assignedRequest = meController.listMyAssignedTasks({
      pageNumber: 1,
      pageSize: MAX_PAGE_SIZE,
      includeCompleted: false,
    })

    const workspaceRequest = workspaceId
      ? taskController.listByWorkspace(workspaceId, {
          pageNumber: 1,
          pageSize: MAX_PAGE_SIZE,
          includeCompleted: false,
        })
      : Promise.resolve(null)

    const [assignedResult, workspaceResult] = await Promise.allSettled([
      assignedRequest,
      workspaceRequest,
    ])

    if (requestId !== refreshRequestId) return

    const errors: string[] = []

    if (assignedResult.status === 'fulfilled') {
      const response = assignedResult.value

      if (response.isSuccess && response.data) {
        assignedTasks.value = response.data.items ?? []
      } else {
        assignedTasks.value = []
        errors.push(getApiResultErrorMessage(response, 'Không tải được task được giao cho bạn.'))
      }
    } else {
      assignedTasks.value = []
      errors.push(getApiErrorMessage(assignedResult.reason, 'Không tải được task được giao cho bạn.'))
    }

    if (workspaceResult.status === 'fulfilled') {
      const response = workspaceResult.value

      if (!response) {
        workspaceTasks.value = []
      } else if (response.isSuccess && response.data) {
        workspaceTasks.value = response.data.items ?? []
      } else {
        workspaceTasks.value = []
        errors.push(getApiResultErrorMessage(response, 'Không tải được task trong workspace hiện tại.'))
      }
    } else {
      workspaceTasks.value = []
      errors.push(getApiErrorMessage(workspaceResult.reason, 'Không tải được task trong workspace hiện tại.'))
    }

    generatedAt.value = new Date()
    error.value = errors.length ? errors.join(' ') : null
    isLoading.value = false
  }

  function startAutoRefresh() {
    stopAutoRefresh()

    autoRefreshTimer = window.setInterval(() => {
      void refresh()
    }, AUTO_REFRESH_INTERVAL_MS)
  }

  function stopAutoRefresh() {
    if (autoRefreshTimer === null) return

    window.clearInterval(autoRefreshTimer)
    autoRefreshTimer = null
  }

  watch(
    () => currentWorkspaceId.value,
    () => {
      void refresh()
    },
    { immediate: true }
  )

  return {
    assignedTasks: readonly(assignedTasks),
    workspaceTasks: readonly(workspaceTasks),
    allTasks,
    reminders,
    urgentReminders,
    focusReminders,
    summary,
    badgeCount,
    badgeLabel,
    isLoading: readonly(isLoading),
    error: readonly(error),
    generatedAt: readonly(generatedAt),
    refresh,
    startAutoRefresh,
    stopAutoRefresh,
  }
}
