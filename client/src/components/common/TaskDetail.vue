<template>
  <aside class="task-detail-panel d-flex flex-column border-start border-outline-variant">
    <div class="task-detail-scroll p-4 h-100 overflow-auto">
      
      <!-- Header / Nút đóng -->
      <div class="d-flex align-items-center justify-content-between mb-4">
        <span class="panel-label">Task Detail</span>
        <button class="close-btn" type="button" @click="$emit('close')">
          <span class="material-symbols-outlined">close</span>
        </button>
      </div>

      <!-- Tiêu đề & Badges -->
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

      <!-- Người thực hiện & Ngày hết hạn -->
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
              <span class="meta-value text-truncate">
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

      <!-- Mô tả -->
      <div class="detail-section mb-4">
        <div class="meta-label mb-2">Description</div>
        <div class="description-card">
          <p class="task-desc mb-0">
            {{ normalizedTask.description || 'Task này chưa có mô tả.' }}
          </p>
        </div>
      </div>

      <!-- Tổng quan (Overview) -->
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
            <span class="overview-value text-end text-truncate">{{ normalizedTask.pageTitle }}</span>
          </div>
        </div>
      </div>
      
    </div>
  </aside>
</template>

<script setup>
import { computed } from 'vue'
import { getPriorityLabel, getStatusLabel } from '@/utils/task.utils'

const props = defineProps({
  task: {
    type: Object,
    required: true
  }
})

defineEmits(['close'])

const normalizedTask = computed(() => {
  const t = props.task || {}
  return {
    pageTitle: t.pageTitle || 'Workspace Task',
    title: t.title || 'Untitled Task',
    description: t.description || '',
    priority: getPriorityLabel(t.priority), 
    status: getStatusLabel(t.status),       
    dueDate: t.dueDate ? new Date(t.dueDate).toLocaleDateString() : '', 
    assignee: {
      name: t?.assignee?.name || t.assigneeName || 'Unknown',
      avatar: t?.assignee?.avatar || t.assigneeAvatar || ''
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
  right: 0; /* ĐÃ SỬA: Neo dính sát lề phải màn hình */
  bottom: 0;
  width: 400px;
  z-index: 1045; /* Đảm bảo đè lên trên SidebarRight (z-index 1040) */
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.025), rgba(255, 255, 255, 0.015)),
    #0a0a0a;
  color: #e7e5e4;
  box-shadow: -12px 0 40px rgba(0, 0, 0, 0.5);
  animation: slideInRight 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes slideInRight {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}

.border-outline-variant { border-color: rgba(255, 255, 255, 0.1) !important; }

/* ... (Các CSS bên dưới giữ nguyên) ... */
.task-detail-scroll { scrollbar-width: thin; scrollbar-color: rgba(189, 194, 255, 0.28) transparent; }
.task-detail-scroll::-webkit-scrollbar { width: 6px; }
.task-detail-scroll::-webkit-scrollbar-track { background: transparent; }
.task-detail-scroll::-webkit-scrollbar-thumb { background: rgba(189, 194, 255, 0.24); border-radius: 999px; }

.panel-label { font-size: 10px; font-weight: 700; letter-spacing: 0.14em; text-transform: uppercase; color: #818cf8; }
.close-btn { width: 34px; height: 34px; border: 0; border-radius: 8px; background: transparent; color: #acabaa; display: inline-flex; align-items: center; justify-content: center; transition: 0.2s ease; }
.close-btn:hover { background: rgba(255, 255, 255, 0.1); color: #fff; }

.task-breadcrumb { font-size: 11px; color: #8e8d8c; margin-bottom: 8px; }
.task-title { font-size: 28px; line-height: 1.25; font-weight: 700; color: #f3f4f6; }

.tag-pill { padding: 4px 10px; border-radius: 6px; font-size: 10px; font-weight: 700; letter-spacing: 0.04em; text-transform: uppercase; border: 1px solid rgba(255, 255, 255, 0.1); }
.tag-primary { color: #818cf8; background: rgba(129, 140, 248, 0.1); border-color: rgba(129, 140, 248, 0.3); }
.tag-muted { color: #d1d5db; background: rgba(255, 255, 255, 0.05); }

.detail-section { border-top: 1px solid rgba(255, 255, 255, 0.08); padding-top: 18px; }
.meta-label { font-size: 10px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.08em; color: rgba(172, 171, 170, 0.6); }

.meta-card { min-height: 44px; padding: 10px 12px; border-radius: 12px; background: rgba(255, 255, 255, 0.03); border: 1px solid rgba(255, 255, 255, 0.05); }
.meta-value { font-size: 12px; color: #e7e5e4; }

.description-card { padding: 14px; border-radius: 12px; background: rgba(255, 255, 255, 0.03); border: 1px solid rgba(255, 255, 255, 0.05); }
.task-desc { font-size: 13px; line-height: 1.6; color: #d1d5db; }

.mini-avatar { width: 28px; height: 28px; object-fit: cover; border: 1px solid rgba(255, 255, 255, 0.2); }
.mini-avatar-fallback { display: inline-flex; align-items: center; justify-content: center; background: #1f2937; color: #fff; font-size: 11px; font-weight: 700; }
.calendar-icon { font-size: 18px; color: #9ca3af; }

.overview-item { min-height: 40px; padding: 8px 12px; border-radius: 10px; background: rgba(255, 255, 255, 0.02); border: 1px solid rgba(255, 255, 255, 0.04); }
.overview-key { font-size: 11px; color: #9ca3af; }
.overview-value { font-size: 12px; font-weight: 600; color: #e7e5e4; }

.material-symbols-outlined { font-variation-settings: 'FILL' 0, 'wght' 300, 'GRAD' 0, 'opsz' 24; }

@media (max-width: 768px) {
  .task-detail-panel { width: 100%; border-left: none; }
}
</style>