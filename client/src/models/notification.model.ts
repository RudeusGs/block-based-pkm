import type { PagingRequest } from "./paging-request.model"

export interface GetNotificationsQuery extends PagingRequest {
  workspaceId?: number
}

export interface GetUnreadCountQuery {
  workspaceId?: number
}

export interface MarkAllAsReadQuery {
  workspaceId?: number
}