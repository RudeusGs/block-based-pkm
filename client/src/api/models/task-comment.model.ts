import type { DateTimeString, Guid, PagingParams } from './common.model'

export interface TaskCommentResponse {
  id: Guid
  taskId: Guid
  userId: Guid
  parentId: Guid | null
  content: string
  isDeleted: boolean
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
  deletedDate: DateTimeString | null
}

export interface TaskCommentPagedResultResponse {
  items: TaskCommentResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ListTaskCommentsParams extends PagingParams {
  includeDeleted?: boolean
}

export interface CreateTaskCommentRequest {
  content: string
  parentId?: Guid | null
}

export interface UpdateTaskCommentRequest {
  content: string
}