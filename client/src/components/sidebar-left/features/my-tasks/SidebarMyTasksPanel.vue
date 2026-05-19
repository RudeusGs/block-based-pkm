<template>
  <section class="lunar-panel">
    <div class="lunar-panel-head">
      <span>My Tasks</span>

      <div class="lunar-panel-actions">
        <span
          v-if="totalCount > 0"
          class="lunar-panel-count"
        >
          {{ totalCount }}
        </span>

        <button
          type="button"
          title="Tải lại"
          @click.stop="emit('refresh')"
        >
          <i class="bi bi-arrow-clockwise"></i>
        </button>
      </div>
    </div>

    <div
      v-if="isLoading"
      class="lunar-empty compact"
    >
      Đang tải task…
    </div>

    <div
      v-else-if="error"
      class="lunar-error compact"
    >
      <p>{{ error }}</p>
    </div>

    <div
      v-else-if="!tasks.length"
      class="lunar-empty compact"
    >
      Bạn chưa có task đang mở.
    </div>

    <div
      v-else
      class="lunar-task-list"
    >
      <button
        v-for="task in tasks"
        :key="task.id"
        type="button"
        class="lunar-mini-task"
        @click.stop
      >
        <span class="lunar-mini-task-top">
          <span class="lunar-mini-task-title">
            {{ task.title }}
          </span>

          <span
            class="lunar-priority-dot"
            :class="taskPriorityTone(task.priority)"
          ></span>
        </span>

        <span
          v-if="task.description"
          class="lunar-mini-task-desc"
        >
          {{ task.description }}
        </span>

        <span class="lunar-mini-task-meta">
          <span
            class="lunar-status-pill"
            :class="taskStatusTone(task.status)"
          >
            {{ statusLabel(task.status) }}
          </span>

          <span>{{ priorityLabel(task.priority) }}</span>

          <span v-if="task.dueDate">
            {{ formatDateTime(task.dueDate) }}
          </span>
        </span>
      </button>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { WorkTaskResponse } from '@/api/models/task.model'
import {
  formatDateTime,
  priorityLabel,
  statusLabel,
  taskPriorityTone,
  taskStatusTone,
} from '../../utils/sidebar-format.util'

defineProps<{
  tasks: WorkTaskResponse[]
  totalCount: number
  isLoading: boolean
  error: string | null
}>()

const emit = defineEmits<{
  refresh: []
}>()
</script>