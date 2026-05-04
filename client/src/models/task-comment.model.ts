export interface TaskCommentQuery {
  commentId?: number | string
  taskId?: number | string
  userId?: number | string
  page?: number
  pageSize?: number
}

export interface CreateCommentRequest {
  taskId: number | string
  content: string
  parentId?: number | string
}

export interface UpdateCommentRequest {
  commentId: number | string
  content: string
}

export interface RestoreCommentRequest {
  commentId: number | string
}

export interface TaskComment {
  id: number
  taskId: number
  userId: number
  content: string
  parentId?: number
  createdAt: string
  updatedAt?: string
}