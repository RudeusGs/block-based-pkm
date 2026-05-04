<template>
  <div class="task-list-wrap">
    <div 
      class="task-row border-top-custom" 
      v-for="task in tasks" 
      :key="task.id"
      @click="handleRowClick(task)"
    >
      <div class="task-row-inner">
        <!-- Cột trái -->
        <div class="task-row-left">
          <!-- SỬ DỤNG HÀM TỪ UTILS -->
          <div class="status-dot" :class="getStatusClass(task.status)"></div>

          <div class="flex-grow-1 min-w-0">
            <h4 class="task-title mb-1">{{ task.title }}</h4>
            <p class="task-desc mb-0">{{ task.description || 'No description' }}</p>
          </div>
        </div>

        <!-- Cột phải: Giao diện Desktop -->
        <div class="task-row-right d-none d-lg-flex">
          <!-- SỬ DỤNG HÀM TỪ UTILS -->
          <span class="priority-badge" :class="getPriorityClass(task.priority)">
            {{ getShortPriority(task.priority) }}
          </span>

          <span class="status-badge cursor-pointer hover-scale" @click.stop="toggleStatus(task)">
            {{ getStatusLabel(task.status) }}
          </span>

          <div class="d-flex align-items-center gap-2">
            <img v-if="task.assigneeAvatar" :src="task.assigneeAvatar" :alt="task.assigneeName" class="assignee-avatar" />
            <div v-else class="assignee-avatar bg-secondary d-flex align-items-center justify-content-center text-white" style="font-size: 10px; font-weight: bold;">
              {{ task.assigneeName ? task.assigneeName.charAt(0).toUpperCase() : '?' }}
            </div>
            <span class="meta-text">{{ task.assigneeName || 'Unassigned' }}</span>
          </div>

          <span class="meta-text due-date">{{ task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'No due date' }}</span>
        </div>
      </div>

      <!-- Cột dưới: Giao diện Mobile -->
      <div class="task-row-mobile d-lg-none mt-2">
        <div class="d-flex flex-wrap gap-2 align-items-center">
           <!-- SỬ DỤNG HÀM TỪ UTILS -->
          <span class="priority-badge" :class="getPriorityClass(task.priority)">
            {{ getShortPriority(task.priority) }}
          </span>
          <span class="status-badge cursor-pointer hover-scale" @click.stop="toggleStatus(task)">
            {{ getStatusLabel(task.status) }}
          </span>
          <span class="meta-text">{{ task.assigneeName || 'Unassigned' }}</span>
          <span class="meta-text">{{ task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'No due date' }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
// IMPORT CÁC HÀM DÙNG CHUNG TỪ UTILS
import { getPriorityClass, getShortPriority, getStatusClass, getStatusLabel } from '@/utils/task.utils'

const props = defineProps({
  tasks: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['task-click', 'update-status'])

function handleRowClick(task) {
  emit('task-click', task)
}

function toggleStatus(task) {
  let nextStatus = task.status + 1
  if (nextStatus > 3) {
    nextStatus = 1
  }
  emit('update-status', task.id, nextStatus)
}
</script>

<style scoped>
/* Giữ nguyên phần CSS của bạn */
.cursor-pointer { cursor: pointer; }
.hover-scale { display: inline-block; transition: transform 0.15s ease, background-color 0.15s ease; }
.hover-scale:hover { transform: scale(1.05); background-color: #2a2b2b; }
.task-list-wrap { border-top: 1px solid rgba(72, 72, 72, 0.1); }
.task-row { padding: 0.2rem 0.5rem; border-bottom: 1px solid rgba(72, 72, 72, 0.1); transition: 0.2s ease; cursor: pointer; }
.task-row:hover { background: rgba(255, 255, 255, 0.04); }
.task-row-inner { display: flex; align-items: center; gap: 1rem; padding: 1rem 0; }
.task-row-left { flex: 1; min-width: 0; display: flex; align-items: flex-start; gap: 1rem; }
.task-row-right { flex-shrink: 0; align-items: center; gap: 1.5rem; }

.status-dot { width: 8px; height: 8px; border-radius: 50%; margin-top: 0.5rem; flex-shrink: 0; }
.status-dot.todo { background: #60a5fa; box-shadow: 0 0 8px rgba(96, 165, 250, 0.5); }
.status-dot.doing { background: #fbbf24; box-shadow: 0 0 8px rgba(251, 191, 36, 0.5); }
.status-dot.done { background: #34d399; box-shadow: 0 0 8px rgba(52, 211, 153, 0.5); }
.status-dot.unknown { background: #9ca3af; }

.task-title { font-size: 0.92rem; font-weight: 700; color: #e7e5e5; }
.task-desc { font-size: 0.76rem; color: rgba(172, 171, 170, 0.6); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.priority-badge, .status-badge { padding: 0.2rem 0.55rem; border-radius: 0.45rem; font-size: 10px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.08em; }
.priority-badge.high { background: rgba(238, 125, 119, 0.15); color: #ee7d77; border: 1px solid rgba(238, 125, 119, 0.3); }
.priority-badge.medium { background: rgba(255, 255, 255, 0.05); color: #acabaa; border: 1px solid rgba(255, 255, 255, 0.1); }
.priority-badge.low { background: rgba(255, 255, 255, 0.02); color: #6f777b; border: 1px solid rgba(255, 255, 255, 0.05); }

.status-badge { background: #191a1a; color: #acabaa; border: 1px solid rgba(255, 255, 255, 0.08); }

.assignee-avatar { width: 24px; height: 24px; border-radius: 50%; object-fit: cover; filter: grayscale(0.5); transition: 0.2s; }
.task-row:hover .assignee-avatar { filter: grayscale(0); }

.meta-text { font-size: 11px; color: rgba(172, 171, 170, 0.5); }
.due-date { min-width: 80px; text-align: right; }
.task-row-mobile { padding-bottom: 0.8rem; }
</style>