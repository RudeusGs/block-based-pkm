import type { PagingRequest } from './paging-request.model'

export interface AddPageModel {
  title: string
  workspaceId: number
  parentPageId?: number
}

export interface UpdatePageModel {
  title?: string
  content?: string
  icon?: string
  coverImage?: string
  parentPageId?: number
  workspaceId?: number
}

export interface GetPagesQuery extends PagingRequest {
  q?: string
}