<template>
  <div class="app-wrapper bg-surface-lowest text-on-surface d-flex vh-100 vw-100 overflow-hidden">
    <!-- 1. THANH BÊN TRÁI: ĐIỀU HƯỚNG & QUẢN LÝ WORKSPACE -->
    <SidebarLeft @open-create-workspace="showCreateWorkspace = true" />
    
    <!-- 2. KHU VỰC NỘI DUNG CHÍNH (MAIN CONTENT) -->
    <div class="flex-grow-1 d-flex flex-column position-relative overflow-auto">
      
      <!-- 2.1 Header: Breadcrumb & Thanh công cụ -->
      <header class="top-nav navbar sticky-top px-4 border-bottom border-outline-variant" style="z-index: 10;">
        <div class="container-fluid d-flex justify-content-between align-items-center">
          <nav class="d-flex align-items-center small text-on-surface-variant fw-medium gap-2 fade-in">
            <span class="cursor-pointer hover-white">Workspaces</span>
            <span class="divider">/</span>
            <!-- Hiển thị tên workspace động -->
            <span class="text-on-surface fw-bold">
              {{ activeWorkspace ? activeWorkspace.name : 'Select a Workspace' }}
            </span>
          </nav>

          <div class="d-flex align-items-center gap-3">
            <button class="btn btn-share d-flex align-items-center gap-2 py-1 px-3">
              <span class="material-symbols-outlined fs-7">share</span>
              <span class="fw-medium small">Share</span>
            </button>
            <div class="d-flex align-items-center gap-1">
              <button class="btn btn-icon-ghost"><span class="material-symbols-outlined">star</span></button>
              <button class="btn btn-icon-ghost"><span class="material-symbols-outlined">history</span></button>
              
              <!-- SỬA NÚT NÀY THÀNH NÚT BẬT/TẮT COLLABORATORS -->
              <button 
                @click="isRightSidebarOpen = !isRightSidebarOpen" 
                class="btn btn-icon-ghost position-relative"
                :class="{ 'bg-active': isRightSidebarOpen }"
                title="Collaborators"
              >
                <span class="material-symbols-outlined">group</span>
                <!-- Dấu chấm xanh báo có người online -->
                <span class="position-absolute top-0 start-100 translate-middle p-1 bg-success border border-dark rounded-circle"></span>
              </button>

            </div>
          </div>
        </div>
      </header>

      <main class="content-container w-100 mx-auto px-4 pb-5">
        
        <!-- 2.2 TRẠNG THÁI: CHƯA CHỌN WORKSPACE (EMPTY STATE) -->
        <div v-if="!activeWorkspace" class="empty-state-container d-flex flex-column align-items-center justify-content-center h-100 text-center text-muted fade-in mt-5 pt-5">
          <div class="empty-icon-wrap mb-4 d-flex align-items-center justify-content-center rounded-4 bg-surface-container">
            <span class="material-symbols-outlined display-4 text-primary-custom">architecture</span>
          </div>
          <h2 class="fw-bold text-on-surface mb-2">Welcome to Block Paged</h2>
          <p class="mb-4 text-on-surface-variant" style="max-width: 400px;">
            Select a workspace from the sidebar to view tasks and notes, or create a new one to start your journey.
          </p>
          <button @click="showCreateWorkspace = true" class="btn btn-primary-solid px-4 py-2 rounded-3 fw-bold d-flex align-items-center gap-2">
            <span class="material-symbols-outlined fs-5">add</span> Create Workspace
          </button>
        </div>

        <!-- 2.3 TRẠNG THÁI: ĐÃ CHỌN WORKSPACE -->
        <div v-else class="fade-in">
          
          <!-- Banner & Tiêu đề Workspace -->
          <section class="mt-5 header-group">
            <div class="hero-banner position-relative rounded-4 overflow-hidden mb-4 bg-surface-container">
              <img 
                class="w-100 h-100 object-fit-cover opacity-60" 
                src="https://lh3.googleusercontent.com/aida-public/AB6AXuCHKgnShiuxNCbtthBDWD8gAFXRJpTZ6RSiyXj8grAA7xXANbnEqoJDaOm-T0F5Oly5mV5BfZ-4RF4OcGDNAenyjt6l3uXNK_-Nu8kXj0iV1KHBY2M9JtvkLYU9rmF0kxcqZgW81LnThknzajWEqhSUPF-CPF9lz0dXCvmY_MzCoDG3nGxFcHTUnGxuCUAT6MU1reEH-nJcQpGji2uFsiuw-n9B9fTbCXXmiRDM7iXHl4sL3QBtg_aJj55ILPnOUqlD-kPmqJ94Ulo" 
                alt="Hero Background"
              />
              <div class="banner-overlay position-absolute top-0 start-0 w-100 h-100"></div>
            </div>

            <div class="emoji-box d-flex align-items-center justify-content-center bg-surface-container rounded-3 mb-3 fs-2 shadow-sm">
              🚀
            </div>

            <h1 class="display-5 fw-extrabold text-on-surface tracking-tighter mb-2">
              {{ activeWorkspace.name }}
            </h1>
            
            <p class="text-on-surface-variant small d-flex align-items-center gap-2 fw-medium">
              <span>{{ activeWorkspace.description || 'No description provided.' }}</span>
              <span class="dot-separator"></span>
              <span class="text-primary-custom d-flex align-items-center gap-1">
                <span class="material-symbols-outlined fs-7">shield_person</span> Workspace Owner
              </span>
            </p>
          </section>

          <!-- Danh sách Công việc (Active Tasks) -->
          <section class="mt-5 pt-5 border-top border-outline-variant-soft">
            <div class="d-flex align-items-center justify-content-between mb-4">
              <div class="d-flex align-items-center gap-3">
                <h3 class="text-uppercase tracking-widest text-on-surface-variant fw-bold fs-7 mb-0">Active Tasks</h3>
                <span class="badge rounded-pill bg-dark-container text-on-secondary-container px-3 fw-bold fs-11 border border-outline-variant-soft">
                  {{ currentTasks.length }} TASKS
                </span>
              </div>
              
              <!-- Nút mở Form tạo Task -->
              <button 
                v-if="!showCreateTaskForm"
                @click="showCreateTaskForm = true"
                class="btn-text-toggle d-flex align-items-center gap-1"
              >
                <span class="material-symbols-outlined fs-6">add</span> Add Task
              </button>
            </div>
              
            <!-- Form hiển thị nội tuyến (Inline Form) -->
            <div v-if="showCreateTaskForm" class="mb-4 fade-in">
              <CreateTaskInlineForm 
                @cancel="showCreateTaskForm = false" 
                @submit="handleCreateTask" 
              />
            </div>

            <!-- Trạng thái Loading Tasks -->
            <div v-if="isLoadingTasks" class="text-center py-4 text-muted border border-outline-variant-soft rounded-3">
              <span class="spinner-border spinner-border-sm me-2"></span> Fetching tasks...
            </div>
            
            <!-- Trạng thái Không có Task -->
            <div v-else-if="currentTasks.length === 0 && !showCreateTaskForm" class="text-center py-5 text-muted border border-outline-variant-soft rounded-3 bg-surface-container opacity-75">
               <span class="material-symbols-outlined fs-1 mb-2 opacity-50">task</span>
               <p class="mb-0 fw-medium">No tasks found. Press "Add Task" to start planning.</p>
            </div>

            <!-- Khối Render Danh sách Task -->
            <div v-if="currentTasks.length > 0" class="task-list-wrapper rounded-3 overflow-hidden border border-outline-variant-soft">
               <TaskList 
                 :tasks="currentTasks" 
                 @task-click="handleTaskClick" 
                 @update-status="handleUpdateTaskStatus" 
               />
            </div>
          </section>

          <!-- Vùng soạn thảo EditorJS (Workspace Notes) -->
          <section class="mt-5 pt-5 border-top border-outline-variant-soft mb-5">
            <div class="d-flex align-items-center gap-2 mb-2">
              <span class="material-symbols-outlined text-primary-custom fs-5">edit_document</span>
              <h3 class="text-uppercase tracking-widest text-on-surface-variant fw-bold fs-7 mb-0">Workspace Notes</h3>
            </div>
            <p class="text-muted small mb-4">Use '/' for commands. Changes are autosaved.</p>
            
            <!-- Nhúng EditorJS Component -->
            <PageNotesSection v-model="editorData" />
          </section>

        </div>
      </main>

      <button class="fab-btn position-fixed bottom-0 end-0 m-4 rounded-circle border-0 d-flex align-items-center justify-content-center shadow-lg">
        <span class="material-symbols-outlined fs-2">smart_toy</span>
      </button>
    </div>
    
    <!-- 3. THANH BÊN PHẢI: Truyền prop và nhận sự kiện đóng -->
    <SidebarRight 
      :is-open="isRightSidebarOpen" 
      @close="isRightSidebarOpen = false" 
    />

    <!-- 4. MODALS & PANELS (Lớp phủ lên UI) -->
    
    <!-- Panel chi tiết Task (Slide over) -->
    <TaskDetail 
      v-if="selectedTask" 
      :task="selectedTask" 
      @close="handleCloseTaskDetail" 
    />

    <!-- Modal Tạo Workspace -->
    <CreateWorkspaceModal
      v-model="showCreateWorkspace"
      @submit="handleCreateWorkspace"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { storeToRefs } from 'pinia'

