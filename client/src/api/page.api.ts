import api from './base.api'
import type {
  AddPageModel,
  UpdatePageModel,
  GetPagesQuery,
} from '@/models/page.model'

export const PageAPI = {
  create: (data: AddPageModel) =>
    api.post('pages', data),

  update: (pageId: number | string, data: UpdatePageModel) =>
    api.put(`pages/${pageId}`, data),

  delete: (pageId: number | string) =>
    api.delete(`pages/${pageId}`),

  getById: (pageId: number | string) =>
    api.get(`pages/${pageId}`),

  getByWorkspace: (
    workspaceId: number | string,
    query?: GetPagesQuery
  ) =>
    api.get(`pages/workspace/${workspaceId}`, query),

  getSubPages: (pageId: number | string) =>
    api.get(`pages/${pageId}/sub-pages`),
}