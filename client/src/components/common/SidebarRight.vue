<template>
  <!-- Thêm class động 'open' dựa trên prop isOpen -->
  <aside 
    class="right-sidebar d-flex flex-column border-start border-outline-variant"
    :class="{ 'open': isOpen }"
  >
    <div class="p-4 border-bottom border-outline-variant d-flex align-items-center justify-content-between">
      <div>
        <h2 class="sidebar-title mb-1">Collaborators</h2>
        <p class="sidebar-subtitle mb-0">{{ activeUsers.length }} Online Now</p>
      </div>
      <!-- Nút đóng Sidebar -->
      <button @click="$emit('close')" class="btn-close-custom d-flex align-items-center justify-content-center">
        <span class="material-symbols-outlined fs-5 text-muted">close</span>
      </button>
    </div>

    <div class="flex-grow-1 overflow-auto p-4 custom-scrollbar">
      <div v-if="isLoading" class="text-center py-4">
        <span class="spinner-border spinner-border-sm text-secondary"></span>
      </div>

      <div v-else class="mb-5 fade-in">
        <div class="section-label text-online mb-3">Online Workspace</div>
        <div v-if="activeUsers.length === 0" class="text-muted small">No one is here right now.</div>

        <div class="d-flex flex-column gap-3">
          <div v-for="user in activeUsers" :key="user.userId" class="d-flex align-items-center gap-3">
            <div class="position-relative">
               <div class="mini-avatar rounded-circle">
                 {{ user.userName.charAt(0).toUpperCase() }}
               </div>
              <span class="online-dot"></span>
            </div>
            <div class="d-flex flex-column">
              <span class="user-name">{{ user.userName }}</span>
              <span class="user-status text-online">Active</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </aside>
  
  <!-- Lớp phủ tối màu mờ ảo (Overlay) khi Sidebar Right mở trên màn hình nhỏ -->
  <div 
    v-if="isOpen" 
    class="sidebar-overlay d-lg-none"
    @click="$emit('close')"
  ></div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useWorkspaceStore } from '@/stores/workspace.store'
import { PresenceAPI } from '@/api/presence.api'

// Khai báo Props (Nhận lệnh từ AppLayout) và Emits (Báo lại cho AppLayout)
defineProps({
  isOpen: {
    type: Boolean,
    default: false
  }
})
defineEmits(['close'])

const workspaceStore = useWorkspaceStore()
const { currentWorkspaceId } = storeToRefs(workspaceStore)

const activeUsers = ref<any[]>([])
const isLoading = ref(false)
let pollingInterval: any = null

const fetchActiveUsers = async (workspaceId: number) => {
  if (!workspaceId) return
  try {
    const response: any = await PresenceAPI.getActiveUsers(workspaceId)
    if (Array.isArray(response)) {
        activeUsers.value = response
    } else if (response.data) {
        activeUsers.value = response.data
    }
  } catch (error) {
    console.error('Lỗi khi lấy danh sách user online:', error)
    activeUsers.value = []
  }
}

watch(currentWorkspaceId, (newId) => { if (newId) fetchActiveUsers(newId) })

onMounted(() => {
  if (currentWorkspaceId.value) fetchActiveUsers(currentWorkspaceId.value)
  pollingInterval = setInterval(() => {
    if (currentWorkspaceId.value) fetchActiveUsers(currentWorkspaceId.value)
  }, 10000)
})

onUnmounted(() => { if (pollingInterval) clearInterval(pollingInterval) })
</script>

<style scoped>
.right-sidebar {
  width: 320px;
  background: #0a0a0a;
  color: #e7e5e4;
  position: fixed;
  top: 0;
  right: 0;
  height: 100vh;
  z-index: 1040;
  /* CSS Hiệu ứng trượt ra ngoài màn hình bên phải */
  transform: translateX(100%); 
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

/* Khi có class 'open', sidebar sẽ trượt vào màn hình */
.right-sidebar.open {
  transform: translateX(0);
}

.btn-close-custom {
  background: transparent; border: none; border-radius: 8px; width: 32px; height: 32px; transition: 0.2s;
}
.btn-close-custom:hover { background: rgba(255,255,255,0.1); }
.btn-close-custom:hover span { color: #fff !important; }

.sidebar-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 1035; backdrop-filter: blur(2px);
}

.sidebar-title { font-size: 12px; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; color: #e7e5e4; }
.sidebar-subtitle { font-size: 11px; color: #acabaa; }
.section-label { font-size: 10px; font-weight: 700; letter-spacing: 0.14em; text-transform: uppercase; }
.text-online { color: #c180ff; }
.mini-avatar { width: 36px; height: 36px; background: #1f2020; display: flex; align-items: center; justify-content: center; font-weight: bold; color: #e7e5e4; border: 1px solid rgba(255,255,255,0.1); }
.user-name { font-size: 13px; font-weight: 600; color: #e7e5e4; }
.user-status { font-size: 11px; }
.online-dot { position: absolute; right: -2px; bottom: -2px; width: 12px; height: 12px; border-radius: 50%; background-color: #22c55e; border: 2px solid #000; }
.border-outline-variant { border-color: rgba(72, 72, 72, 0.25) !important; }
.fade-in { animation: fadeIn 0.3s forwards; }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

.custom-scrollbar::-webkit-scrollbar { width: 6px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: #333; border-radius: 4px; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: #555; }
</style>