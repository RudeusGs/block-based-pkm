import { defineStore } from 'pinia'
import { ref } from 'vue'
import { WorkspaceAPI } from '@/api/workspace.api'
import type { WorkspaceListItemResponse } from '@/models/workspace.model' // Bạn có thể cần cập nhật model này khớp với backend

export const useWorkspaceStore = defineStore('workspace', () => {
  const workspaces = ref<any[]>([])
  const isLoading = ref(false)
  const currentWorkspaceId = ref<string | null>(null)

  const fetchMyWorkspaces = async () => {
    isLoading.value = true
    try {
      const response: any = await WorkspaceAPI.getMyWorkspaces({ pageNumber: 1, pageSize: 50 })
      if (response.isSuccess && response.data) {
        workspaces.value = response.data.items || []
        
        // Tự động chọn workspace đầu tiên nếu chưa chọn
        if (workspaces.value.length > 0 && !currentWorkspaceId.value) {
          currentWorkspaceId.value = workspaces.value[0].id
        }
      }
    } catch (error) {
      console.error('Lỗi khi lấy danh sách workspace:', error)
    } finally {
      isLoading.value = false
    }
  }

  const setCurrentWorkspace = (id: string) => {
    currentWorkspaceId.value = id
  }

  return {
    workspaces,
    isLoading,
    currentWorkspaceId,
    fetchMyWorkspaces,
    setCurrentWorkspace
  }
})