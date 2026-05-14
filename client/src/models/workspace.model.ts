export type WorkspaceRole = 'owner' | 'manager' | 'member' | 'viewer'

export interface CreateWorkspaceRequest {
  name: string
  description?: string | null
}

export interface UpdateWorkspaceRequest {
  name: string
  description?: string | null
}

export interface AddWorkspaceMemberRequest {
  userId: string
  role: WorkspaceRole
}

export interface ChangeWorkspaceMemberRoleRequest {
  role: WorkspaceRole
}

export interface WorkspaceResponse {
  id: string
  name: string
  description?: string | null
  ownerId: string
  lastModifiedBy?: string | null
  createdDate: string
  updatedDate?: string | null
  currentUserRole?: WorkspaceRole | null
  canRead: boolean
  canWrite: boolean
  canManageMembers: boolean
}

export interface WorkspaceListItemResponse {
  id: string
  name: string
  description?: string | null
  ownerId: string
  createdDate: string
  updatedDate?: string | null
  currentUserRole: WorkspaceRole
}

export interface WorkspaceMemberResponse {
  workspaceId: string
  userId: string
  role: WorkspaceRole
  isOwner: boolean
  createdDate: string
  updatedDate?: string | null
}

export interface WorkspacePagedResultResponse {
  pageNumber: number
  pageSize: number
  totalCount: number
  items: WorkspaceListItemResponse[]
}

export interface WorkspaceQuery {
  pageNumber?: number
  pageSize?: number
}