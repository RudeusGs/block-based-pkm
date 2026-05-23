import api from '../base.api'
import type { ApiResult } from '../models/common.model'
import type {
  ChangeMyPasswordRequest,
  ListMyAssignedTasksParams,
  ListMyWorkspacesParams,
  MyRolesResponse,
  MyWorkspacesResponse,
  UpdateMyProfileRequest,
  UserProfileResponse,
} from '../models/me.model'
import type { WorkTaskPagedResultResponse } from '../models/task.model'

export const meController = {
  getMyProfile() {
    return api.get<ApiResult<UserProfileResponse>>('me')
  },

  updateMyProfile(payload: UpdateMyProfileRequest) {
    return api.patch<ApiResult<UserProfileResponse>>('me/profile', payload)
  },

  changeMyPassword(payload: ChangeMyPasswordRequest) {
    return api.patch<ApiResult>('me/password', payload)
  },

  getMyRoles() {
    return api.get<ApiResult<MyRolesResponse>>('me/roles')
  },

  listMyWorkspaces(params?: ListMyWorkspacesParams) {
    return api.get<ApiResult<MyWorkspacesResponse>>('me/workspaces', params)
  },

  listMyAssignedTasks(params?: ListMyAssignedTasksParams) {
    return api.get<ApiResult<WorkTaskPagedResultResponse>>('me/tasks', params)
  },
}
