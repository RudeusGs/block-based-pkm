import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  CompleteTaskRecommendationRequest,
  GenerateTaskRecommendationsRequest,
  ListTaskRecommendationsParams,
  TaskRecommendationPagedResultResponse,
  TaskRecommendationResponse,
  UpdateUserTaskPreferenceRequest,
  UserTaskPreferenceResponse,
} from '../models/recommendation.model'

export const recommendationController = {
  generate(workspaceId: Guid, payload: GenerateTaskRecommendationsRequest) {
    return api.post<ApiResult<TaskRecommendationResponse[]>>(
      `workspaces/${workspaceId}/task-recommendations:generate`,
      payload
    )
  },

  list(params?: ListTaskRecommendationsParams) {
    return api.get<ApiResult<TaskRecommendationPagedResultResponse>>(
      'task-recommendations',
      params
    )
  },

  accept(recommendationId: Guid) {
    return api.post<ApiResult<TaskRecommendationResponse>>(
      `task-recommendations/${recommendationId}:accept`
    )
  },

  reject(recommendationId: Guid) {
    return api.post<ApiResult<TaskRecommendationResponse>>(
      `task-recommendations/${recommendationId}:reject`
    )
  },

  complete(
    recommendationId: Guid,
    payload: CompleteTaskRecommendationRequest
  ) {
    return api.post<ApiResult<TaskRecommendationResponse>>(
      `task-recommendations/${recommendationId}:complete`,
      payload
    )
  },

  getPreference(workspaceId: Guid) {
    return api.get<ApiResult<UserTaskPreferenceResponse>>(
      `workspaces/${workspaceId}/task-recommendation-preference`
    )
  },

  updatePreference(
    workspaceId: Guid,
    payload: UpdateUserTaskPreferenceRequest
  ) {
    return api.put<ApiResult<UserTaskPreferenceResponse>>(
      `workspaces/${workspaceId}/task-recommendation-preference`,
      payload
    )
  },
}
