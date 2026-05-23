import type { DateTimeString, Guid, PagingParams } from './common.model'

export interface NotificationResponse {
  id: Guid
  userId: Guid
  workspaceId: Guid | null
  type: string
  title: string
  message: string
  referenceId: Guid | null
  referenceType: string | null
  isRead: boolean
  readAtUtc: DateTimeString | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface NotificationPagedResultResponse {
  items: NotificationResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface NotificationUnreadCountResponse {
  userId: Guid
  workspaceId: Guid | null
  unreadCount: number
}

export interface ListNotificationsParams extends PagingParams {
  workspaceId?: Guid | null
  unreadOnly?: boolean
}

export interface GetUnreadNotificationCountParams {
  workspaceId?: Guid | null
}

export interface MarkAllNotificationsAsReadParams {
  workspaceId?: Guid | null
}
