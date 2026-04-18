<template>
  <div class="task-list-wrap">
    <div class="task-row border-top-custom" v-for="task in tasks" :key="task.id">
      <div class="task-row-inner">
        <div class="task-row-left">
          <div class="status-dot" :class="statusClass(task.status)"></div>

          <div class="flex-grow-1 min-w-0">
            <h4 class="task-title mb-1">{{ task.title }}</h4>
            <p class="task-desc mb-0">{{ task.description }}</p>
          </div>
        </div>

        <div class="task-row-right d-none d-lg-flex">
          <span class="priority-badge" :class="priorityClass(task.priority)">
            {{ shortPriority(task.priority) }}
          </span>

          <span class="status-badge">
            {{ task.status }}
          </span>

          <div class="d-flex align-items-center gap-2">
            <img :src="task.assigneeAvatar" :alt="task.assigneeName" class="assignee-avatar" />
            <span class="meta-text">{{ task.assigneeName }}</span>
          </div>

          <span class="meta-text due-date">{{ task.dueDate }}</span>
        </div>
      </div>

      <div class="task-row-mobile d-lg-none mt-2">
        <div class="d-flex flex-wrap gap-2 align-items-center">
          <span class="priority-badge" :class="priorityClass(task.priority)">
            {{ shortPriority(task.priority) }}
          </span>
          <span class="status-badge">{{ task.status }}</span>
          <span class="meta-text">{{ task.assigneeName }}</span>
          <span class="meta-text">{{ task.dueDate }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  tasks: {
    type: Array,
    default: () => []
  }
})

function shortPriority(priority) {
  if (priority === 'Medium') return 'Med'
  return priority
}

function priorityClass(priority) {
  return priority.toLowerCase()
}

function statusClass(status) {
  return status.toLowerCase()
}
</script>

<style scoped>
.task-list-wrap {
  border-top: 1px solid rgba(72, 72, 72, 0.1);
}

.task-row {
  padding: 0.2rem 0.5rem;
  border-bottom: 1px solid rgba(72, 72, 72, 0.1);
  transition: 0.2s ease;
}

.task-row:hover {
  background: rgba(255, 255, 255, 0.04);
}

.task-row-inner {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 0;
}

.task-row-left {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: flex-start;
  gap: 1rem;
}

.task-row-right {
  flex-shrink: 0;
  align-items: center;
  gap: 1.5rem;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-top: 0.5rem;
  flex-shrink: 0;
}

.status-dot.todo {
  background: #60a5fa;
}

.status-dot.doing {
  background: #fbbf24;
}

.status-dot.done {
  background: #34d399;
}

.task-title {
  font-size: 0.92rem;
  font-weight: 700;
  color: #e7e5e5;
}

.task-desc {
  font-size: 0.76rem;
  color: rgba(172, 171, 170, 0.6);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.priority-badge,
.status-badge {
  padding: 0.2rem 0.55rem;
  border-radius: 0.45rem;
  font-size: 10px;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.priority-badge.high {
  background: rgba(127, 41, 39, 0.2);
  color: #ee7d77;
}

.priority-badge.medium {
  background: rgba(37, 38, 38, 1);
  color: #acabaa;
}

.priority-badge.low {
  background: rgba(37, 38, 38, 1);
  color: #acabaa;
}

.status-badge {
  background: #191a1a;
  color: #acabaa;
}

.assignee-avatar {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  object-fit: cover;
  filter: grayscale(1);
}

.meta-text {
  font-size: 11px;
  color: rgba(172, 171, 170, 0.5);
}

.due-date {
  min-width: 80px;
  text-align: right;
}

.task-row-mobile {
  padding-bottom: 0.8rem;
}
</style>