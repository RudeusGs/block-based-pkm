import type { Guid } from './common.model'

export type WorkspaceRoleValue = 'manager' | 'member' | 'viewer'

export interface CreateWorkspaceRequest {
  name: string
  description?: string | null
}

export interface UpdateWorkspaceRequest {
  name: string
  description?: string | null
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
  description?: string | null
  ownerId: Guid
  lastModifiedBy?: Guid | null
  createdDate: string
  updatedDate?: string | null
  currentUserRole?: string | null
  canRead: boolean
  canWrite: boolean
  canManageMembers: boolean
}

export interface WorkspaceMemberResponse {
  workspaceId: Guid
  userId: Guid
  role: string
  isOwner: boolean
  createdDate: string
  updatedDate?: string | null
}

export interface WorkspaceInvitationResponse {
  id: Guid
  workspaceId: Guid
  email: string
  role: string
  invitedByUserId: Guid
  expiresAtUtc: string
  acceptedAtUtc?: string | null
  createdDate: string
  updatedDate?: string | null
}