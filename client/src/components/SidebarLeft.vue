<script setup>
import { ref, onMounted } from 'vue';
import { storeToRefs } from 'pinia';
import { useWorkspaceStore } from '@/stores/workspace.store';

// Lấy dữ liệu và hàm từ Store
const workspaceStore = useWorkspaceStore();
const { workspaces, isLoading, currentWorkspaceId } = storeToRefs(workspaceStore);

const isCollapsed = ref(false);
const isProjectsOpen = ref(true);

onMounted(() => {
  // Tải danh sách workspace khi component khởi tạo
  workspaceStore.fetchMyWorkspaces();
});

const expandSidebar = () => { if (isCollapsed.value) isCollapsed.value = false; };
const toggleProjects = () => {
  if (isCollapsed.value) { isCollapsed.value = false; isProjectsOpen.value = true; }
  else { isProjectsOpen.value = !isProjectsOpen.value; }
};
const createProject = () => console.log('Main add');
</script>

<template>
  <!-- ... Các phần header và search giữ nguyên ... -->
  
  <div class="px-2 py-1 text-muted fw-bold mt-2" style="font-size: 0.65rem; letter-spacing: 0.05em;">
    WORKSPACES
  </div>

  <!-- Loading state -->
  <div v-if="isLoading" class="text-center py-2">
    <span class="spinner-border spinner-border-sm text-secondary" role="status"></span>
  </div>

  <!-- Render danh sách workspace động -->
  <ul v-else class="list-unstyled d-flex flex-column gap-1">
    <li 
      v-for="ws in workspaces" 
      :key="ws.id"
      @click="workspaceStore.setCurrentWorkspace(ws.id)"
      class="p-2 rounded-2 nav-item-hover cursor-pointer d-flex align-items-center gap-2"
      :class="currentWorkspaceId === ws.id ? 'text-white bg-white bg-opacity-10' : 'text-muted'"
    >
      <span class="material-symbols-outlined fs-6">grid_view</span>
      <span v-if="!isCollapsed" class="small fw-medium">{{ ws.name }}</span>
      <span v-if="!isCollapsed && ws.currentUserRole === 'Owner'" class="badge bg-dark ms-auto" style="font-size: 0.5rem">OWNER</span>
    </li>
  </ul>
  
  <!-- ... Phần còn lại giữ nguyên ... -->
</template>