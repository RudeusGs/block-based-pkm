import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { WorkspaceAPI } from '@/api/workspace.api'
import { useTaskStore } from './task.store'

export const useWorkspaceStore = defineStore('workspace', () => {
  const workspaces = ref<any[]>([])
  const isLoading = ref(false)
  const currentWorkspaceId = ref<number | null>(null)

  const activeWorkspace = computed(() => {
    if (!currentWorkspaceId.value) return null
    return workspaces.value.find(ws => ws.id === currentWorkspaceId.value) || null
  })

  const fetchMyWorkspaces = async () => {
    isLoading.value = true
    try {
      const response: any = await WorkspaceAPI.getMyWorkspaces({ pageNumber: 1, pageSize: 50 })
      if (response.isSuccess && response.data) {
        workspaces.value = response.data.items || []
        
        if (workspaces.value.length > 0 && !currentWorkspaceId.value) {
          setCurrentWorkspace(workspaces.value[0].id)
        }
      }
    } catch (error) {
      console.error('Lỗi khi lấy danh sách workspace:', error)
    } finally {
      isLoading.value = false
    }
  }

  const setCurrentWorkspace = (id: number) => {
    currentWorkspaceId.value = id
    const taskStore = useTaskStore()
    taskStore.fetchTasksByWorkspace(id)
  }

  return {
    workspaces,
    isLoading,
    currentWorkspaceId,
    activeWorkspace,
    fetchMyWorkspaces,
    setCurrentWorkspace
  }
})