import api from './base.api'
import type {
  TaskRecommendationQuery,
  GenerateRecommendationRequest,
  ActionRecommendationRequest,
  RecalculateWeightsRequest,
} from '@/models/task-recommendation.model'

export const TaskRecommendationAPI = {
  generate: (data: GenerateRecommendationRequest) =>
    api.post(
      `task-recommendations/user/${data.userId}/workspace/${data.workspaceId}/generate`
    ),

  getPending: (userId: number | string, workspaceId: number | string) =>
    api.get(
      `task-recommendations/user/${userId}/workspace/${workspaceId}/pending`
    ),

  accept: (recommendationId: number | string) =>
    api.post(
      `task-recommendations/${recommendationId}/accept`
    ),

  reject: (recommendationId: number | string) =>
    api.post(
      `task-recommendations/${recommendationId}/reject`
    ),

  complete: (recommendationId: number | string) =>
    api.post(
      `task-recommendations/${recommendationId}/complete`
    ),

  getHistory: (userId: number | string, workspaceId: number | string) =>
    api.get(
      `task-recommendations/user/${userId}/workspace/${workspaceId}/history`
    ),

  getById: (recommendationId: number | string) =>
    api.get(
      `task-recommendations/${recommendationId}`
    ),

  getEffectiveness: (
    userId: number | string,
    workspaceId: number | string
  ) =>
    api.get(
      `task-recommendations/user/${userId}/workspace/${workspaceId}/effectiveness`
    ),

  cleanupExpired: () =>
    api.post(`task-recommendations/cleanup-expired`),

  recalculateWeights: (data: RecalculateWeightsRequest) =>
    api.post(
      `task-recommendations/workspace/${data.workspaceId}/recalculate-weights`
    ),

  getTopTasks: (
    workspaceId: number | string,
    limit = 10
  ) =>
    api.get(
      `task-recommendations/workspace/${workspaceId}/top`,
      { limit }
    ),

  getHighestScoring: (
    userId: number | string,
    limit = 5
  ) =>
    api.get(
      `task-recommendations/user/${userId}/highest-scoring`,
      { limit }
    ),
}