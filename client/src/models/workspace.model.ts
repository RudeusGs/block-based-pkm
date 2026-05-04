export interface Workspace {
  id: number
  name: string
  description?: string | null
  ownerId: number
  createdAt?: string
  updatedAt?: string
}

export interface CreateWorkspaceRequest {
  name: string
  description?: string
}

export interface UpdateWorkspaceRequest {
  id: number
  name: string
  description?: string
}

export interface WorkspaceQuery {
  page?: number
  pageSize?: number
}