<template>
  <aside 
    class="sidebar-left d-flex flex-column py-4 px-3 border-end border-outline-variant transition-300"
    :class="{ 'collapsed': isCollapsed, 'sidebar-clickable': isCollapsed }"
    @click="expandSidebar"
  >
    <!-- Header: Logo & Toggle -->
    <div class="sidebar-header d-flex align-items-center mb-4 px-2" :class="isCollapsed ? 'justify-content-center' : 'justify-content-between'">
      <div class="d-flex align-items-center overflow-hidden">
        <div class="logo-box rounded-3 bg-dark d-flex align-items-center justify-content-center text-white flex-shrink-0">
          <span class="material-symbols-outlined fill-1">grid_view</span>
        </div>
        <div v-if="!isCollapsed" class="d-flex flex-column overflow-hidden ms-3 fade-in">
          <span class="fs-6 fw-bold text-light text-nowrap">Block-based</span>
          <span class="text-uppercase text-on-surface-variant tracking-widest" style="font-size: 10px;">Pro Workspace</span>
        </div>
      </div>

      <button 
        v-if="!isCollapsed" 
        @click.stop="isCollapsed = true" 
        class="btn-text-toggle fade-in flex-shrink-0"
      >
        &lt;&lt;
      </button>
    </div>

    <!-- Thanh tìm kiếm -->
    <div class="mb-4 px-1">
      <div class="search-wrapper d-flex align-items-center rounded-4 border border-outline-variant transition-200" :class="isCollapsed ? 'justify-content-center py-2' : 'px-3 py-2 gap-2'">
        <span class="material-symbols-outlined text-on-surface-variant fs-6 flex-shrink-0">search</span>
        <input 
          v-if="!isCollapsed" 
          type="text" 
          class="search-input bg-transparent border-0 flex-grow-1 text-white fs-7 fade-in" 
          placeholder="Search..."
        >
      </div>
    </div>

    <!-- Khu vực cuộn chính: Workspaces -->
    <nav class="flex-grow-1 overflow-x-hidden overflow-y-auto scrollbar-hide">
      <div class="px-1 pb-2">
        
        <!-- Header danh sách Workspace -->
        <div class="d-flex align-items-center justify-content-between mb-2 mt-2 px-2">
          <span v-if="!isCollapsed" class="text-on-surface-variant fw-bold" style="font-size: 0.65rem; letter-spacing: 0.05em;">WORKSPACES</span>
          <button v-if="!isCollapsed" @click.stop="$emit('open-create-workspace')" class="btn-add-small fade-in ms-auto">
            <span class="material-symbols-outlined">add</span>
          </button>
        </div>

        <!-- Trạng thái Loading -->
        <div v-if="isLoading" class="text-center py-3 fade-in">
          <span class="spinner-border spinner-border-sm text-secondary" role="status"></span>
        </div>

        <!-- Render danh sách thật từ Backend -->
        <ul v-else class="list-unstyled d-flex flex-column gap-1 m-0 fade-in">
          <li 
            v-for="ws in workspaces" 
            :key="ws.id"
            @click.stop="workspaceStore.setCurrentWorkspace(ws.id)"
            class="nav-link-custom d-flex align-items-center rounded-3 cursor-pointer p-2"
            :class="[
              isCollapsed ? 'justify-content-center' : 'gap-2', 
              currentWorkspaceId === ws.id ? 'bg-white bg-opacity-10 text-white' : 'text-on-surface-variant'
            ]"
          >
            <span class="material-symbols-outlined flex-shrink-0 fs-6">folder</span>
            <span v-if="!isCollapsed" class="text-truncate flex-grow-1 fs-7 fw-medium">{{ ws.name }}</span>
          </li>
        </ul>

        <!-- Task Menu cứng (Có thể dev sau) -->
        <div class="d-flex align-items-center nav-link-custom position-relative mt-4">
          <button class="border-0 d-flex align-items-center bg-transparent text-inherit w-100" :class="isCollapsed ? 'justify-content-center p-0' : 'gap-2 py-2 ps-3'">
            <span class="material-symbols-outlined flex-shrink-0 text-white folder-icon">task_alt</span>
            <span v-if="!isCollapsed" class="fade-in text-start flex-grow-1 ms-1 fw-medium text-white">My Tasks</span>
          </button>
        </div>
      </div>
    </nav>

    <!-- Khu vực Footer: User & Settings & Logout -->
    <div class="mt-auto px-1 pb-2 pt-3 border-top border-outline-variant">
      <div class="d-flex flex-column gap-1">
        <a href="#" class="nav-link-custom d-flex align-items-center rounded-3" :class="isCollapsed ? 'justify-content-center py-2' : 'gap-3 py-2 ps-3'">
          <span class="material-symbols-outlined flex-shrink-0">account_circle</span>
          <span v-if="!isCollapsed" class="fade-in fs-7">Profile</span>
        </a>
        <a href="#" class="nav-link-custom d-flex align-items-center rounded-3" :class="isCollapsed ? 'justify-content-center py-2' : 'gap-3 py-2 ps-3'">
          <span class="material-symbols-outlined flex-shrink-0">settings</span>
          <span v-if="!isCollapsed" class="fade-in fs-7">Settings</span>
        </a>
        <!-- Nút Logout -->
        <a href="#" @click.prevent="handleLogout" class="nav-link-custom d-flex align-items-center rounded-3 text-danger mt-1" :class="isCollapsed ? 'justify-content-center py-2' : 'gap-3 py-2 ps-3'">
          <span class="material-symbols-outlined flex-shrink-0">logout</span>
          <span v-if="!isCollapsed" class="fade-in fs-7 fw-medium">Logout</span>
        </a>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { storeToRefs } from 'pinia';
import { useWorkspaceStore } from '@/stores/workspace.store';
import { useAuthStore } from '@/stores/auth.store';

// Định nghĩa emit để AppLayout bắt được sự kiện mở Modal
defineEmits(['open-create-workspace']);

// Khởi tạo Stores
const workspaceStore = useWorkspaceStore();
const authStore = useAuthStore();

const { workspaces, isLoading, currentWorkspaceId } = storeToRefs(workspaceStore);

const isCollapsed = ref(false);

onMounted(() => {
  // Tải danh sách workspace khi component render
  workspaceStore.fetchMyWorkspaces();
});

const expandSidebar = () => { 
  if (isCollapsed.value) isCollapsed.value = false; 
};

// Gọi hàm đăng xuất từ store
const handleLogout = () => {
  authStore.logout();
};
</script>

<style scoped src="./css/SidebarLeft.css"></style>