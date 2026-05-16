import type { DateTimeString, Guid, PagingParams } from './common.model'

export type WorkTaskStatusRequest = 'todo' | 'doing' | 'done'
export type WorkTaskPriorityRequest = 'low' | 'medium' | 'high'

export interface WorkTaskAssigneeResponse {
  userId: Guid
}

export interface WorkTaskResponse {
  id: Guid
  workspaceId: Guid
  pageId: Guid | null
  title: string
  description: string | null
  status: string
  priority: string
  dueDate: DateTimeString | null
  createdById: Guid
  lastModifiedById: Guid | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
  assignees: WorkTaskAssigneeResponse[]
}

export interface WorkTaskPagedResultResponse {
  items: WorkTaskResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CreateWorkTaskRequest {
  title: string
  description?: string | null
  priority: WorkTaskPriorityRequest
  dueDate?: DateTimeString | null
  assigneeUserIds?: Guid[] | null
}

export interface UpdateWorkTaskRequest {
  pageId: Guid
  title: string
  description?: string | null
  priority: WorkTaskPriorityRequest
  dueDate?: DateTimeString | null
}

export interface AssignWorkTaskRequest {
  userId: Guid
}

export interface ChangeWorkTaskStatusRequest {
  status: WorkTaskStatusRequest
}

export interface WorkTaskListParams extends PagingParams {
  keyword?: string | null
  status?: WorkTaskStatusRequest | null
  priority?: WorkTaskPriorityRequest | null
  assigneeUserId?: Guid | null
  dueFrom?: DateTimeString | null
  dueTo?: DateTimeString | null
  includeCompleted?: boolean
}