import api from './base.api'
import type {
  WorkTaskQuery,
  CreateWorkTaskRequest,
  UpdateWorkTaskRequest,
  WorkTaskActionRequest,
} from '@/models/work-task.model'

export const WorkTaskAPI = {
  create: (data: CreateWorkTaskRequest) =>
    api.post(`work-tasks`, data),

  update: (data: UpdateWorkTaskRequest) =>
    api.put(`work-tasks/${data.taskId}`, data),

  delete: (taskId: number | string) =>
    api.delete(`work-tasks/${taskId}`),

  getById: (taskId: number | string) =>
    api.get(`work-tasks/${taskId}`),

  getByWorkspace: (
    workspaceId: number | string,
    params?: { pageNumber?: number; pageSize?: number }
  ) =>
    api.get(`work-tasks/workspace/${workspaceId}`, params),

  getByStatus: (
    workspaceId: number | string,
    params?: { status?: string }
  ) =>
    api.get(`work-tasks/workspace/${workspaceId}/status`, params),

  complete: (taskId: number | string) =>
    api.post(`work-tasks/${taskId}/complete`),

  reopen: (taskId: number | string) =>
    api.post(`work-tasks/${taskId}/reopen`),
}