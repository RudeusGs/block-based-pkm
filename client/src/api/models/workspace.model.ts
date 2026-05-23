import type { DateTimeString, Guid } from './common.model'

export type WorkspaceRoleValue = 'manager' | 'member' | 'viewer'
export type WorkspaceVisibilityValue = 'private' | 'public'

export interface CreateWorkspaceRequest {
  name: string
  description?: string | null
  visibility?: WorkspaceVisibilityValue | null
}

export interface UpdateWorkspaceRequest {
  name: string
  description?: string | null
  visibility?: WorkspaceVisibilityValue | null
}

export interface AddWorkspaceMemberRequest {
  email: string
  role: WorkspaceRoleValue
}

export interface ChangeWorkspaceMemberRoleRequest {
  role: WorkspaceRoleValue | 'owner'
}

export interface WorkspaceResponse {
  id: Guid
  name: string
  description: string | null
  visibility: string
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
  visibility: string
  ownerId: Guid
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
  currentUserRole: string
}

export interface WorkspaceMemberResponse {
  workspaceId: Guid
  userId: Guid
  userName: string
  email: string
  fullName: string
  avatarUrl: string | null
  userStatus: string
  role: string
  isOwner: boolean
  isCurrentUser: boolean
  joinedAt: DateTimeString
  updatedDate: DateTimeString | null
}

export interface WorkspaceInvitationResponse {
  id: Guid
  workspaceId: Guid
  email: string
  role: string
  invitedByUserId: Guid
  expiresAtUtc: DateTimeString
  acceptedAtUtc: DateTimeString | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface WorkspacePagedResultResponse {
  pageNumber: number
  pageSize: number
  totalCount: number
  items: WorkspaceListItemResponse[]
}
