<template>
  <section class="mt-5 pt-5 border-top border-outline-variant-soft">
    <!-- Tiêu đề & Nút Add Task -->
    <div class="d-flex align-items-center justify-content-between mb-4">
      <div class="d-flex align-items-center gap-3">
        <h3 class="text-uppercase tracking-widest text-on-surface-variant fw-bold fs-7 mb-0">Active Tasks</h3>
        <!-- Sửa: Thêm Optional Chaining (?.) để tránh lỗi undefined -->
        <span class="badge rounded-pill bg-dark-container text-on-secondary-container px-3 fw-bold fs-11 border border-outline-variant-soft">
          {{ currentTasks?.length || 0 }} TASKS
        </span>
      </div>
      
      <button 
        v-if="!showCreateTaskForm"
        @click="showCreateTaskForm = true"
        class="btn-text-toggle d-flex align-items-center gap-1"
      >
        <span class="material-symbols-outlined fs-6">add</span> Add Task
      </button>
    </div>

    <!-- Form tạo Task (Inline) -->
    <div v-if="showCreateTaskForm" class="mb-4 fade-in">
      <CreateTaskInlineForm 
        @cancel="showCreateTaskForm = false" 
        @submit="handleCreateTask" 
      />
    </div>

    <!-- Trạng thái Loading -->
    <div v-if="isLoadingTasks" class="text-center py-4 text-muted border border-outline-variant-soft rounded-3">
      <span class="spinner-border spinner-border-sm me-2"></span> Fetching tasks...
    </div>
    
    <!-- Trạng thái Trống (Empty) -->
    <!-- Sửa: Kiểm tra currentTasks có tồn tại không trước khi check length -->
    <div v-else-if="(!currentTasks || currentTasks.length === 0) && !showCreateTaskForm" class="text-center py-5 text-muted border border-outline-variant-soft rounded-3 bg-surface-container opacity-75">
        <span class="material-symbols-outlined fs-1 mb-2 opacity-50">task</span>
        <p class="mb-0 fw-medium">No tasks found. Press "Add Task" to start planning.</p>
    </div>

    <!-- Danh sách Task -->
    <!-- Sửa: Đảm bảo currentTasks tồn tại -->
    <div v-if="currentTasks && currentTasks.length > 0" class="task-list-wrapper rounded-3 overflow-hidden border border-outline-variant-soft">
        <TaskList 
          :tasks="currentTasks" 
          @task-click="handleTaskClick" 
          @update-status="handleUpdateTaskStatus" 
        />
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { storeToRefs } from 'pinia'
import TaskList from '@/components/workspace/TaskList.vue'
import CreateTaskInlineForm from '@/components/workspace/CreateTaskInlineForm.vue'
// Sửa: Nhập từ task.store.ts thay vì workspace.store.ts
import { useTaskStore } from '@/stores/task.store'

const emit = defineEmits(['open-task-detail'])

// Sửa: Khởi tạo taskStore
const taskStore = useTaskStore()
const { currentTasks, isLoadingTasks } = storeToRefs(taskStore)

const showCreateTaskForm = ref(false)

const handleTaskClick = (task: any) => {
  emit('open-task-detail', task)
}

const handleCreateTask = async (formData: any) => {
  const priorityMap: Record<string, number> = { 'Low': 1, 'Medium': 2, 'High': 3 }
  const payload = {
    ...formData,
    priority: priorityMap[formData.priority] || 2
  }

  // Sửa: Gọi createTask từ taskStore
  const success = await taskStore.createTask(payload)
  if (success) {
    showCreateTaskForm.value = false
  }
}

const handleUpdateTaskStatus = async (taskId: number, newStatus: number) => {
  // Sửa: Gọi updateTaskStatus từ taskStore
  await taskStore.updateTaskStatus(taskId, newStatus)
}
</script>

<style scoped>
.text-on-surface-variant { color: #9ca3af; }
.bg-dark-container { background-color: #1f2937; }
.border-outline-variant-soft { border-color: rgba(255, 255, 255, 0.08) !important; }
.bg-surface-container { background-color: #121212; border: 1px solid rgba(255,255,255,0.05); }

.btn-text-toggle {
  background: transparent; border: none; color: #9ca3af;
  font-size: 13px; font-weight: 600; padding: 6px 12px; border-radius: 6px; transition: 0.2s;
}
.btn-text-toggle:hover { background: rgba(255,255,255,0.05); color: #fff; }

.fade-in { animation: fadeIn 0.4s ease forwards; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
</style>