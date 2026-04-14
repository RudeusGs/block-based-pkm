import { PriorityWorkTask } from './enums/priority-work-task.enum'


export interface UserTaskPreferenceQuery {
    userId: number
    workspaceId: number
}

export interface UserTaskPreference {
    id: number

    userId: number
    workspaceId: number

    workDayStartHour: number
    workDayEndHour: number

    preferredDaysOfWeek: number[]

    maxRecommendationsPerSession: number
    minPriorityForRecommendation: PriorityWorkTask
    recommendationSensitivity: number
    recommendationIntervalMinutes: number
    enableAutoRecommendation: boolean
}

export interface UpdateWorkHoursRequest {
    userId: number
    workspaceId: number
    start: number
    end: number
}

export interface UpdatePreferredDaysRequest {
    userId: number
    workspaceId: number
    days: number[]
}

export interface UpdateSensitivityRequest {
    userId: number
    workspaceId: number
    sensitivity: number
}

export interface UpdateMinPriorityRequest {
    userId: number
    workspaceId: number
    minPriority: PriorityWorkTask
}

export interface UpdateIntervalRequest {
    userId: number
    workspaceId: number
    intervalMinutes: number
}

export interface ToggleAutoRecommendationRequest {
    userId: number
    workspaceId: number
    enable: boolean
}

export interface UpdateMaxRecommendationRequest {
    userId: number
    workspaceId: number
    maxCount: number
}