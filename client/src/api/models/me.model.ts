import type { DateTimeString, Guid, PagingParams } from './common.model'
import type { WorkspacePagedResultResponse } from './workspace.model'
import type {
  WorkTaskPagedResultResponse,
  WorkTaskPriorityRequest,
  WorkTaskStatusRequest,
} from './task.model'

export interface UserProfileResponse {
  id: Guid
  userName: string
  email: string
  fullName: string
  avatarUrl: string | null
  status: string
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface UpdateMyProfileRequest {
  fullName: string
  avatarUrl?: string | null
}

export interface ChangeMyPasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface ListMyWorkspacesParams extends PagingParams {}

export type MyRolesResponse = string[]

export type MyWorkspacesResponse = WorkspacePagedResultResponse

export interface ListMyAssignedTasksParams extends PagingParams {
  workspaceId?: Guid | null
  keyword?: string | null
  status?: WorkTaskStatusRequest | null
  priority?: WorkTaskPriorityRequest | null
  dueFrom?: DateTimeString | null
  dueTo?: DateTimeString | null
  includeCompleted?: boolean
}