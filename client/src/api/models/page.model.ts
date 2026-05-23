import type { DateTimeString, Guid, PagingParams } from './common.model'

export interface PageResponse {
  id: Guid
  workspaceId: Guid
  parentPageId: Guid | null
  title: string
  icon: string | null
  coverImage: string | null
  isArchived: boolean
  archivedAt: DateTimeString | null
  currentRevision: number
  createdBy: Guid
  lastModifiedBy: Guid | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface PagePagedResultResponse {
  items: PageResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PageQuickAccessResponse {
  id: Guid
  workspaceId: Guid
  parentPageId: Guid | null
  workspaceName: string
  title: string
  icon: string | null
  coverImage: string | null
  isArchived: boolean
  currentRevision: number
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
  isFavorite: boolean
  favoritedAtUtc: DateTimeString | null
  lastVisitedAtUtc: DateTimeString | null
  visitCount: number
}

export interface PageQuickAccessPagedResultResponse {
  items: PageQuickAccessResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CreatePageRequest {
  title: string
  parentPageId?: Guid | null
  icon?: string | null
  coverImage?: string | null
}

export interface UpdatePageMetadataRequest {
  expectedRevision: number
  title: string
  icon?: string | null
  coverImage?: string | null
}

export interface ListWorkspacePagesParams extends PagingParams {}

export interface SearchPagesParams extends PagingParams {
  keyword: string
}

export interface PagePresenceUserResponse {
  userId: Guid
  userName: string | null
  connectionCount: number
  lastSeenUtc: DateTimeString
}

export interface PagePresenceResponse {
  workspaceId: Guid
  pageId: Guid
  activeUsers: PagePresenceUserResponse[]
}