// Import Components
import SidebarLeft from '@/components/SidebarLeft.vue'
import SidebarRight from '@/components/SidebarRight.vue'
import TaskList from '@/components/workspace/TaskList.vue'
import CreateWorkspaceModal from '@/components/workspace/CreateWorkspaceModal.vue'
import CreateTaskInlineForm from '@/components/workspace/CreateTaskInlineForm.vue'
import PageNotesSection from '@/components/workspace/PageNotesSection.vue'
import TaskDetail from '@/components/TaskDetail.vue'

// Import Store & API
import { useWorkspaceStore } from '@/stores/workspace.store'
import { WorkspaceAPI } from '@/api/workspace.api'

const workspaceStore = useWorkspaceStore()
const { activeWorkspace, currentTasks, isLoadingTasks } = storeToRefs(workspaceStore)

const showCreateWorkspace = ref(false)
const showCreateTaskForm = ref(false)
const selectedTask = ref<any>(null)
const isRightSidebarOpen = ref(false)

const editorData = ref({
  time: Date.now(),
  blocks: [
    // Chỉ cần để sẵn một Header, sau đó người dùng enter xuống dòng là tự có placeholder
    { type: 'header', data: { text: 'Workspace Documentation', level: 2 } }
  ]
})


const handleTaskClick = (task: any) => {
  selectedTask.value = task
}

