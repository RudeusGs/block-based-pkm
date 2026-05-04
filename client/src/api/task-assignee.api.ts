import api from './base.api'
import type {
  TaskAssigneeQuery,
  AssignTaskRequest,
  AssignSingleRequest,
  ReassignTaskRequest,
} from '@/models/task-assignee.model'

export const TaskAssigneeAPI = {
  assign: (data: AssignTaskRequest) =>
    api.post(
      `task-assignees/tasks/${data.taskId}/assignees`,
      data.userIds
    ),

  assignSingle: (data: AssignSingleRequest) =>
    api.post(
      `task-assignees/tasks/${data.taskId}/assignees/${data.userId}`
    ),

  unassign: (query: Required<Pick<TaskAssigneeQuery, 'taskId' | 'userId'>>) =>
    api.delete(
      `task-assignees/tasks/${query.taskId}/assignees/${query.userId}`
    ),

  unassignAll: (taskId: number | string) =>
    api.delete(
      `task-assignees/tasks/${taskId}/assignees`
    ),

  getAssignees: (taskId: number | string) =>
    api.get(
      `task-assignees/tasks/${taskId}/assignees`
    ),

  getAssignedTasks: (userId: number | string) =>
    api.get(
      `task-assignees/users/${userId}/tasks`
    ),

  getAssignedByWorkspace: (
    workspaceId: number | string,
    userId: number | string
  ) =>
    api.get(
      `task-assignees/workspaces/${workspaceId}/users/${userId}/tasks`
    ),

  getAssignedByUsers: (userIds: (number | string)[]) =>
    api.get(
      `task-assignees/tasks`,
      { userIds }
    ),

  isAssigned: (taskId: number | string, userId: number | string) =>
    api.get(
      `task-assignees/tasks/${taskId}/users/${userId}`
    ),

  reassign: (data: ReassignTaskRequest) =>
    api.post(
      `task-assignees/tasks/${data.taskId}/assignees/${data.oldUserId}/reassign`,
      data.newUserId
    ),
}