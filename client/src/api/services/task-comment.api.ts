import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  CreateTaskCommentRequest,
  ListTaskCommentsParams,
  TaskCommentPagedResultResponse,
  TaskCommentResponse,
  UpdateTaskCommentRequest,
} from '../models/task-comment.model'

export const taskCommentController = {
  listByTask(taskId: Guid, params?: ListTaskCommentsParams) {
    return api.get<ApiResult<TaskCommentPagedResultResponse>>(
      `tasks/${taskId}/comments`,
      params
    )
  },

  create(taskId: Guid, payload: CreateTaskCommentRequest) {
    return api.post<ApiResult<TaskCommentResponse>>(
      `tasks/${taskId}/comments`,
      payload
    )
  },

  update(commentId: Guid, payload: UpdateTaskCommentRequest) {
    return api.patch<ApiResult<TaskCommentResponse>>(
      `task-comments/${commentId}`,
      payload
    )
  },

  delete(commentId: Guid) {
    return api.delete<ApiResult<TaskCommentResponse>>(
      `task-comments/${commentId}`
    )
  },

  restore(commentId: Guid) {
    return api.post<ApiResult<TaskCommentResponse>>(
      `task-comments/${commentId}:restore`
    )
  },
}