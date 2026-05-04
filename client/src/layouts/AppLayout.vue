<template>
  <div class="app-wrapper bg-surface-lowest text-on-surface d-flex vh-100 vw-100 overflow-hidden">
    <SidebarLeft @open-create-workspace="showCreateWorkspace = true" />
    
    <div class="flex-grow-1 d-flex flex-column position-relative overflow-auto">
      
      <header class="top-nav navbar sticky-top px-4 border-bottom border-outline-variant" style="z-index: 10;">
        <div class="container-fluid d-flex justify-content-between align-items-center">
          <nav class="d-flex align-items-center small text-on-surface-variant fw-medium gap-2 fade-in">
            <span class="cursor-pointer hover-white">Workspaces</span>
            <span class="divider">/</span>
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
              
              <!-- NÚT BẬT/TẮT COLLABORATORS -->
              <button 
                @click="isRightSidebarOpen = !isRightSidebarOpen" 
                class="btn btn-icon-ghost position-relative"
                :class="{ 'bg-active': isRightSidebarOpen }"
                title="Collaborators"
              >
                <span class="material-symbols-outlined">group</span>
                <span class="position-absolute top-0 start-100 translate-middle p-1 bg-success border border-dark rounded-circle"></span>
              </button>
            </div>
          </div>
        </div>
      </header>

      <main 
        class="content-container w-100 mx-auto px-4 pb-5 transition-300"
        :class="{ 'shifted-left': isRightSidebarOpen }"
      >
        <!-- EMPTY STATE -->
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

        <!-- MAIN CONTENT -->
        <div v-else class="fade-in">
          <!-- Banner & Tiêu đề -->
          <section class="mt-5 header-group">
            <div class="hero-banner position-relative rounded-4 overflow-hidden mb-4 bg-surface-container">
              <img 
                class="w-100 h-100 object-fit-cover opacity-60" 
                src="https://lh3.googleusercontent.com/aida-public/AB6AXuCHKgnShiuxNCbtthBDWD8gAFXRJpTZ6RSiyXj8grAA7xXANbnEqoJDaOm-T0F5Oly5mV5BfZ-4RF4OcGDNAenyjt6l3uXNK_-Nu8kXj0iV1KHBY2M9JtvkLYU9rmF0kxcqZgW81LnThknzajWEqhSUPF-CPF9lz0dXCvmY_MzCoDG3nGxFcHTUnGxuCUAT6MU1reEH-nJcQpGji2uFsiuw-n9B9fTbCXXmiRDM7iXHl4sL3QBtg_aJj55ILPnOUqlD-kPmqJ94Ulo" 
                alt="Hero Background"
              />
              <div class="banner-overlay position-absolute top-0 start-0 w-100 h-100"></div>
            </div>
            <div class="emoji-box d-flex align-items-center justify-content-center bg-surface-container rounded-3 mb-3 fs-2 shadow-sm">🚀</div>
            <h1 class="display-5 fw-extrabold text-on-surface tracking-tighter mb-2">{{ activeWorkspace.name }}</h1>
            <p class="text-on-surface-variant small d-flex align-items-center gap-2 fw-medium">
              <span>{{ activeWorkspace.description || 'No description provided.' }}</span>
              <span class="dot-separator"></span>
              <span class="text-primary-custom d-flex align-items-center gap-1">
                <span class="material-symbols-outlined fs-7">shield_person</span> Workspace Owner
              </span>
            </p>
          </section>

          <!-- MODULE: ACTIVE TASKS (Đã được tách ra) -->
          <WorkspaceTasks @open-task-detail="handleTaskClick" />

          <!-- MODULE: EDITOR JS -->
          <section class="mt-5 pt-5 border-top border-outline-variant-soft mb-5">
            <div class="d-flex align-items-center justify-content-between mb-2">
              <div class="d-flex align-items-center gap-2">
                <span class="material-symbols-outlined text-primary-custom fs-5">edit_document</span>
                <h3 class="text-uppercase tracking-widest text-on-surface-variant fw-bold fs-7 mb-0">Workspace Notes</h3>
              </div>
              
              <!-- HIỂN THỊ TRẠNG THÁI AUTOSAVE -->
              <span class="text-muted small d-flex align-items-center gap-1">
                <span v-if="isSaving" class="spinner-border spinner-border-sm" style="width: 12px; height: 12px; border-width: 0.15em;"></span>
                <span v-if="isSaving" class="fst-italic">Saving...</span>
                <span v-else-if="lastSavedTime" class="text-success d-flex align-items-center gap-1">
                  <span class="material-symbols-outlined" style="font-size: 14px;">check_circle</span>
                  Saved {{ lastSavedTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }}
                </span>
              </span>
            </div>
            <p class="text-muted small mb-4">Use '/' for commands. Changes are autosaved.</p>
            
            <PageNotesSection v-model="editorData" />
          </section>
        </div>
      </main>

      <button class="fab-btn position-fixed bottom-0 end-0 m-4 rounded-circle border-0 d-flex align-items-center justify-content-center shadow-lg">
        <span class="material-symbols-outlined fs-2">smart_toy</span>
      </button>
    </div>
    
    <SidebarRight :is-open="isRightSidebarOpen" @close="isRightSidebarOpen = false" />
    <TaskDetail v-if="selectedTask" :task="selectedTask" @close="handleCloseTaskDetail" />
    <CreateWorkspaceModal v-model="showCreateWorkspace" @submit="handleCreateWorkspace" />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { storeToRefs } from 'pinia'

