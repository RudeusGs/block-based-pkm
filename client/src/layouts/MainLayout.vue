<template>
  <div class="app-shell">
    <SidebarLeft
      :collapsed="isSidebarCollapsed"
      @toggle="isSidebarCollapsed = !isSidebarCollapsed"
      @open-create-workspace="showCreateWorkspace = true"
    />

    <main
      class="app-content"
      :class="{ collapsed: isSidebarCollapsed }"
    >
      <RouterView />
    </main>

    <SidebarRight />

    <CreateWorkspaceModal
      v-model="showCreateWorkspace"
      @submit="handleCreateWorkspace"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { RouterView } from 'vue-router'
import SidebarLeft from '@/components/SidebarLeft.vue'
import SidebarRight from '@/components/SidebarRight.vue'
import CreateWorkspaceModal from '@/components/workspace/CreateWorkspaceModal.vue'

const isSidebarCollapsed = ref(false)
const showCreateWorkspace = ref(false)

function handleCreateWorkspace(payload) {
  console.log('Create workspace payload:', payload)
  showCreateWorkspace.value = false
}
</script>

<style scoped>
.app-shell {
  min-height: 100vh;
  background-color: #000000;
  position: relative;
  overflow-x: hidden;
}

.app-content {
  min-height: 100vh;
  padding-left: 300px;
  padding-right: 320px;
  box-sizing: border-box;
  background-color: #000000;
  transition: padding-left 0.25s ease;
}

.app-content.collapsed {
  padding-left: 88px;
}

@media (max-width: 1200px) {
  .app-content,
  .app-content.collapsed {
    padding-right: 0;
  }
}

@media (max-width: 992px) {
  .app-content,
  .app-content.collapsed {
    padding-left: 0;
    padding-right: 0;
  }
}
</style>