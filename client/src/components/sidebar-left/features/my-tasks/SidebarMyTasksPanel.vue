<template>
  <Teleport to="body">
    <Transition name="notion-task-overlay">
      <div
        v-if="open"
        class="notion-task-overlay"
        @click="emit('close')"
      ></div>
    </Transition>

    <Transition name="notion-task-panel">
      <aside
        v-if="open"
        class="notion-task-drawer"
        role="dialog"
        aria-modal="true"
        aria-label="My tasks"
        @click.stop
      >
        <header class="notion-task-header">
          <div class="notion-task-title-content">
            <span>My Tasks</span>
            <h2>Việc của bạn</h2>
          </div>

          <button
            type="button"
            class="notion-task-icon-btn"
            title="Đóng"
            aria-label="Đóng My Tasks"
            @click="emit('close')"
          >
            <span aria-hidden="true">×</span>
          </button>
        </header>

        <main class="notion-task-body">
          <section class="notion-task-command-box">
            <div>
              <h3>Inbox công việc cá nhân</h3>

              <p>
                Tất cả task được assign cho bạn sẽ hiển thị tại đây, không cần
                chọn đúng workspace hay page.
              </p>
            </div>

            <button
              type="button"
              class="notion-task-primary"
              :disabled="isLoading"
              @click="emit('refresh')"
            >
              {{ isLoading ? 'Đang tải...' : 'Tải lại' }}
            </button>
          </section>

          <section class="notion-task-stat-grid">
            <article class="notion-task-stat-card">
              <span>Open</span>
              <strong>{{ openTaskCount }}</strong>
            </article>

            <article class="notion-task-stat-card">
              <span>Overdue</span>
              <strong>{{ overdueTaskCount }}</strong>
            </article>

            <article class="notion-task-stat-card">
              <span>High</span>
              <strong>{{ highPriorityCount }}</strong>
            </article>
          </section>

          <section class="notion-task-list-head">
            <div>
              <strong>{{ tasks.length }}</strong>
              <span>task đang hiển thị</span>
            </div>

            <span v-if="totalCount > tasks.length">
              {{ tasks.length }} / {{ totalCount }}
            </span>
          </section>

          <section class="notion-task-content">
            <div
              v-if="error"
              class="notion-task-empty notion-task-error"
            >
              <h3>Không tải được My Tasks</h3>
              <p>{{ error }}</p>

              <button
                type="button"
                class="notion-task-primary small"
                @click="emit('refresh')"
              >
                Thử lại
              </button>
            </div>

            <div
              v-else-if="isLoading"
              class="notion-task-skeleton-list"
            >
              <div
                v-for="index in 5"
                :key="index"
                class="notion-task-skeleton-card"
              >
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>

            <div
              v-else-if="!tasks.length"
              class="notion-task-empty"
            >
              <h3>Chưa có task đang mở</h3>

              <p>
                Khi có task được assign cho bạn, nó sẽ tự xuất hiện ở đây.
              </p>
            </div>

            <div
              v-else
              class="notion-task-list"
            >
              <article
                v-for="task in sortedTasks"
                :key="task.id"
                class="notion-task-card"
                :class="taskDueClass(task.dueDate)"
              >
                <div class="notion-task-check">
                  <span>{{ statusMark(task.status) }}</span>
                </div>

                <div class="notion-task-card-main">
                  <div class="notion-task-card-header">
                    <h3>{{ task.title }}</h3>

                    <span
                      v-if="task.dueDate"
                      class="notion-task-due"
                      :class="taskDueClass(task.dueDate)"
                    >
                      {{ dueLabel(task.dueDate) }}
                    </span>
                  </div>

                  <p
                    v-if="task.description"
                    class="notion-task-description"
                  >
                    {{ task.description }}
                  </p>

                  <div class="notion-task-meta-line">
                    <span>{{ statusLabel(task.status) }}</span>
                    <span>{{ priorityLabel(task.priority) }}</span>
                    <span v-if="task.pageId">Page task</span>
                    <span v-else>No page</span>
                  </div>
                </div>
              </article>
            </div>
          </section>
        </main>
      </aside>
    </Transition>
  </Teleport>
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
  open: boolean
  tasks: WorkTaskResponse[]
  totalCount: number
  isLoading: boolean
  error: string | null
}>()

const emit = defineEmits<{
  close: []
  refresh: []
}>()

