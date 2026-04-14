import api from './base.api'
import type {
  TaskCommentQuery,
  CreateCommentRequest,
  UpdateCommentRequest,
} from '@/models/task-comment.model'

export const TaskCommentAPI = {
  // ADD COMMENT
  add: (data: CreateCommentRequest) =>
    api.post(`task-comments`, data),

  // UPDATE COMMENT
  update: (data: UpdateCommentRequest) =>
    api.put(`task-comments/${data.commentId}`, {
      content: data.content,
    }),

  // DELETE COMMENT (soft delete backend)
  delete: (commentId: number | string) =>
    api.delete(`task-comments/${commentId}`),

  // GET BY ID
  getById: (commentId: number | string) =>
    api.get(`task-comments/${commentId}`),

  // GET BY TASK (with paging)
  getByTask: (
    taskId: number | string,
    paging?: { pageNumber?: number; pageSize?: number }
  ) =>
    api.get(`task-comments/tasks/${taskId}`, paging),

  // COUNT COMMENTS
  count: (taskId: number | string) =>
    api.get(`task-comments/tasks/${taskId}/count`),

  // GET BY USER
  getByUser: (
    userId: number | string,
    paging?: { pageNumber?: number; pageSize?: number }
  ) =>
    api.get(`task-comments/users/${userId}`, paging),

  // RESTORE COMMENT
  restore: (commentId: number | string) =>
    api.put(`task-comments/${commentId}/restore`),

  // GET REPLIES
  getReplies: (commentId: number | string) =>
    api.get(`task-comments/${commentId}/replies`),
}