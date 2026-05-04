import api from './base.api'

export const WorkspaceAPI = {
  getMyWorkspaces: (query?: any) => api.get(`workspaces/my`, query)
}