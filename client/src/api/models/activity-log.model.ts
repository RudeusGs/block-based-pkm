import type { DateTimeString, Guid, PagingParams } from './common.model'

export type ActivityLogAction =
  | 'Create'
  | 'Update'
  | 'Delete'
  | 'Archive'
  | 'Restore'
  | 'Move'
  | 'Assign'
  | 'Unassign'
  | 'Complete'
  | 'Reopen'
  | 'Login'
  | 'ChangePermissions'
  | string

export type ActivityLogEntityType =
  | 'Workspace'
  | 'WorkspaceMember'
  | 'Page'
  | 'WorkTask'
  | 'TaskComment'
  | 'TaskAssignee'
  | 'Block'
  | 'User'
  | 'UserPreference'
  | 'RealtimeSession'
  | string

export interface ActivityLogResponse {
  id: Guid
  workspaceId: Guid
  userId: Guid
  userName: string | null
  userFullName: string | null
  userAvatarUrl: string | null
  action: ActivityLogAction
  entityType: ActivityLogEntityType
  entityId: Guid
  description: string | null
  metadataJson: string | null
  ipAddress: string | null
  occurredAt: DateTimeString
  createdDate: DateTimeString
}

export interface ActivityLogPagedResultResponse {
  items: ActivityLogResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ListWorkspaceActivityLogsParams extends PagingParams {
  action?: string | null
  entityType?: string | null
  userId?: Guid | null
  fromUtc?: DateTimeString | null
  toUtc?: DateTimeString | null
  search?: string | null
}
