import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  FriendRequestResponse,
  FriendResponse,
  SearchUsersParams,
  SendFriendRequestRequest,
  UpdateMyProfilePageRequest,
  UserProfilePageResponse,
  UserSearchResultResponse,
} from '../models/social.model'

function appendImageFile(formData: FormData, file: File) {
  formData.append('file', file, file.name)
}

export const socialController = {
  searchUsers(params: SearchUsersParams) {
    return api.get<ApiResult<UserSearchResultResponse[]>>(
      'social/users/search',
      params
    )
  },

  getProfile(userId: Guid) {
    return api.get<ApiResult<UserProfilePageResponse>>(
      `social/users/${userId}/profile`
    )
  },

  updateMyProfilePage(payload: UpdateMyProfilePageRequest) {
    return api.put<ApiResult<UserProfilePageResponse>>(
      'social/me/profile-page',
      payload
    )
  },

  uploadMyProfileCoverImage(file: File) {
    const formData = new FormData()
    appendImageFile(formData, file)

    return api.postForm<ApiResult<UserProfilePageResponse>>(
      'social/me/profile-cover-image',
      formData
    )
  },

  sendFriendRequest(payload: SendFriendRequestRequest) {
    return api.post<ApiResult<FriendRequestResponse>>(
      'social/friend-requests',
      payload
    )
  },

  listIncomingRequests(pageNumber = 1, pageSize = 20) {
    return api.get<ApiResult<FriendRequestResponse[]>>(
      'social/friend-requests/incoming',
      { pageNumber, pageSize }
    )
  },

  listOutgoingRequests(pageNumber = 1, pageSize = 20) {
    return api.get<ApiResult<FriendRequestResponse[]>>(
      'social/friend-requests/outgoing',
      { pageNumber, pageSize }
    )
  },

  acceptFriendRequest(requestId: Guid) {
    return api.post<ApiResult<FriendRequestResponse>>(
      `social/friend-requests/${requestId}/accept`
    )
  },

  rejectFriendRequest(requestId: Guid) {
    return api.post<ApiResult<FriendRequestResponse>>(
      `social/friend-requests/${requestId}/reject`
    )
  },

  cancelFriendRequest(requestId: Guid) {
    return api.post<ApiResult<FriendRequestResponse>>(
      `social/friend-requests/${requestId}/cancel`
    )
  },

  listFriends(pageNumber = 1, pageSize = 50) {
    return api.get<ApiResult<FriendResponse[]>>('social/friends', {
      pageNumber,
      pageSize,
    })
  },

  removeFriend(friendUserId: Guid) {
    return api.delete<ApiResult>(`social/friends/${friendUserId}`)
  },
}
