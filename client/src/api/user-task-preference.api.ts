import api from './base.api'
import type {
  UserTaskPreferenceQuery,
  UpdateWorkHoursRequest,
  UpdatePreferredDaysRequest,
  UpdateSensitivityRequest,
  UpdateMinPriorityRequest,
  UpdateIntervalRequest,
  ToggleAutoRecommendationRequest,
  UpdateMaxRecommendationRequest,
} from '@/models/user-task-preference.model'

export const UserTaskPreferenceAPI = {
  create: (userId: number | string, workspaceId: number | string) =>
    api.post(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}`
    ),

  get: (userId: number | string, workspaceId: number | string) =>
    api.get(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}`
    ),

  update: (
    userId: number | string,
    workspaceId: number | string,
    data: any
  ) =>
    api.put(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}`,
      data
    ),

  updateWorkHours: (data: UpdateWorkHoursRequest) =>
    api.put(`user-task-preferences/work-hours`, data),

  updatePreferredDays: (data: UpdatePreferredDaysRequest) =>
    api.put(`user-task-preferences/preferred-days`, data),

  updateSensitivity: (
    userId: number | string,
    workspaceId: number | string,
    sensitivity: number
  ) =>
    api.put(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}/sensitivity`,
      sensitivity
    ),

  updateMinPriority: (
    userId: number | string,
    workspaceId: number | string,
    minPriority: string
  ) =>
    api.put(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}/min-priority`,
      minPriority
    ),

  updateInterval: (
    userId: number | string,
    workspaceId: number | string,
    intervalMinutes: number
  ) =>
    api.put(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}/interval`,
      intervalMinutes
    ),

  toggleAuto: (
    userId: number | string,
    workspaceId: number | string
  ) =>
    api.post(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}/toggle-auto`
    ),

  updateMax: (
    userId: number | string,
    workspaceId: number | string,
    maxCount: number
  ) =>
    api.put(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}/max-recommendations`,
      maxCount
    ),

  reset: (userId: number | string, workspaceId: number | string) =>
    api.post(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}/reset`
    ),

  isAutoEnabled: (userId: number | string, workspaceId: number | string) =>
    api.get(
      `user-task-preferences/user/${userId}/workspace/${workspaceId}/is-auto-enabled`
    ),
}