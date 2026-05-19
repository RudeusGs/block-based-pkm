<template>
  <section class="notion-sidebar-panel tasks-panel">
    <header class="notion-panel-header">
      <div class="notion-panel-title">
        <strong>My Tasks</strong>
        <span>{{ openTaskCount }} open</span>
      </div>

      <button
        type="button"
        class="notion-panel-action"
        title="Tải lại"
        :disabled="isLoading"
        @click.stop="emit('refresh')"
      >
        <i
          class="bi bi-arrow-clockwise"
          :class="{ spinning: isLoading }"
        ></i>
      </button>
    </header>

    <div
      v-if="isLoading"
      class="notion-skeleton-list"
    >
      <div
        v-for="index in 3"
        :key="index"
        class="notion-skeleton-row"
      >
        <span></span>
        <span></span>
      </div>
    </div>

    <div
      v-else-if="error"
      class="notion-empty-state error"
    >
      <strong>Không thể tải My Tasks</strong>
      <p>{{ error }}</p>

      <button
        type="button"
        @click.stop="emit('refresh')"
      >
        Thử lại
      </button>
    </div>

    <div
      v-else-if="!tasks.length"
      class="notion-empty-state"
    >
      <strong>Bạn chưa có task đang mở</strong>
      <p>Task được assign cho bạn sẽ hiện ở đây.</p>
    </div>

    <div
      v-else
      class="notion-task-list"
    >
      <button
        v-for="task in sortedTasks"
        :key="task.id"
        type="button"
        class="notion-task-row"
        :class="taskDueClass(task.dueDate)"
        @click.stop
      >
        <span
          class="notion-task-check"
          :class="taskStatusClass(task.status)"
          aria-hidden="true"
        >
          <i :class="statusIcon(task.status)"></i>
        </span>

        <span class="notion-task-content">
          <span class="notion-task-title-line">
            <span class="notion-task-title">
              {{ task.title }}
            </span>

            <span
              class="notion-priority-dot"
              :class="taskPriorityClass(task.priority)"
              :title="priorityLabel(task.priority)"
            ></span>
          </span>

          <span
            v-if="task.description"
            class="notion-task-description"
          >
            {{ task.description }}
          </span>

          <span class="notion-task-meta">
            <span>{{ statusLabel(task.status) }}</span>
            <span>{{ priorityLabel(task.priority) }}</span>
            <span v-if="task.dueDate">{{ dueLabel(task.dueDate) }}</span>
          </span>
        </span>
      </button>
    </div>

    <footer
      v-if="totalCount > tasks.length && !isLoading && !error"
      class="notion-panel-footer"
    >
      Showing {{ tasks.length }} of {{ totalCount }} tasks
    </footer>
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
  if (normalized === 'doing') return 'bi bi-dash'

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

  return formatDateTime(dueDate)
}
</script>

<style scoped>
.notion-sidebar-panel {
  margin: 2px 0 10px;
  padding: 4px 0 8px;
  border-bottom: 1px solid var(--sidebar-border);
}

.notion-panel-header {
  min-height: 30px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 0 4px 4px 7px;
}

.notion-panel-title {
  min-width: 0;
  display: flex;
  align-items: baseline;
  gap: 7px;
}

.notion-panel-title strong {
  color: var(--sidebar-text);
  font-size: 12px;
  font-weight: 700;
}

.notion-panel-title span,
.notion-panel-footer {
  color: var(--sidebar-faint);
  font-size: 11px;
}

.notion-panel-action {
  width: 24px;
  height: 24px;
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--sidebar-faint);
  background: transparent;
}

.notion-panel-action:hover:not(:disabled) {
  color: var(--sidebar-text);
  background: var(--sidebar-bg-hover);
}

.notion-panel-action i {
  font-size: 13px;
}

.notion-panel-action i.spinning {
  animation: sidebar-spin 0.8s linear infinite;
}

.notion-task-list,
.notion-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.notion-task-row {
  width: 100%;
  min-width: 0;
  border: 0;
  border-radius: 6px;
  display: flex;
  align-items: flex-start;
  gap: 7px;
  padding: 5px 7px;
  color: var(--sidebar-muted);
  background: transparent;
  text-align: left;
}

.notion-task-row:hover {
  color: var(--sidebar-text);
  background: var(--sidebar-bg-hover);
}

.notion-task-check {
  width: 18px;
  height: 20px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--sidebar-faint);
}

.notion-task-check i {
  font-size: 12px;
}

.notion-task-check.doing {
  color: #d6b15d;
}

.notion-task-check.done {
  color: #75b798;
}

.notion-task-content {
  min-width: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.notion-task-title-line {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 6px;
}

.notion-task-title {
  min-width: 0;
  flex: 1;
  overflow: hidden;
  color: var(--sidebar-text);
  font-size: 13px;
  line-height: 1.35;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.notion-task-description {
  display: -webkit-box;
  overflow: hidden;
  color: var(--sidebar-faint);
  font-size: 11.5px;
  line-height: 1.35;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.notion-task-meta {
  min-width: 0;
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 4px 8px;
  color: var(--sidebar-faint);
  font-size: 10.5px;
}

.notion-task-meta span + span::before {
  content: '·';
  margin-right: 8px;
  color: var(--sidebar-border-strong);
}

.notion-priority-dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
  flex-shrink: 0;
  background: var(--sidebar-faint);
}

.notion-priority-dot.high,
.notion-task-row.overdue .notion-task-title {
  color: #f2a6a6;
}

.notion-priority-dot.high {
  background: #d55f5f;
}

.notion-priority-dot.medium {
  background: #d6b15d;
}

.notion-priority-dot.low {
  background: #75b798;
}

.notion-task-row.today .notion-task-title,
.notion-task-row.soon .notion-task-title {
  color: #e4c978;
}

.notion-empty-state {
  padding: 8px 7px 10px;
  color: var(--sidebar-faint);
  font-size: 12px;
  line-height: 1.45;
}

.notion-empty-state strong {
  display: block;
  color: var(--sidebar-muted);
  font-size: 12.5px;
  font-weight: 650;
}

.notion-empty-state p {
  margin: 3px 0 0;
}

.notion-empty-state.error strong {
  color: var(--sidebar-danger);
}

.notion-empty-state button {
  margin-top: 7px;
  border: 0;
  border-radius: 6px;
  padding: 4px 7px;
  color: var(--sidebar-text);
  background: var(--sidebar-bg-hover);
  font-size: 12px;
}

.notion-skeleton-row {
  padding: 7px;
}

.notion-skeleton-row span {
  display: block;
  height: 8px;
  border-radius: 999px;
  background: linear-gradient(90deg, #191919, #242424, #191919);
  background-size: 220% 100%;
  animation: sidebar-skeleton 1.2s ease-in-out infinite;
}

.notion-skeleton-row span:first-child {
  width: 76%;
}

.notion-skeleton-row span:last-child {
  width: 52%;
  margin-top: 7px;
}

.notion-panel-footer {
  padding: 7px 7px 0;
}

.notion-panel-action:focus-visible,
.notion-task-row:focus-visible,
.notion-empty-state button:focus-visible {
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
  .notion-panel-action i.spinning,
  .notion-skeleton-row span {
    animation: none;
  }
}
</style>