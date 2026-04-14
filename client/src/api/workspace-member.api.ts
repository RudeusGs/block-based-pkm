import api from './base.api'
import type {
  AddMemberRequest,UpdateMemberRoleRequest
} from '@/models/workspace-member.model'

export const WorkspaceMemberAPI = {
  addMember: (data: AddMemberRequest) =>
    api.post(`workspace-members`, data),

  getMembers: (workspaceId: number | string) =>
    api.get(`workspace-members/${workspaceId}`),

  removeMember: (workspaceId: number | string, userId: number | string) =>
    api.delete(`workspace-members/${workspaceId}/users/${userId}`),

  updateRole: (
    data: UpdateMemberRoleRequest
  ) =>
    api.put(
      `workspace-members/${data.workspaceId}/users/${data.userId}`,
      data.role
    ),

  leaveWorkspace: (workspaceId: number | string) =>
    api.delete(`workspace-members/${workspaceId}/leave`),
}