import type { Guid } from '@/api/models/common.model'
import type { WorkTaskResponse } from '@/api/models/task.model'

export type NormalizedTaskStatus = 'todo' | 'doing' | 'done'
export type NormalizedTaskPriority = 'low' | 'medium' | 'high'

export interface TaskMemberOption {
  userId: Guid
  displayName: string
  email: string
  role: string
  avatarUrl: string
  initials: string
  isCurrentUser: boolean
  isOwner: boolean
}

export interface TaskAssigneeView {
  userId: Guid
  name: string
  avatarUrl: string
  initials: string
  role?: string | null
}

export interface WorkTaskView {
  id: Guid
  workspaceId: Guid
  pageId: Guid | null
  title: string
  description: string
  status: NormalizedTaskStatus
  priority: NormalizedTaskPriority
  dueDateRaw: string | null
  dueDateLabel: string
  overdue: boolean
  createdById: Guid
  updatedDate: string | null
  assignees: TaskAssigneeView[]
}

export interface CreateTaskCreatedPayload {
  task: WorkTaskResponse
  assignees: TaskMemberOption[]
}

export interface TaskCommentAuthorView {
  userId: Guid
  name: string
  role: string
  avatarUrl: string
  initials: string
}

export interface TaskCommentView {
  id: Guid
  taskId: Guid
  parentId: Guid | null
  author: TaskCommentAuthorView
  content: string
  createdAt: string
  isDeleted: boolean
  replies: TaskCommentView[]
}
