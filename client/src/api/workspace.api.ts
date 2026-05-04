import api from './base.api'
import type { CreateWorkspaceRequest } from '@/models/workspace.model'

export const WorkspaceAPI = {
  // Lấy danh sách Workspace
  getMyWorkspaces: (query?: any) => api.get(`workspaces/my`, query),
  
  // Tạo Workspace mới
  create: (data: CreateWorkspaceRequest) => api.post(`workspaces`, data)
}