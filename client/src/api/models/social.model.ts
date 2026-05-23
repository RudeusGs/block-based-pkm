import type { DateTimeString, Guid, PagingParams } from './common.model'

export type FriendshipStatus =
  | 'self'
  | 'none'
  | 'friends'
  | 'request_sent'
  | 'request_received'
  | string

export interface UserSummaryResponse {
  id: Guid
  userName: string
  fullName: string
  avatarUrl: string | null
}

export interface UserSearchResultResponse {
  id: Guid
  userName: string
  fullName: string
  avatarUrl: string | null
  isCurrentUser: boolean
  friendshipStatus: FriendshipStatus
}

export interface FriendRequestResponse {
  id: Guid
  requesterId: Guid
  addresseeId: Guid
  status: string
  otherUser: UserSummaryResponse
  createdDate: DateTimeString
  respondedAtUtc: DateTimeString | null
}

export interface FriendResponse {
  userId: Guid
  userName: string
  fullName: string
  avatarUrl: string | null
  friendsSinceUtc: DateTimeString
}

export interface ProfileWorkspaceResponse {
  id: Guid
  name: string
  description: string | null
  visibility: string
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface UserProfilePageResponse {
  userId: Guid
  userName: string
  fullName: string
  avatarUrl: string | null
  bio: string | null
  coverImageUrl: string | null
  friendshipStatus: FriendshipStatus
  friendCount: number
  workspaces: ProfileWorkspaceResponse[]
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface SendFriendRequestRequest {
  addresseeUserId: Guid
}

export interface UpdateMyProfilePageRequest {
  bio?: string | null
  coverImageUrl?: string | null
}

export interface SearchUsersParams extends PagingParams {
  keyword: string
}
