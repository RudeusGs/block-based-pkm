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
  userId?: Guid | null
  actorId?: Guid | null
  occurredAtUtc?: string
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