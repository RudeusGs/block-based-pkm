import type { DateTimeString, Guid, PagingParams } from './common.model'
import type { UserSummaryResponse } from './social.model'

export type MessageType = 'Text' | 'Image' | 'text' | 'image' | string

export interface ConversationResponse {
  id: Guid
  otherUser: UserSummaryResponse
  lastMessagePreview: string | null
  lastMessageAtUtc: DateTimeString | null
  unreadCount: number
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface MessageResponse {
  id: Guid
  conversationId: Guid
  senderUserId: Guid
  recipientUserId: Guid
  type: MessageType
  body: string | null
  imageUrl: string | null
  attachmentFileId: Guid | null
  isMine: boolean
  readAtUtc: DateTimeString | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface ConversationPagedResultResponse {
  items: ConversationResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface MessagePagedResultResponse {
  items: MessageResponse[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CreateDirectConversationRequest {
  recipientUserId: Guid
}

export interface SendTextMessageRequest {
  body: string
}

export interface ListConversationsParams extends PagingParams {}

export interface ListMessagesParams extends PagingParams {}
