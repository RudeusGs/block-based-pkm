<template>
  <section class="sidebar-insight-panel tasks-panel">
    <header class="insight-panel-header">
      <div class="insight-panel-title-wrap">
        <span class="insight-panel-icon">
          <i class="bi bi-check2-square"></i>
        </span>

        <div class="insight-panel-title-copy">
          <strong>My Tasks</strong>
          <span>
            {{ openTaskCount > 0 ? `${openTaskCount} open tasks` : 'No open tasks' }}
          </span>
        </div>
      </div>

      <div class="insight-panel-actions">
        <span
          v-if="totalCount > 0"
          class="task-total-pill"
          title="Total tasks"
        >
          {{ totalCount }}
        </span>

        <button
          type="button"
          class="insight-icon-button"
          title="Tải lại"
          :disabled="isLoading"
          @click.stop="emit('refresh')"
        >
          <i
            class="bi bi-arrow-clockwise"
            :class="{ spinning: isLoading }"
          ></i>
        </button>
      </div>
    </header>

    <div
      v-if="tasks.length && !isLoading && !error"
      class="task-overview-grid"
    >
      <div class="task-overview-card">
        <span>{{ openTaskCount }}</span>
        <small>Open</small>
      </div>

      <div class="task-overview-card">
        <span>{{ highPriorityCount }}</span>
        <small>High</small>
      </div>

      <div class="task-overview-card">
        <span>{{ dueSoonCount }}</span>
        <small>Due soon</small>
      </div>
    </div>

    <div
      v-if="isLoading"
      class="task-skeleton-list"
    >
      <div
        v-for="index in 3"
        :key="index"
        class="task-skeleton-card"
      >
        <span class="task-skeleton-check"></span>

        <div class="task-skeleton-lines">
          <span></span>
          <span></span>
          <span></span>
        </div>
      </div>
    </div>

    <div
      v-else-if="error"
      class="task-state-card error"
    >
      <span class="task-state-icon">
        <i class="bi bi-exclamation-triangle"></i>
      </span>

      <div class="task-state-copy">
        <strong>Không thể tải My Tasks</strong>
        <p>{{ error }}</p>
      </div>

      <button
        type="button"
        class="task-state-action"
        @click.stop="emit('refresh')"
      >
        Thử lại
      </button>
    </div>

    <div
      v-else-if="!tasks.length"
      class="task-state-card empty"
    >
      <span class="task-state-icon">
        <i class="bi bi-check2-circle"></i>
      </span>

      <div class="task-state-copy">
        <strong>Bạn chưa có task đang mở</strong>
        <p>Task được assign cho bạn sẽ hiện ở đây để xử lý nhanh.</p>
      </div>
    </div>

    <div
      v-else
      class="task-card-list"
    >
      <button
        v-for="task in sortedTasks"
        :key="task.id"
        type="button"
        class="task-card"
        :class="[
          taskStatusClass(task.status),
          taskDueClass(task.dueDate),
        ]"
        @click.stop
      >
        <span class="task-status-marker">
          <i :class="statusIcon(task.status)"></i>
        </span>

        <span class="task-card-body">
          <span class="task-card-topline">
            <span class="task-title">
              {{ task.title }}
            </span>

            <span
              class="task-priority-dot"
              :class="taskPriorityClass(task.priority)"
              :title="priorityLabel(task.priority)"
            ></span>
          </span>

          <span
            v-if="task.description"
            class="task-description"
          >
            {{ task.description }}
          </span>

          <span class="task-meta-row">
            <span
              class="task-status-pill"
              :class="taskStatusClass(task.status)"
            >
              {{ statusLabel(task.status) }}
            </span>

            <span
              class="task-priority-pill"
              :class="taskPriorityClass(task.priority)"
            >
              {{ priorityLabel(task.priority) }}
            </span>

            <span
              v-if="task.dueDate"
              class="task-due-pill"
              :class="taskDueClass(task.dueDate)"
            >
              <i class="bi bi-calendar2-week"></i>
              {{ dueLabel(task.dueDate) }}
            </span>
          </span>
        </span>
      </button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { WorkTaskResponse } from '@/api/models/task.model'
