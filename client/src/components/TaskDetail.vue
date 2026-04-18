<template>
  <aside class="task-detail-panel d-flex flex-column">
    <div class="task-detail-scroll p-4 h-100 overflow-auto">
      <div class="d-flex align-items-center justify-content-between mb-4">
        <span class="panel-label">Task Detail</span>

        <button class="close-btn" type="button" @click="$emit('close')">
          <span class="material-symbols-outlined">close</span>
        </button>
      </div>

      <div class="mb-4">
        <div class="task-breadcrumb">
          {{ normalizedTask.pageTitle }}
        </div>

        <h2 class="task-title mb-3">
          {{ normalizedTask.title }}
        </h2>

        <div class="d-flex flex-wrap gap-2">
          <span class="tag-pill tag-primary">
            {{ normalizedTask.status }}
          </span>

          <span class="tag-pill tag-muted">
            {{ normalizedTask.priority }}
          </span>
        </div>
      </div>

      <div class="detail-section mb-4">
        <div class="row g-3">
          <div class="col-12 col-sm-6">
            <div class="meta-label mb-2">Assignee</div>

            <div class="meta-card d-flex align-items-center gap-2">
              <img
                v-if="normalizedTask.assignee.avatar"
                class="mini-avatar rounded-circle"
                :src="normalizedTask.assignee.avatar"
                :alt="normalizedTask.assignee.name"
              />
              <div v-else class="mini-avatar mini-avatar-fallback rounded-circle">
                {{ assigneeInitial }}
              </div>

              <span class="meta-value">
                {{ normalizedTask.assignee.name }}
              </span>
            </div>
          </div>

          <div class="col-12 col-sm-6">
            <div class="meta-label mb-2">Due Date</div>

            <div class="meta-card d-flex align-items-center gap-2">
              <span class="material-symbols-outlined calendar-icon">calendar_today</span>
              <span class="meta-value">
                {{ normalizedTask.dueDate || 'No deadline' }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div class="detail-section mb-4">
        <div class="meta-label mb-2">Description</div>

        <div class="description-card">
          <p class="task-desc mb-0">
            {{ normalizedTask.description || 'Task này chưa có mô tả.' }}
          </p>
        </div>
      </div>

      <div class="detail-section">
        <div class="meta-label mb-2">Overview</div>

        <div class="overview-list d-flex flex-column gap-2">
          <div class="overview-item d-flex align-items-center justify-content-between gap-3">
            <span class="overview-key">Status</span>
            <span class="overview-value">{{ normalizedTask.status }}</span>
          </div>

          <div class="overview-item d-flex align-items-center justify-content-between gap-3">
            <span class="overview-key">Priority</span>
            <span class="overview-value">{{ normalizedTask.priority }}</span>
          </div>

          <div class="overview-item d-flex align-items-center justify-content-between gap-3">
            <span class="overview-key">Page</span>
            <span class="overview-value text-end">{{ normalizedTask.pageTitle }}</span>
          </div>
        </div>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  task: {
    type: Object,
    required: true
  }
})

defineEmits(['close'])

const normalizedTask = computed(() => {
  const task = props.task || {}

  return {
    pageTitle: task.pageTitle || 'Untitled Page',
    title: task.title || 'Untitled Task',
    description: task.description || '',
    priority: task.priority || 'Medium',
    status: task.status || 'ToDo',
    dueDate: task.dueDate || '',
    assignee: {
      name: task?.assignee?.name || task.assigneeName || 'Unknown',
      avatar: task?.assignee?.avatar || task.assigneeAvatar || ''
    }
  }
})

const assigneeInitial = computed(() => {
  return normalizedTask.value.assignee.name.charAt(0).toUpperCase()
})
</script>

<style scoped>
.task-detail-panel {
  position: fixed;
  top: 0;
  right: 320px;
  bottom: 0;
  width: 400px;
  z-index: 1035;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.025), rgba(255, 255, 255, 0.015)),
    #111111;
  border-left: 1px solid rgba(72, 72, 72, 0.15);
  color: #e7e5e4;
  box-shadow: -12px 0 32px rgba(0, 0, 0, 0.35);
}

.task-detail-scroll {
  scrollbar-width: thin;
  scrollbar-color: rgba(189, 194, 255, 0.28) transparent;
}

.task-detail-scroll::-webkit-scrollbar {
  width: 8px;
}

.task-detail-scroll::-webkit-scrollbar-track {
  background: transparent;
}

.task-detail-scroll::-webkit-scrollbar-thumb {
  background: rgba(189, 194, 255, 0.24);
  border-radius: 999px;
  border: 2px solid transparent;
  background-clip: padding-box;
}

.panel-label {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: #bdc2ff;
}

.close-btn {
  width: 34px;
  height: 34px;
  border: 0;
  border-radius: 10px;
  background: transparent;
  color: #acabaa;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: 0.2s ease;
}

.close-btn:hover {
  background: rgba(255, 255, 255, 0.05);
  color: #e7e5e4;
}

.task-breadcrumb {
  font-size: 11px;
  color: #8e8d8c;
  margin-bottom: 8px;
}

.task-title {
  font-size: 28px;
  line-height: 1.25;
  font-weight: 700;
  color: #e7e5e4;
}

.tag-pill {
  padding: 6px 12px;
  border-radius: 999px;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.tag-primary {
  color: #bdc2ff;
  background: rgba(189, 194, 255, 0.1);
}

.tag-muted {
  color: #acabaa;
  background: rgba(255, 255, 255, 0.05);
}

.detail-section {
  border-top: 1px solid rgba(72, 72, 72, 0.12);
  padding-top: 18px;
}

.meta-label {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: rgba(172, 171, 170, 0.6);
}

.meta-card {
  min-height: 44px;
  padding: 10px 12px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

.meta-value {
  font-size: 12px;
  color: #e7e5e4;
}

.description-card {
  padding: 14px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

.task-desc {
  font-size: 12px;
  line-height: 1.8;
  color: #acabaa;
}

.mini-avatar {
  width: 28px;
  height: 28px;
  object-fit: cover;
}

.mini-avatar-fallback {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: rgba(189, 194, 255, 0.12);
  color: #bdc2ff;
  font-size: 11px;
  font-weight: 700;
}

.calendar-icon {
  font-size: 18px;
  color: #acabaa;
}

.overview-item {
  min-height: 44px;
  padding: 10px 12px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.025);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

.overview-key {
  font-size: 11px;
  color: #8f8e8d;
}

.overview-value {
  font-size: 12px;
  font-weight: 600;
  color: #e7e5e4;
}

.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 300, 'GRAD' 0, 'opsz' 24;
}

@media (max-width: 1400px) {
  .task-detail-panel {
    right: 0;
  }
}

@media (max-width: 992px) {
  .task-detail-panel {
    top: 0;
    right: 0;
    width: 100%;
    z-index: 1045;
  }

  .task-title {
    font-size: 24px;
  }
}
</style>