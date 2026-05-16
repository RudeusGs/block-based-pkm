import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  AddWorkspaceMemberRequest,
  ChangeWorkspaceMemberRoleRequest,
  CreateWorkspaceRequest,
  UpdateWorkspaceRequest,
  WorkspaceMemberResponse,
  WorkspaceResponse,
} from '../models/workspace.model'

export const workspaceController = {
  create(payload: CreateWorkspaceRequest) {
    return api.post<ApiResult<WorkspaceResponse>>('workspaces', payload)
  },

  update(workspaceId: Guid, payload: UpdateWorkspaceRequest) {
    return api.put<ApiResult<WorkspaceResponse>>(
      `workspaces/${workspaceId}`,
      payload
    )
  },

  delete(workspaceId: Guid) {
    return api.delete<ApiResult>(`workspaces/${workspaceId}`)
  },

  getById(workspaceId: Guid) {
    return api.get<ApiResult<WorkspaceResponse>>(`workspaces/${workspaceId}`)
  },

  listMembers(workspaceId: Guid) {
    return api.get<ApiResult<WorkspaceMemberResponse[]>>(
      `workspaces/${workspaceId}/members`
    )
  },

  addMember(workspaceId: Guid, payload: AddWorkspaceMemberRequest) {
    return api.post<ApiResult<WorkspaceMemberResponse>>(
      `workspaces/${workspaceId}/members`,
      payload
    )
  },

  changeMemberRole(
    workspaceId: Guid,
    userId: Guid,
    payload: ChangeWorkspaceMemberRoleRequest
  ) {
    return api.patch<ApiResult<WorkspaceMemberResponse>>(
      `workspaces/${workspaceId}/members/${userId}/role`,
      payload
    )
  },

  removeMember(workspaceId: Guid, userId: Guid) {
    return api.delete<ApiResult>(
      `workspaces/${workspaceId}/members/${userId}`
    )
  },
}