import {
  formatDateTime,
  priorityLabel,
  statusLabel,
} from '../../utils/sidebar-format.util'

const props = defineProps<{
  tasks: WorkTaskResponse[]
  totalCount: number
  isLoading: boolean
  error: string | null
}>()

const emit = defineEmits<{
  refresh: []
}>()

const openTaskCount = computed(() => {
  return props.tasks.filter((task) => normalize(task.status) !== 'done').length
})

const highPriorityCount = computed(() => {
  return props.tasks.filter((task) => normalize(task.priority) === 'high')
    .length
})

const dueSoonCount = computed(() => {
  return props.tasks.filter((task) => {
    const state = dueState(task.dueDate)
    return state === 'overdue' || state === 'today' || state === 'soon'
  }).length
})

const sortedTasks = computed(() => {
  return [...props.tasks].sort((left, right) => {
    return taskScore(right) - taskScore(left)
  })
})

function normalize(value: string | null | undefined) {
  return value?.trim().toLowerCase() ?? ''
}

function priorityScore(priority: string) {
  const normalized = normalize(priority)

  if (normalized === 'high') return 30
  if (normalized === 'medium') return 20
  if (normalized === 'low') return 10

  return 0
}

function statusScore(status: string) {
  const normalized = normalize(status)

  if (normalized === 'doing') return 20
  if (normalized === 'todo') return 10
  if (normalized === 'done') return -100

  return 0
}

function dueScore(dueDate: string | null) {
  const state = dueState(dueDate)

  if (state === 'overdue') return 35
  if (state === 'today') return 30
  if (state === 'soon') return 18

  return 0
}

function taskScore(task: WorkTaskResponse) {
  return (
    priorityScore(task.priority) +
    statusScore(task.status) +
    dueScore(task.dueDate)
  )
}

function taskPriorityClass(priority: string) {
  const normalized = normalize(priority)

  if (normalized === 'high') return 'high'
  if (normalized === 'medium') return 'medium'
  if (normalized === 'low') return 'low'

  return 'none'
}

function taskStatusClass(status: string) {
  const normalized = normalize(status)

  if (normalized === 'doing') return 'doing'
  if (normalized === 'done') return 'done'
  if (normalized === 'todo') return 'todo'

  return 'todo'
}

function statusIcon(status: string) {
  const normalized = normalize(status)

  if (normalized === 'done') return 'bi bi-check2'
  if (normalized === 'doing') return 'bi bi-play-fill'

  return 'bi bi-circle'
}

function dueState(dueDate: string | null) {
  if (!dueDate) return 'none'

  const due = new Date(dueDate)
  if (Number.isNaN(due.getTime())) return 'none'

  const now = new Date()
  const startToday = new Date(
    now.getFullYear(),
    now.getMonth(),
    now.getDate()
  )
  const startTomorrow = new Date(startToday)
  startTomorrow.setDate(startToday.getDate() + 1)

  const soon = new Date(startToday)
  soon.setDate(startToday.getDate() + 4)

  if (due < startToday) return 'overdue'
  if (due < startTomorrow) return 'today'
  if (due < soon) return 'soon'

  return 'later'
}

function taskDueClass(dueDate: string | null) {
  return dueState(dueDate)
}

function dueLabel(dueDate: string) {
  const state = dueState(dueDate)

  if (state === 'overdue') return 'Overdue'
  if (state === 'today') return 'Today'
  if (state === 'soon') return formatDateTime(dueDate)

  return formatDateTime(dueDate)
}
</script>

<style scoped>
.sidebar-insight-panel {
  margin: -2px 0 12px;
  padding: 10px;
  border: 1px solid var(--sidebar-border);
  border-radius: 16px;
  background:
    radial-gradient(circle at top left, rgba(255, 255, 255, 0.045), transparent 42%),
    rgba(23, 23, 23, 0.94);
}

.insight-panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 10px;
}

.insight-panel-title-wrap {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 9px;
}

.insight-panel-icon {
  width: 32px;
  height: 32px;
  border: 1px solid var(--sidebar-border);
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--sidebar-text);
  background: linear-gradient(135deg, #252525, #171717);
}

