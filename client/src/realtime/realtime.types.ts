import type { Guid } from '@/api/models/common.model'

export type RealtimeConnectionStatus =
  | 'idle'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'
  | 'error'

export interface RealtimeEnvelope<TPayload = unknown> {
  eventName?: string
  workspaceId?: Guid | null
  pageId?: Guid | null
  taskId?: Guid | null
  blockId?: Guid | null
  conversationId?: Guid | null
  userId?: Guid | null
  actorId?: Guid | null
  senderUserId?: Guid | null
  recipientUserId?: Guid | null
  occurredAtUtc?: string
  revision?: number | null
  payload?: TPayload
}

export type RealtimeEventHandler<TPayload = unknown> = (
  envelope: RealtimeEnvelope<TPayload>
) => void | Promise<void>

export interface RealtimeState {
  status: RealtimeConnectionStatus
  error: string | null
  connectionId: string | null
  startedAtUtc: string | null
  lastConnectedAtUtc: string | null
  lastDisconnectedAtUtc: string | null
}

export interface ConversationJoinAck {
  conversationId: Guid
  groupName: string
}

export interface PageCursorRequest {
  pageId: Guid
  blockId?: Guid | null
  anchorKey?: string | null
  offset?: number | null
  color?: string | null
}

export interface PageMousePointerRequest {
  pageId: Guid
  blockId?: Guid | null
  x: number
  y: number
  color?: string | null
  isLeaving?: boolean
}

export interface PageMousePointerPayload extends PageMousePointerRequest {
  workspaceId: Guid
  userId: Guid
  userName?: string | null
  connectionId: string
  occurredAtUtc: string
}

export interface BlockDraftRequest {
  pageId: Guid
  blockId: Guid
  editorSessionId: string
  baseRevision: number
  clientSequence: number
  type?: string | null
  textContent?: string | null
  propsJson?: string | null
}

export interface BlockDraftPayload extends BlockDraftRequest {
  workspaceId: Guid
  userId: Guid
  userName?: string | null
  connectionId: string
  occurredAtUtc: string
}

export interface BlockEditingStateRequest {
  pageId: Guid
  blockId: Guid
  editorSessionId: string
  isEditing: boolean
}

export interface BlockEditingStatePayload extends BlockEditingStateRequest {
  workspaceId: Guid
  userId: Guid
  userName?: string | null
  connectionId: string
  occurredAtUtc: string
}