const openTaskCount = computed(() => {
  return props.tasks.filter((task) => normalize(task.status) !== 'done').length
})

const overdueTaskCount = computed(() => {
  return props.tasks.filter((task) => dueState(task.dueDate) === 'overdue').length
})

const highPriorityCount = computed(() => {
  return props.tasks.filter((task) => normalize(task.priority) === 'high').length
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

function statusMark(status: string) {
  const normalized = normalize(status)

  if (normalized === 'done') return '✓'
  if (normalized === 'doing') return '−'

  return ''
}
</script>

<style scoped>
.notion-task-overlay {
  position: fixed;
  inset: 0;
  z-index: 940;
  background: rgba(0, 0, 0, 0.18);
}

.notion-task-drawer {
  position: fixed;
  z-index: 950;
  top: 0;
  right: 0;
  bottom: 0;
  left: auto;
  width: min(620px, calc(100vw - 48px));
  overflow: hidden;
  display: flex;
  flex-direction: column;
  border-left: 1px solid #2a2a2a;
  border-radius: 0;
  color: #e6e6e6;
  background: #191919;
  box-shadow: -16px 0 48px rgba(0, 0, 0, 0.36);
}

.notion-task-header {
  min-height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 12px 16px;
  border-bottom: 1px solid #2a2a2a;
  background: #191919;
}

.notion-task-title-content {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.notion-task-title-content span {
  color: #8b8b8b;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.notion-task-title-content h2 {
  margin: 0;
  overflow: hidden;
  color: #e6e6e6;
  font-size: 17px;
  font-weight: 650;
  line-height: 1.2;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.notion-task-icon-btn {
  width: 30px;
  height: 30px;
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #9b9b9b;
  background: transparent;
  cursor: pointer;
}

.notion-task-icon-btn:hover {
  color: #eeeeee;
  background: #252525;
}

.notion-task-icon-btn span {
  font-size: 22px;
  line-height: 1;
}

.notion-task-body {
  min-height: 0;
  overflow-y: auto;
  padding: 14px;
}

.notion-task-body::-webkit-scrollbar {
  width: 10px;
}

.notion-task-body::-webkit-scrollbar-track {
  background: transparent;
}

.notion-task-body::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.notion-task-command-box {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
  padding: 12px 0 14px;
  border-bottom: 1px solid #2a2a2a;
}

.notion-task-command-box h3 {
  margin: 0;
  color: #e6e6e6;
  font-size: 14px;
  font-weight: 650;
  line-height: 1.35;
}

.notion-task-command-box p {
  max-width: 420px;
  margin: 5px 0 0;
  color: #9b9b9b;
  font-size: 12.5px;
  line-height: 1.5;
}

.notion-task-primary {
  min-height: 30px;
  border: 1px solid #373737;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0 10px;
  color: #d8d8d8;
  background: #222222;
  font-size: 12.5px;
  font-weight: 550;
  white-space: nowrap;
  cursor: pointer;
}

.notion-task-primary:hover:not(:disabled) {
  background: #2a2a2a;
  border-color: #464646;
}

.notion-task-primary:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.notion-task-primary.small {
  margin-top: 10px;
}

.notion-task-stat-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  margin-bottom: 14px;
}

.notion-task-stat-card {
  padding: 10px;
  border: 1px solid #2a2a2a;
  border-radius: 6px;
  background: #1d1d1d;
}

.notion-task-stat-card span {
  display: block;
  color: #8b8b8b;
  font-size: 11.5px;
  font-weight: 500;
  line-height: 1.3;
}

.notion-task-stat-card strong {
  display: block;
  margin-top: 4px;
  color: #e6e6e6;
  font-size: 20px;
  font-weight: 650;
  letter-spacing: -0.03em;
  line-height: 1;
}

.notion-task-list-head {
  min-height: 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  color: #858585;
  font-size: 12px;
}

.notion-task-list-head div {
  display: inline-flex;
  align-items: baseline;
  gap: 5px;
}

.notion-task-list-head strong {
  color: #d8d8d8;
  font-size: 14px;
  font-weight: 650;
}

.notion-task-content {
  min-height: 260px;
}

.notion-task-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.notion-task-card {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px 8px;
  border: 1px solid transparent;
  border-radius: 6px;
  background: transparent;
  transition:
    background-color 120ms ease,
    border-color 120ms ease;
}

.notion-task-card:hover {
  border-color: #2c2c2c;
  background: #202020;
}

.notion-task-card.overdue .notion-task-due {
  color: #d8d8d8;
  border-color: #555555;
  background: #2a2a2a;
}

.notion-task-check {
  width: 18px;
  height: 18px;
  margin-top: 2px;
  border: 1px solid #5a5a5a;
  border-radius: 4px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #d8d8d8;
  background: transparent;
  font-size: 12px;
  line-height: 1;
}

.notion-task-card-main {
  min-width: 0;
  flex: 1;
}

.notion-task-card-header {
  min-width: 0;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.notion-task-card h3 {
  margin: 0;
  overflow: hidden;
  color: #e6e6e6;
  font-size: 14px;
  font-weight: 560;
  line-height: 1.4;
  text-overflow: ellipsis;
}

.notion-task-due {
  min-height: 22px;
  border: 1px solid #333333;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
  padding: 0 8px;
  color: #9b9b9b;
  background: #202020;
  font-size: 11px;
  font-weight: 500;
  line-height: 1;
}

.notion-task-description {
  display: -webkit-box;
  overflow: hidden;
  margin: 5px 0 0;
  color: #9b9b9b;
  font-size: 12.5px;
  line-height: 1.45;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.notion-task-meta-line {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 8px;
  color: #858585;
  font-size: 11.5px;
}

.notion-task-meta-line span {
  display: inline-flex;
  align-items: center;
}

.notion-task-meta-line span:not(:last-child)::after {
  content: '·';
  margin-left: 6px;
  color: #5f5f5f;
}

.notion-task-empty {
  min-height: 280px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  padding: 28px 18px;
  border: 1px dashed #333333;
  border-radius: 6px;
  color: #8b8b8b;
  background: transparent;
  text-align: center;
}

.notion-task-empty h3 {
  margin: 0;
  color: #d8d8d8;
  font-size: 14px;
  font-weight: 600;
}

.notion-task-empty p {
  max-width: 340px;
  margin: 6px 0 0;
  font-size: 12.5px;
  line-height: 1.5;
}

.notion-task-error {
  border-color: #4a4a4a;
}

.notion-task-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.notion-task-skeleton-card {
  padding: 12px 8px;
  border-radius: 6px;
  background: transparent;
}

.notion-task-skeleton-card span {
  display: block;
  height: 8px;
  border-radius: 999px;
  background: linear-gradient(90deg, #222222, #2b2b2b, #222222);
  background-size: 220% 100%;
  animation: notion-task-skeleton 1.2s ease-in-out infinite;
}

.notion-task-skeleton-card span:nth-child(1) {
  width: 28%;
}

.notion-task-skeleton-card span:nth-child(2) {
  width: 78%;
  margin-top: 12px;
}

.notion-task-skeleton-card span:nth-child(3) {
  width: 52%;
  margin-top: 8px;
}

.notion-task-overlay-enter-active,
.notion-task-overlay-leave-active,
.notion-task-panel-enter-active,
.notion-task-panel-leave-active {
  transition:
    opacity 150ms ease,
    transform 160ms ease;
}

.notion-task-overlay-enter-from,
.notion-task-overlay-leave-to {
  opacity: 0;
}

.notion-task-panel-enter-from,
.notion-task-panel-leave-to {
  opacity: 0;
  transform: translateX(18px);
}

.notion-task-icon-btn:focus-visible,
.notion-task-primary:focus-visible {
  outline: 2px solid #555555;
  outline-offset: 2px;
}

@keyframes notion-task-skeleton {
  0% {
    background-position: 120% 0;
  }

  100% {
    background-position: -120% 0;
  }
}

@media (max-width: 900px) {
  .notion-task-drawer {
    inset: 0;
    width: auto;
  }

  .notion-task-command-box {
    flex-direction: column;
  }

  .notion-task-primary {
    width: 100%;
  }

  .notion-task-stat-grid {
    grid-template-columns: 1fr;
  }
}

@media (prefers-reduced-motion: reduce) {
  .notion-task-overlay-enter-active,
  .notion-task-overlay-leave-active,
  .notion-task-panel-enter-active,
  .notion-task-panel-leave-active,
  .notion-task-skeleton-card span {
    transition: none;
    animation: none;
  }
}
</style>