.insight-panel-icon i {
  font-size: 14px;
}

.insight-panel-title-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.insight-panel-title-copy strong {
  color: var(--sidebar-text);
  font-size: 13.5px;
  font-weight: 850;
  letter-spacing: -0.01em;
}

.insight-panel-title-copy span {
  overflow: hidden;
  color: var(--sidebar-faint);
  font-size: 11px;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.insight-panel-actions {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.task-total-pill {
  min-width: 24px;
  height: 24px;
  border: 1px solid var(--sidebar-border);
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--sidebar-text);
  background: var(--sidebar-bg-active);
  font-size: 10.5px;
  font-weight: 900;
}

.insight-icon-button {
  width: 28px;
  height: 28px;
  border: 1px solid transparent;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--sidebar-faint);
  background: transparent;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease,
    opacity 140ms ease;
}

.insight-icon-button:hover:not(:disabled) {
  color: var(--sidebar-text);
  border-color: var(--sidebar-border);
  background: var(--sidebar-bg-hover);
}

.insight-icon-button:disabled {
  opacity: 0.42;
  cursor: not-allowed;
}

.insight-icon-button i {
  font-size: 13px;
}

.insight-icon-button i.spinning {
  animation: sidebar-spin 0.8s linear infinite;
}

.task-overview-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 7px;
  margin-bottom: 9px;
}

.task-overview-card {
  min-width: 0;
  border: 1px solid var(--sidebar-border);
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 8px 7px;
  background: var(--sidebar-bg);
}

.task-overview-card span {
  color: var(--sidebar-text);
  font-size: 15px;
  font-weight: 900;
  line-height: 1;
}

.task-overview-card small {
  overflow: hidden;
  color: var(--sidebar-faint);
  font-size: 10px;
  font-weight: 800;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.task-card-list,
.task-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.task-card {
  width: 100%;
  min-width: 0;
  border: 1px solid transparent;
  border-radius: 14px;
  display: flex;
  align-items: flex-start;
  gap: 9px;
  padding: 9px;
  color: var(--sidebar-muted);
  background: var(--sidebar-bg);
  text-align: left;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease,
    transform 140ms ease;
}

.task-card:hover {
  color: var(--sidebar-text);
  border-color: var(--sidebar-border);
  background: var(--sidebar-bg-hover);
  transform: translateY(-1px);
}

.task-card.overdue {
  border-color: rgba(248, 113, 113, 0.22);
  background:
    linear-gradient(135deg, rgba(248, 113, 113, 0.055), transparent),
    var(--sidebar-bg);
}

.task-card.today {
  border-color: rgba(251, 191, 36, 0.22);
  background:
    linear-gradient(135deg, rgba(251, 191, 36, 0.045), transparent),
    var(--sidebar-bg);
}

.task-status-marker {
  width: 28px;
  height: 28px;
  border: 1px solid var(--sidebar-border);
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--sidebar-faint);
  background: var(--sidebar-bg-soft);
}

.task-status-marker i {
  font-size: 12px;
}

.task-card.doing .task-status-marker {
  color: #fde68a;
  background: rgba(120, 53, 15, 0.2);
}

.task-card.done .task-status-marker {
  color: #86efac;
  background: rgba(20, 83, 45, 0.2);
}

