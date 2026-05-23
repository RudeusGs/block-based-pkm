import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  ConversationPagedResultResponse,
  ConversationResponse,
  CreateDirectConversationRequest,
  ListConversationsParams,
  ListMessagesParams,
  MessagePagedResultResponse,
  MessageResponse,
  SendTextMessageRequest,
} from '../models/messaging.model'

function appendImageFile(formData: FormData, file: File) {
  formData.append('file', file, file.name)
}

export const messagingController = {
  createOrGetDirectConversation(payload: CreateDirectConversationRequest) {
    return api.post<ApiResult<ConversationResponse>>(
      'conversations/direct',
      payload
    )
  },

  listConversations(params?: ListConversationsParams) {
    return api.get<ApiResult<ConversationPagedResultResponse>>(
      'conversations',
      params
    )
  },

  listMessages(conversationId: Guid, params?: ListMessagesParams) {
    return api.get<ApiResult<MessagePagedResultResponse>>(
      `conversations/${conversationId}/messages`,
      params
    )
  },

  sendTextMessage(conversationId: Guid, payload: SendTextMessageRequest) {
    return api.post<ApiResult<MessageResponse>>(
      `conversations/${conversationId}/messages`,
      payload
    )
  },

  sendImageMessage(conversationId: Guid, file: File, caption?: string | null) {
    const formData = new FormData()
    appendImageFile(formData, file)

    if (caption?.trim()) {
      formData.append('caption', caption.trim())
    }

    return api.postForm<ApiResult<MessageResponse>>(
      `conversations/${conversationId}/messages/image`,
      formData
    )
  },

  markRead(conversationId: Guid) {
    return api.post<ApiResult>(`conversations/${conversationId}/read`)
  },
}
