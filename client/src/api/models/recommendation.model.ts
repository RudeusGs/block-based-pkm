import type { DateTimeString, Guid, PagingParams } from './common.model'

export type TaskRecommendationStatusRequest =
  | 'pending'
  | 'accepted'
  | 'rejected'
  | 'completed'
  | 'expired'

export type PriorityRequest = 'low' | 'medium' | 'high'

export interface TaskRecommendationResponse {
  id: Guid
  taskId: Guid
  userId: Guid
  workspaceId: Guid
  pageId: Guid | null
  taskTitle: string
  taskDescription: string | null
  taskPriority: string
  taskStatus: string
  taskDueDate: DateTimeString | null
  score: number
  reason: string | null
  status: string
  expiresAt: DateTimeString | null
  acceptedAt: DateTimeString | null
  rejectedAt: DateTimeString | null
  completedAt: DateTimeString | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface TaskRecommendationPagedResultResponse {
  items: TaskRecommendationResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface GenerateTaskRecommendationsRequest {
  pageId?: Guid | null
  force?: boolean
}

export interface ListTaskRecommendationsParams extends PagingParams {
  workspaceId?: Guid | null
  status?: TaskRecommendationStatusRequest | null
}

export interface CompleteTaskRecommendationRequest {
  notes?: string | null
}

export interface UserTaskPreferenceResponse {
  id: Guid
  userId: Guid
  workspaceId: Guid
  workDayStartHour: number
  workDayEndHour: number
  preferredDaysOfWeek: number[]
  maxRecommendationsPerSession: number
  minPriorityForRecommendation: string
  recommendationSensitivity: number
  recommendationIntervalMinutes: number
  enableAutoRecommendation: boolean
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface UpdateUserTaskPreferenceRequest {
  workDayStartHour: number
  workDayEndHour: number
  preferredDaysOfWeek: number[]
  maxRecommendationsPerSession: number
  minPriorityForRecommendation: PriorityRequest
  recommendationSensitivity: number
  recommendationIntervalMinutes: number
  enableAutoRecommendation: boolean
}