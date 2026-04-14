import apiClient from './base.api'
import type {
  CreateWorkspaceRequest,
  UpdateWorkspaceRequest,
  WorkspaceQuery
} from '@/models/workspace.model'

export const WorkspaceAPI = {
  create: (req: CreateWorkspaceRequest) =>
    apiClient.post(`workspaces`, req),

  update: (req: UpdateWorkspaceRequest) =>
    apiClient.put(`workspaces/${req.id}`, {
      name: req.name,
      description: req.description
    }),

  delete: (id: number) =>
    apiClient.delete(`workspaces/${id}`),

  getById: (id: number) =>
    apiClient.get(`workspaces/${id}`),

  getMyWorkspaces: (query?: WorkspaceQuery) =>
    apiClient.get(`workspaces/my`, query)
}