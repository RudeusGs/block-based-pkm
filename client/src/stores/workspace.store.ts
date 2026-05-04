import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { WorkspaceAPI } from '@/api/workspace.api'
import { WorkTaskAPI } from '@/api/work-task.api'

export const useWorkspaceStore = defineStore('workspace', () => {
  // --- State của Workspace ---
  const workspaces = ref<any[]>([])
  const isLoading = ref(false)
  const currentWorkspaceId = ref<number | null>(null)

  // --- State của Work Tasks ---
  const currentTasks = ref<any[]>([])
  const isLoadingTasks = ref(false)

  // Computed để lấy thông tin chi tiết của Workspace đang chọn
  const activeWorkspace = computed(() => {
    if (!currentWorkspaceId.value) return null
    return workspaces.value.find(ws => ws.id === currentWorkspaceId.value) || null
  })

  // --- Actions ---
  const fetchMyWorkspaces = async () => {
    isLoading.value = true
    try {
      const response: any = await WorkspaceAPI.getMyWorkspaces({ pageNumber: 1, pageSize: 50 })
      if (response.isSuccess && response.data) {
        workspaces.value = response.data.items || []
        
        // Tự động chọn workspace đầu tiên nếu chưa có
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

  // Lấy danh sách Task của một Workspace
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

  // Khi set Workspace hiện tại, tự động gọi API lấy Task của Workspace đó
  const setCurrentWorkspace = (id: number) => {
    currentWorkspaceId.value = id
    fetchTasksByWorkspace(id)
  }

  const createTask = async (taskData: any) => {
    if (!currentWorkspaceId.value) return false;
    
    try {
      // Gọi API tạo mới Task
      const response: any = await WorkTaskAPI.create({
        title: taskData.title,
        workspaceId: currentWorkspaceId.value,
        createdById: 1, // Tạm thời hardcode, thực tế BE sẽ lấy ID từ JWT Token
        priority: taskData.priority // Sẽ truyền enum 1, 2, 3 từ UI
      })

      if (response.isSuccess) {
        // Tải lại danh sách task của workspace hiện tại để thấy task mới
        fetchTasksByWorkspace(currentWorkspaceId.value)
        return true
      }
      return false
    } catch (error) {
      console.error('Lỗi khi tạo task:', error)
      return false
    }
  }

// THÊM HÀM MỚI: Cập nhật Trạng thái Task (bám sát API complete / reopen)
  const updateTaskStatus = async (taskId: number, newStatus: number) => {
    // 1. Tìm task hiện tại
    const taskIndex = currentTasks.value.findIndex(t => t.id === taskId)
    if (taskIndex === -1) return false

    const task = currentTasks.value[taskIndex]
    const oldStatus = task.status

    try {
      // 2. Cập nhật UI ngay lập tức (Optimistic UI Update)
      task.status = newStatus

      let response: any;

      // 3. Xử lý gọi đúng API theo thiết kế Backend
      // Giả định: 1 = ToDo, 2 = Doing, 3 = Done
      if (newStatus === 3) {
        // Đánh dấu hoàn thành
        response = await WorkTaskAPI.complete(taskId);
      } else {
        // Trở về trạng thái ToDo / Doing
        response = await WorkTaskAPI.reopen(taskId);
      }

      // Kiểm tra kết quả trả về
      if (response && response.isSuccess === false) {
        task.status = oldStatus // Rollback UI nếu API báo lỗi
        console.error('Lỗi khi cập nhật trạng thái task:', response.message)
        return false
      }
      
      return true
    } catch (error) {
      task.status = oldStatus // Rollback UI nếu API sập/time-out
      console.error('Lỗi Exception khi cập nhật trạng thái task:', error)
      return false
    }
  }

  return {
    workspaces,
    isLoading,
    currentWorkspaceId,
    activeWorkspace,
    currentTasks,
    isLoadingTasks,
    fetchMyWorkspaces,
    setCurrentWorkspace,
    fetchTasksByWorkspace,
    createTask,
    updateTaskStatus // Bổ sung export
  }
})