// Import Các UI Layout Components
import SidebarLeft from '@/components/common/SidebarLeft.vue'
import SidebarRight from '@/components/common/SidebarRight.vue'
import TaskDetail from '@/components/common/TaskDetail.vue'
import CreateWorkspaceModal from '@/components/workspace/CreateWorkspaceModal.vue'

// Import Các Module nội dung đã được chia nhỏ
import WorkspaceTasks from '@/components/workspace/WorkspaceTasks.vue' 
import PageNotesSection from '@/components/workspace/PageNotesSection.vue'

// Import Stores & APIs
import { useWorkspaceStore } from '@/stores/workspace.store'
import { WorkspaceAPI } from '@/api/workspace.api'

// Chỉ import CSS của AppLayout
import '@/assets/css/views/AppLayout.css';

// --- SETUP STATE ---
const workspaceStore = useWorkspaceStore()
const { activeWorkspace } = storeToRefs(workspaceStore)

const showCreateWorkspace = ref(false)
const selectedTask = ref<any>(null)
const isRightSidebarOpen = ref(false)

const editorData = ref({
  time: Date.now(),
  blocks: [{ type: 'header', data: { text: 'Workspace Documentation', level: 2 } }]
})

// --- LOGIC AUTOSAVE EDITOR JS ---
const isSaving = ref(false)
const lastSavedTime = ref<Date | null>(null)
let saveTimeout: any = null

watch(() => editorData.value, (newData) => {
  isSaving.value = true
  if (saveTimeout) clearTimeout(saveTimeout)
  
  saveTimeout = setTimeout(async () => {
    try {
      console.log('Auto-saving data to DB...', newData)
      lastSavedTime.value = new Date()
    } catch (error) {
      console.error("Lỗi khi Autosave:", error)
    } finally {
      isSaving.value = false
    }
  }, 1500) 
}, { deep: true })

// --- HANDLERS ---
const handleTaskClick = (task: any) => { selectedTask.value = task }
const handleCloseTaskDetail = () => { selectedTask.value = null }

const handleCreateWorkspace = async (payload: { name: string; description?: string }) => {
  try {
    const response: any = await WorkspaceAPI.create(payload)
    if (response.isSuccess) {
      showCreateWorkspace.value = false
      await workspaceStore.fetchMyWorkspaces()
      if (response.data && response.data.id) workspaceStore.setCurrentWorkspace(response.data.id)
    }
  } catch (error) { console.error('Lỗi khi tạo Workspace:', error) }
}
</script>
