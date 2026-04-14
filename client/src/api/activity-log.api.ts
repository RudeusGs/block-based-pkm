import api from './base.api'
import type { DateRangePayload } from '@/models/activity-log.model'

export const ActivityLogAPI = {
  getWorkspaceLogs: (workspaceId: number) =>
    api.get(`activity-logs/workspace/${workspaceId}`),

  getUserLogs: (userId: number, workspaceId: number) =>
    api.get(
      `activity-logs/user/${userId}/workspace/${workspaceId}`
    ),

  getEntityLogs: (entityType: string, entityId: number) =>
    api.get(
      `activity-logs/entity/${entityType}/${entityId}`
    ),

  getLogsByAction: (workspaceId: number, action: string) =>
    api.get(
      `activity-logs/workspace/${workspaceId}/action/${action}`
    ),

  getLogsByDateRange: (payload: DateRangePayload) =>
    api.post(`activity-logs/date-range`, payload),

  getRecentLogs: (workspaceId: number, limit = 10) =>
    api.get(
      `activity-logs/workspace/${workspaceId}/recent`,
      { limit }
    ),

  deleteLog: (logId: number) =>
    api.delete(`activity-logs/${logId}`),

  deleteOldLogs: (workspaceId: number, daysOld: number) =>
    api.delete(
      `activity-logs/workspace/${workspaceId}/old/${daysOld}`
    ),

  getStats: (workspaceId: number) =>
    api.get(
      `activity-logs/workspace/${workspaceId}/stats`
    ),
}