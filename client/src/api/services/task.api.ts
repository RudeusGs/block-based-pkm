import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  AssignWorkTaskRequest,
  ChangeWorkTaskStatusRequest,
  CreateWorkTaskRequest,
  UpdateWorkTaskRequest,
  WorkTaskListParams,
  WorkTaskPagedResultResponse,
  WorkTaskResponse,
} from '../models/task.model'

export const taskController = {
  create(pageId: Guid, payload: CreateWorkTaskRequest) {
    return api.post<ApiResult<WorkTaskResponse>>(
      `pages/${pageId}/tasks`,
      payload
    )
  },

  update(taskId: Guid, payload: UpdateWorkTaskRequest) {
    return api.patch<ApiResult<WorkTaskResponse>>(`tasks/${taskId}`, payload)
  },

  delete(taskId: Guid) {
    return api.delete<ApiResult>(`tasks/${taskId}`)
  },

  getById(taskId: Guid) {
    return api.get<ApiResult<WorkTaskResponse>>(`tasks/${taskId}`)
  },

  listByPage(pageId: Guid, params?: WorkTaskListParams) {
    return api.get<ApiResult<WorkTaskPagedResultResponse>>(
      `pages/${pageId}/tasks`,
      params
    )
  },

  listByWorkspace(workspaceId: Guid, params?: WorkTaskListParams) {
    return api.get<ApiResult<WorkTaskPagedResultResponse>>(
      `workspaces/${workspaceId}/tasks`,
      params
    )
  },

  assign(taskId: Guid, payload: AssignWorkTaskRequest) {
    return api.post<ApiResult<WorkTaskResponse>>(
      `tasks/${taskId}/assignees`,
      payload
    )
  },

  unassign(taskId: Guid, userId: Guid) {
    return api.delete<ApiResult<WorkTaskResponse>>(
      `tasks/${taskId}/assignees/${userId}`
    )
  },

  changeStatus(taskId: Guid, payload: ChangeWorkTaskStatusRequest) {
    return api.post<ApiResult<WorkTaskResponse>>(
      `tasks/${taskId}:change-status`,
      payload
    )
  },
}
