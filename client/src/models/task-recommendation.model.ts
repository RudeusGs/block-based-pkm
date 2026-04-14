import { StatusTaskRecommendation } from './enums/task-recommendation.enum'

export interface TaskRecommendationQuery {
    recommendationId?: number
    userId?: number
    workspaceId?: number
    page?: number
    pageSize?: number
}

export interface GenerateRecommendationRequest {
    userId: number
    workspaceId: number
}

export interface ActionRecommendationRequest {
    recommendationId: number
}

export interface RecalculateWeightsRequest {
    workspaceId: number
}

export interface TaskRecommendation {
    id: number
    taskId: number
    userId: number
    workspaceId: number
    score: number
    status: StatusTaskRecommendation

    createdAt: string
}