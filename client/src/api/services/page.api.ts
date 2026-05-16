import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  CreatePageRequest,
  ListWorkspacePagesParams,
  PagePagedResultResponse,
  PagePresenceResponse,
  PageResponse,
  SearchPagesParams,
  UpdatePageMetadataRequest,
} from '../models/page.model'

export const pageController = {
  create(workspaceId: Guid, payload: CreatePageRequest) {
    return api.post<ApiResult<PageResponse>>(
      `workspaces/${workspaceId}/pages`,
      payload
    )
  },

  updateMetadata(pageId: Guid, payload: UpdatePageMetadataRequest) {
    return api.patch<ApiResult<PageResponse>>(`pages/${pageId}`, payload)
  },

  delete(pageId: Guid) {
    return api.delete<ApiResult>(`pages/${pageId}`)
  },

  getById(pageId: Guid) {
    return api.get<ApiResult<PageResponse>>(`pages/${pageId}`)
  },

  getPresence(pageId: Guid) {
    return api.get<ApiResult<PagePresenceResponse>>(`pages/${pageId}/presence`)
  },

  listByWorkspace(workspaceId: Guid, params?: ListWorkspacePagesParams) {
    return api.get<ApiResult<PagePagedResultResponse>>(
      `workspaces/${workspaceId}/pages`,
      params
    )
  },

  listSubPages(pageId: Guid) {
    return api.get<ApiResult<PageResponse[]>>(`pages/${pageId}/subpages`)
  },

  search(workspaceId: Guid, params: SearchPagesParams) {
    return api.get<ApiResult<PagePagedResultResponse>>(
      `workspaces/${workspaceId}/pages:search`,
      params
    )
  },
}