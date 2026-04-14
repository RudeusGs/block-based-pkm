import api from './base.api'
import type {
  UserTaskHistoryQuery,
  UserTaskHistoryDateRangeRequest,
} from '@/models/user-task-history.model'

export const UserTaskHistoryAPI = {
  getByUser: (userId: number | string) =>
    api.get(`user-task-histories/user/${userId}`),

  getByWorkspace: (
    userId: number | string,
    workspaceId: number | string
  ) =>
    api.get(
      `user-task-histories/user/${userId}/workspace/${workspaceId}`
    ),

  getByTask: (taskId: number | string) =>
    api.get(`user-task-histories/task/${taskId}`),

  getByDateRange: (data: UserTaskHistoryDateRangeRequest) =>
    api.post(`user-task-histories/date-range`, data),

  getTaskAverageDuration: (taskId: number | string) =>
    api.get(
      `user-task-histories/task/${taskId}/average-duration`
    ),

  getUserAverageDuration: (userId: number | string) =>
    api.get(
      `user-task-histories/user/${userId}/average-duration`
    ),

  getCompletionCount: (taskId: number | string) =>
    api.get(
      `user-task-histories/task/${taskId}/completion-count`
    ),

  getCompletionRate: (taskId: number | string) =>
    api.get(
      `user-task-histories/task/${taskId}/completion-rate`
    ),

  delete: (historyId: number | string) =>
    api.delete(`user-task-histories/${historyId}`),

  getRecent: (userId: number | string, limit = 10) =>
    api.get(
      `user-task-histories/user/${userId}/recent`,
      { limit }
    ),
}