export interface TaskAssigneeQuery {
  taskId?: number | string
  userId?: number | string
  workspaceId?: number | string
}

export interface AssignTaskRequest {
  taskId: number | string
  userIds: (number | string)[]
}

export interface AssignSingleRequest {
  taskId: number | string
  userId: number | string
}

export interface ReassignTaskRequest {
  taskId: number | string
  oldUserId: number | string
  newUserId: number | string
}