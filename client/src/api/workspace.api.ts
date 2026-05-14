import api from './base.api'
import type {
  CreateWorkspaceRequest,
  UpdateWorkspaceRequest,
  AddWorkspaceMemberRequest,
  ChangeWorkspaceMemberRoleRequest,
  WorkspaceResponse,
  WorkspaceMemberResponse,
} from '@/models/workspace.model'
import type { ApiResult, ApiEmptyResult } from '@/models/api-result.model'

export const WorkspaceAPI = {
  create: (
    req: CreateWorkspaceRequest
  ): Promise<ApiResult<WorkspaceResponse>> => {
    return api.post<ApiResult<WorkspaceResponse>>('workspaces', req)
  },

  update: (
    workspaceId: string,
    req: UpdateWorkspaceRequest
  ): Promise<ApiResult<WorkspaceResponse>> => {
    return api.put<ApiResult<WorkspaceResponse>>(
      `workspaces/${workspaceId}`,
      {
        name: req.name,
        description: req.description,
      }
    )
  },

  delete: (workspaceId: string): Promise<ApiEmptyResult> => {
    return api.delete<ApiEmptyResult>(`workspaces/${workspaceId}`)
  },

  getById: (workspaceId: string): Promise<ApiResult<WorkspaceResponse>> => {
    return api.get<ApiResult<WorkspaceResponse>>(`workspaces/${workspaceId}`)
  },

  listMembers: (
    workspaceId: string
  ): Promise<ApiResult<WorkspaceMemberResponse[]>> => {
    return api.get<ApiResult<WorkspaceMemberResponse[]>>(
      `workspaces/${workspaceId}/members`
    )
  },

  addMember: (
    workspaceId: string,
    req: AddWorkspaceMemberRequest
  ): Promise<ApiResult<WorkspaceMemberResponse>> => {
    return api.post<ApiResult<WorkspaceMemberResponse>>(
      `workspaces/${workspaceId}/members`,
      req
    )
  },

  changeMemberRole: (
    workspaceId: string,
    userId: string,
    req: ChangeWorkspaceMemberRoleRequest
  ): Promise<ApiResult<WorkspaceMemberResponse>> => {
    return api.put<ApiResult<WorkspaceMemberResponse>>(
      `workspaces/${workspaceId}/members/${userId}/role`,
      req
    )
  },

  removeMember: (
    workspaceId: string,
    userId: string
  ): Promise<ApiResult> => {
    return api.delete<ApiResult>(
      `workspaces/${workspaceId}/members/${userId}`
    )
  },
}