import type { DateTimeString, Guid, PagingParams } from './common.model'

export type WorkspaceRoleRequest = 'owner' | 'manager' | 'member' | 'viewer'

export interface WorkspaceResponse {
  id: Guid
  name: string
  description: string | null
  ownerId: Guid
  lastModifiedBy: Guid | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
  currentUserRole: string | null
  canRead: boolean
  canWrite: boolean
  canManageMembers: boolean
}

export interface WorkspaceListItemResponse {
  id: Guid
  name: string
  description: string | null
  ownerId: Guid
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
  currentUserRole: string
}

export interface WorkspaceMemberResponse {
  workspaceId: Guid
  userId: Guid
  role: string
  isOwner: boolean
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface WorkspacePagedResultResponse {
  pageNumber: number
  pageSize: number
  totalCount: number
  items: WorkspaceListItemResponse[]
}

export interface CreateWorkspaceRequest {
  name: string
  description?: string | null
}

export interface UpdateWorkspaceRequest {
  name: string
  description?: string | null
}

export interface AddWorkspaceMemberRequest {
  userId: Guid
  role: WorkspaceRoleRequest
}

export interface ChangeWorkspaceMemberRoleRequest {
  role: WorkspaceRoleRequest
}

export interface ListMyWorkspacesParams extends PagingParams {}