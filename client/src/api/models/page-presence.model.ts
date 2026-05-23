import type { Guid } from './common.model'

export interface PagePresenceUserResponse {
  userId: Guid
  userName?: string | null
  connectionCount: number
  lastSeenUtc: string
}

export interface PagePresenceResponse {
  workspaceId: Guid
  pageId: Guid
  activeUsers: PagePresenceUserResponse[]
}