const handleCloseTaskDetail = () => {
  selectedTask.value = null
}

const handleCreateWorkspace = async (payload: { name: string; description?: string }) => {
  try {
    const response: any = await WorkspaceAPI.create(payload)
    if (response.isSuccess) {
      showCreateWorkspace.value = false
      await workspaceStore.fetchMyWorkspaces()
      if (response.data && response.data.id) {
        workspaceStore.setCurrentWorkspace(response.data.id)
      }
    }
  } catch (error) {
    console.error('Lỗi khi tạo Workspace:', error)
  }
}

const handleCreateTask = async (formData: any) => {
  
  const priorityMap: Record<string, number> = { 'Low': 1, 'Medium': 2, 'High': 3 }
  const payload = {
    ...formData,
    priority: priorityMap[formData.priority] || 2
  }

  const success = await workspaceStore.createTask(payload)
  if (success) {
    showCreateTaskForm.value = false
  }
}
const handleUpdateTaskStatus = async (taskId: number, newStatus: number) => {
  await workspaceStore.updateTaskStatus(taskId, newStatus)
}
</script>

<style scoped>
/* Màu sắc chủ đạo (Duy trì tính nhất quán) */
.bg-surface-lowest { background-color: #000000; }
.bg-surface-container { background-color: #121212; border: 1px solid rgba(255,255,255,0.05); }
.text-on-surface { color: #f3f4f6; }
.text-on-surface-dim { color: #d1d5db; }
.text-on-surface-variant { color: #9ca3af; }
.text-primary-custom { color: #818cf8; }
.bg-primary-soft { background-color: rgba(129, 140, 248, 0.1); }
.border-outline-variant { border-color: rgba(255, 255, 255, 0.1) !important; }
.border-outline-variant-soft { border-color: rgba(255, 255, 255, 0.08) !important; }
.bg-dark-container { background-color: #1f2937; }

.app-wrapper {
  min-height: 100vh;
  font-family: 'Inter', sans-serif;
  letter-spacing: -0.01em;
}

.content-container { 
  max-width: 860px;
}

.top-nav {
  background-color: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(24px);
  height: 56px;
}

.btn-share {
  background-color: rgba(255,255,255,0.05);
  color: #e5e7eb;
  border: 1px solid rgba(255,255,255,0.08);
  transition: 0.2s ease;
}
.btn-share:hover { background-color: rgba(255,255,255,0.1); color: #fff; }

.btn-icon-ghost { color: #9ca3af; padding: 6px; border: none; background: transparent; transition: 0.2s; border-radius: 8px;}
.btn-icon-ghost:hover { background-color: rgba(255,255,255,0.08); color: #fff; }

.btn-text-toggle {
  background: transparent;
  border: none;
  color: #9ca3af;
  font-size: 13px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 6px;
  transition: 0.2s;
}
.btn-text-toggle:hover { background: rgba(255,255,255,0.05); color: #fff; }

/* Các phần tử tĩnh */
.hero-banner { height: 200px; border: 1px solid rgba(255,255,255,0.05); }
.banner-overlay { background: linear-gradient(to top, #000000, transparent); }
.emoji-box { width: 54px; height: 54px; margin-top: -30px; position: relative; z-index: 2; background: #1a1a1a;}
.empty-icon-wrap { width: 80px; height: 80px; }

/* Nút & Badges */
.btn-primary-solid {
  background: #fff;
  color: #000;
  border: none;
  transition: 0.2s;
}
.btn-primary-solid:hover { background: #e5e7eb; transform: translateY(-1px); }

.fab-btn {
  width: 56px;
  height: 56px;
  background: linear-gradient(135deg, #6366f1, #4338ca);
  color: #fff;
  transition: 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  z-index: 50;
  box-shadow: 0 10px 25px -5px rgba(99, 102, 241, 0.4);
}
.fab-btn:hover { transform: scale(1.08) translateY(-4px); box-shadow: 0 15px 30px -5px rgba(99, 102, 241, 0.6); }

/* Typography & Helpers */
.cursor-pointer { cursor: pointer; }
.hover-white:hover { color: #fff; }
.tracking-tighter { letter-spacing: -0.04em; }
.tracking-widest { letter-spacing: 0.08em; }
.fs-7 { font-size: 14px; }
.fs-11 { font-size: 11px; }
.fw-extrabold { font-weight: 800; }
.dot-separator { width: 4px; height: 4px; border-radius: 50%; background-color: #4b5563; }

/* Animation */
.fade-in { animation: fadeIn 0.4s ease forwards; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
.bg-active { background-color: rgba(255,255,255,0.15) !important; color: #fff !important; }
.transition-300 { transition: padding-right 0.3s cubic-bezier(0.4, 0, 0.2, 1); }

/* Khi Sidebar Right mở, nội dung main sẽ được ép sang trái để không bị che khuất */
@media (min-width: 1200px) {
  .shifted-left {
    padding-right: 340px !important; /* Độ rộng Sidebar (320px) + một chút khoảng cách */
  }
}
</style>