import { defineStore } from 'pinia'
import { ref } from 'vue'
import { WorkTaskAPI } from '@/api/work-task.api'
import { useWorkspaceStore } from './workspace.store'

export const useTaskStore = defineStore('task', () => {
  const workspaceStore = useWorkspaceStore()
  
  const currentTasks = ref<any[]>([])
  const isLoadingTasks = ref(false)

  const fetchTasksByWorkspace = async (workspaceId: number) => {
    isLoadingTasks.value = true
    try {
      const response: any = await WorkTaskAPI.getByWorkspace(workspaceId, { pageNumber: 1, pageSize: 100 })
      if (response.isSuccess && response.data) {
        currentTasks.value = response.data.items || []
      }
    } catch (error) {
      console.error('Lỗi khi lấy danh sách tasks:', error)
      currentTasks.value = []
    } finally {
      isLoadingTasks.value = false
    }
  }


  const createTask = async (taskData: any) => {
    // SỬA: Gọi currentWorkspaceId thông qua workspaceStore
    if (!workspaceStore.currentWorkspaceId) return false;
    
    try {
      const response: any = await WorkTaskAPI.create({
        title: taskData.title,
        workspaceId: workspaceStore.currentWorkspaceId,
        createdById: 1,
        priority: taskData.priority 
      })

      if (response.isSuccess) {
        fetchTasksByWorkspace(workspaceStore.currentWorkspaceId)
        return true
      }
      return false
    } catch (error) {
      console.error('Lỗi khi tạo task:', error)
      return false
    }
  }

  const updateTaskStatus = async (taskId: number, newStatus: number) => {
    const taskIndex = currentTasks.value.findIndex(t => t.id === taskId)
    if (taskIndex === -1) return false

    const task = currentTasks.value[taskIndex]
    const oldStatus = task.status

    try {
      task.status = newStatus

      let response: any;

      if (newStatus === 3) {
        response = await WorkTaskAPI.complete(taskId);
      } else {
        response = await WorkTaskAPI.reopen(taskId);
      }

      if (response && response.isSuccess === false) {
        task.status = oldStatus
        console.error('Lỗi khi cập nhật trạng thái task:', response.message)
        return false
      }
      
      return true
    } catch (error) {
      task.status = oldStatus
      console.error('Lỗi Exception khi cập nhật trạng thái task:', error)
      return false
    }
  }

  return {
    currentTasks,
    isLoadingTasks,
    fetchTasksByWorkspace,
    createTask,
    updateTaskStatus
  }
})