.task-card-body {
  min-width: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.task-card-topline {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.task-title {
  min-width: 0;
  flex: 1;
  overflow: hidden;
  color: var(--sidebar-text);
  font-size: 12.8px;
  font-weight: 850;
  line-height: 1.35;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.task-priority-dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  flex-shrink: 0;
  background: #737373;
}

.task-priority-dot.high,
.task-priority-pill.high {
  color: #fecaca;
  background: rgba(127, 29, 29, 0.22);
  border-color: rgba(248, 113, 113, 0.22);
}

.task-priority-dot.high {
  background: #fca5a5;
}

.task-priority-dot.medium,
.task-priority-pill.medium {
  color: #fde68a;
  background: rgba(120, 53, 15, 0.2);
  border-color: rgba(251, 191, 36, 0.2);
}

.task-priority-dot.medium {
  background: #facc15;
}

.task-priority-dot.low,
.task-priority-pill.low {
  color: #bbf7d0;
  background: rgba(20, 83, 45, 0.18);
  border-color: rgba(74, 222, 128, 0.18);
}

.task-priority-dot.low {
  background: #86efac;
}

.task-description {
  display: -webkit-box;
  overflow: hidden;
  margin-top: 4px;
  color: var(--sidebar-faint);
  font-size: 11.2px;
  line-height: 1.38;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.task-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 5px;
  margin-top: 8px;
}

.task-status-pill,
.task-priority-pill,
.task-due-pill {
  min-height: 22px;
  border: 1px solid var(--sidebar-border);
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 2px 7px;
  color: var(--sidebar-faint);
  background: var(--sidebar-bg-soft);
  font-size: 10px;
  font-weight: 850;
  line-height: 1;
}

.task-status-pill.doing {
  color: #fde68a;
  background: rgba(120, 53, 15, 0.18);
  border-color: rgba(251, 191, 36, 0.18);
}

.task-status-pill.done {
  color: #bbf7d0;
  background: rgba(20, 83, 45, 0.18);
  border-color: rgba(74, 222, 128, 0.18);
}

.task-due-pill.overdue {
  color: #fecaca;
  background: rgba(127, 29, 29, 0.22);
  border-color: rgba(248, 113, 113, 0.22);
}

.task-due-pill.today,
.task-due-pill.soon {
  color: #fde68a;
  background: rgba(120, 53, 15, 0.18);
  border-color: rgba(251, 191, 36, 0.18);
}

.task-state-card {
  border: 1px solid var(--sidebar-border);
  border-radius: 14px;
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 11px;
  background: var(--sidebar-bg);
}

.task-state-card.error {
  border-color: rgba(248, 113, 113, 0.24);
  background: rgba(127, 29, 29, 0.14);
}

.task-state-icon {
  width: 30px;
  height: 30px;
  border: 1px solid var(--sidebar-border);
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--sidebar-muted);
  background: var(--sidebar-bg-soft);
}

.task-state-copy {
  min-width: 0;
  flex: 1;
}

.task-state-copy strong {
  display: block;
  color: var(--sidebar-text);
  font-size: 12.5px;
  font-weight: 850;
}

.task-state-copy p {
  margin: 3px 0 0;
  color: var(--sidebar-faint);
  font-size: 11.2px;
  line-height: 1.4;
}

.task-state-action {
  border: 1px solid var(--sidebar-border);
  border-radius: 8px;
  padding: 5px 8px;
  color: var(--sidebar-text);
  background: var(--sidebar-bg-soft);
  font-size: 11px;
  font-weight: 850;
}

.task-skeleton-card {
  display: flex;
  gap: 9px;
  border-radius: 14px;
  padding: 9px;
  background: var(--sidebar-bg);
}

.task-skeleton-check,
.task-skeleton-lines span {
  display: block;
  border-radius: 999px;
  background: linear-gradient(90deg, #202020, #2a2a2a, #202020);
  background-size: 220% 100%;
  animation: sidebar-skeleton 1.2s ease-in-out infinite;
}

.task-skeleton-check {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
}

.task-skeleton-lines {
  min-width: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding-top: 2px;
}

.task-skeleton-lines span {
  height: 8px;
}

.task-skeleton-lines span:nth-child(1) {
  width: 78%;
}

.task-skeleton-lines span:nth-child(2) {
  width: 100%;
}

.task-skeleton-lines span:nth-child(3) {
  width: 58%;
}

.insight-icon-button:focus-visible,
.task-card:focus-visible,
.task-state-action:focus-visible {
  outline: 2px solid #525252;
  outline-offset: 2px;
}

@keyframes sidebar-spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes sidebar-skeleton {
  0% {
    background-position: 120% 0;
  }

  100% {
    background-position: -120% 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .task-card,
  .insight-icon-button,
  .task-skeleton-check,
  .task-skeleton-lines span,
  .insight-icon-button i.spinning {
    transition: none;
    animation: none;
  }
}
</style>