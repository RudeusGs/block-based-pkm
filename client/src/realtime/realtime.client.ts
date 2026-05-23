import { computed, reactive } from 'vue'
import Cookies from 'js-cookie'
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr'
import type {
  BlockDraftRequest,
  BlockEditingStateRequest,
  ConversationJoinAck,
  PageCursorRequest,
  PageMousePointerRequest,
  RealtimeEnvelope,
  RealtimeEventHandler,
  RealtimeState,
} from './realtime.types'

type HandlerSet = Set<RealtimeEventHandler<any>>

const state = reactive<RealtimeState>({
  status: 'idle',
  error: null,
  connectionId: null,
  startedAtUtc: null,
  lastConnectedAtUtc: null,
  lastDisconnectedAtUtc: null,
})

const handlersByEventName = new Map<string, HandlerSet>()

const KNOWN_BACKEND_EVENTS = [
  'WorkspacePresenceChanged',
  'PagePresenceChanged',
  'PageCursorChanged',
  'PageMousePointerChanged',
  'BlockCreated',
  'BlockUpdated',
  'BlockDeleted',
  'BlockDraftChanged',
  'BlockEditingStateChanged',
  'BlockLeaseChanged',
  'TaskCreated',
  'TaskUpdated',
  'TaskDeleted',
  'TaskAssigned',
  'TaskUnassigned',
  'TaskStatusChanged',
  'TaskAssignedFromRecommendation',
  'TaskCompletedFromRecommendation',
  'TaskCommentCreated',
  'TaskCommentUpdated',
  'TaskCommentDeleted',
  'TaskCommentRestored',
  'NotificationCreated',
  'RecommendationCreated',
  'FriendRequestReceived',
  'FriendRequestSent',
  'FriendRequestAccepted',
  'FriendRequestRejected',
  'FriendRequestCancelled',
  'FriendshipChanged',
  'FriendRemoved',
  'ConversationUpserted',
  'MessageCreated',
  'ConversationRead',
] as const

let connection: HubConnection | null = null
let startPromise: Promise<void> | null = null

function getApiBaseUrl() {
  const fallback = 'https://localhost:7286/api/v1/'
  const rawBaseUrl = import.meta.env.VITE_API_BASE_URL || fallback

  return String(rawBaseUrl).replace(/\/+$/, '')
}

function getHubUrl() {
  return getApiBaseUrl()
    .replace(/\/api\/v\d+$/i, '')
    .replace(/\/api$/i, '')
    .replace(/\/+$/, '')
    .concat('/hubs/collaboration')
}

function getAccessToken() {
  return Cookies.get('token') ?? ''
}

function toErrorMessage(error: unknown, fallback: string) {
  if (error instanceof Error && error.message) return error.message
  if (typeof error === 'string' && error.trim()) return error
  return fallback
}

function normalizeNullableString(value: unknown) {
  if (typeof value !== 'string') return null

  const trimmed = value.trim()
  return trimmed ? trimmed : null
}

function normalizeNullableNumber(value: unknown) {
  if (typeof value === 'number' && Number.isFinite(value)) return value

  if (typeof value === 'string' && value.trim()) {
    const parsed = Number(value)
    return Number.isFinite(parsed) ? parsed : null
  }

  return null
}

function normalizeEnvelope<TPayload>(
  eventName: string,
  raw: unknown
): RealtimeEnvelope<TPayload> {
  if (!raw || typeof raw !== 'object') {
    return {
      eventName,
      payload: raw as TPayload,
      occurredAtUtc: new Date().toISOString(),
      revision: null,
    }
  }

  const value = raw as Record<string, unknown>

  return {
    eventName: String(value.eventName ?? value.EventName ?? eventName),
    workspaceId: normalizeNullableString(value.workspaceId ?? value.WorkspaceId),
    pageId: normalizeNullableString(value.pageId ?? value.PageId),
    taskId: normalizeNullableString(value.taskId ?? value.TaskId),
    blockId: normalizeNullableString(value.blockId ?? value.BlockId),
    conversationId: normalizeNullableString(value.conversationId ?? value.ConversationId),
    userId: normalizeNullableString(value.userId ?? value.UserId),
    actorId: normalizeNullableString(value.actorId ?? value.ActorId),
    senderUserId: normalizeNullableString(value.senderUserId ?? value.SenderUserId),
    recipientUserId: normalizeNullableString(value.recipientUserId ?? value.RecipientUserId),
    occurredAtUtc:
      normalizeNullableString(value.occurredAtUtc ?? value.OccurredAtUtc) ??
      new Date().toISOString(),
    revision: normalizeNullableNumber(value.revision ?? value.Revision),
    payload: (value.payload ?? value.Payload ?? raw) as TPayload,
  }
}

function dispatch(eventName: string, rawEnvelope: unknown) {
  const handlers = handlersByEventName.get(eventName)
  if (!handlers || handlers.size === 0) return

  const envelope = normalizeEnvelope(eventName, rawEnvelope)

  for (const handler of handlers) {
    void Promise.resolve(handler(envelope)).catch((error) => {
      console.error(`[Realtime] handler failed for ${eventName}`, error)
    })
  }
}

function registerHubEvent(eventName: string) {
  if (!connection) return

  const normalizedEventName = eventName.trim()
  const lowerEventName = normalizedEventName.toLowerCase()

  connection.off(normalizedEventName)
  connection.on(normalizedEventName, (rawEnvelope: unknown) => {
    dispatch(normalizedEventName, rawEnvelope)
  })

  if (lowerEventName !== normalizedEventName) {
    connection.off(lowerEventName)
    connection.on(lowerEventName, (rawEnvelope: unknown) => {
      dispatch(normalizedEventName, rawEnvelope)
    })
  }
}

function unregisterHubEvent(eventName: string) {
  if (!connection) return

  const normalizedEventName = eventName.trim()
  const lowerEventName = normalizedEventName.toLowerCase()

  connection.off(normalizedEventName)

  if (lowerEventName !== normalizedEventName) {
    connection.off(lowerEventName)
  }
}

function createConnection() {
  const nextConnection = new HubConnectionBuilder()
    .withUrl(getHubUrl(), {
      accessTokenFactory: getAccessToken,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    .build()

  nextConnection.onreconnecting((error) => {
    state.status = 'reconnecting'
    state.error = error
      ? toErrorMessage(error, 'Realtime đang reconnect.')
      : null
    state.connectionId = null
  })

  nextConnection.onreconnected((connectionId) => {
    state.status = 'connected'
    state.error = null
    state.connectionId = connectionId ?? null
    state.lastConnectedAtUtc = new Date().toISOString()
  })

  nextConnection.onclose((error) => {
    state.status = error ? 'error' : 'disconnected'
    state.error = error
      ? toErrorMessage(error, 'Realtime connection đã đóng.')
      : null
    state.connectionId = null
    state.lastDisconnectedAtUtc = new Date().toISOString()
  })

  const eventNames = new Set<string>([
    ...KNOWN_BACKEND_EVENTS,
    ...handlersByEventName.keys(),
  ])

  for (const eventName of eventNames) {
    const normalizedEventName = eventName.trim()
    const lowerEventName = normalizedEventName.toLowerCase()

    nextConnection.on(normalizedEventName, (rawEnvelope: unknown) => {
      dispatch(normalizedEventName, rawEnvelope)
    })

    if (lowerEventName !== normalizedEventName) {
      nextConnection.on(lowerEventName, (rawEnvelope: unknown) => {
        dispatch(normalizedEventName, rawEnvelope)
      })
    }
  }

  return nextConnection
}

async function start() {
  if (connection?.state === HubConnectionState.Connected) return

  const token = getAccessToken()

  if (!token) {
    state.status = 'disconnected'
    state.error = 'Không tìm thấy token đăng nhập để kết nối realtime.'
    return
  }

  if (startPromise) return startPromise

  if (!connection) {
    connection = createConnection()
  }

  state.status = 'connecting'
  state.error = null

  startPromise = connection
    .start()
    .then(() => {
      state.status = 'connected'
      state.error = null
      state.connectionId = connection?.connectionId ?? null

      const now = new Date().toISOString()
      state.startedAtUtc = state.startedAtUtc ?? now
      state.lastConnectedAtUtc = now
    })
    .catch((error) => {
      state.status = 'error'
      state.error = toErrorMessage(error, 'Không kết nối được realtime server.')
      throw error
    })
    .finally(() => {
      startPromise = null
    })

  return startPromise
}

async function stop() {
  if (!connection) return

  try {
    await connection.stop()
  } finally {
    state.status = 'disconnected'
    state.connectionId = null
    state.lastDisconnectedAtUtc = new Date().toISOString()
  }
}

function on<TPayload = unknown>(
  eventName: string,
  handler: RealtimeEventHandler<TPayload>
) {
  const normalizedEventName = eventName.trim()

  if (!normalizedEventName) {
    throw new Error('Realtime eventName không được để trống.')
  }

  let handlers = handlersByEventName.get(normalizedEventName)

  if (!handlers) {
    handlers = new Set()
    handlersByEventName.set(normalizedEventName, handlers)
    registerHubEvent(normalizedEventName)
  }

  handlers.add(handler as RealtimeEventHandler<any>)

  return () => {
    off(normalizedEventName, handler)
  }
}

function off<TPayload = unknown>(
  eventName: string,
  handler: RealtimeEventHandler<TPayload>
) {
  const handlers = handlersByEventName.get(eventName)
  if (!handlers) return

  handlers.delete(handler as RealtimeEventHandler<any>)

  if (handlers.size === 0) {
    handlersByEventName.delete(eventName)
    unregisterHubEvent(eventName)
  }
}

async function invoke<TResult = unknown>(
  methodName: string,
  ...args: unknown[]
) {
  if (!connection || connection.state !== HubConnectionState.Connected) {
    await start()
  }

  if (!connection || connection.state !== HubConnectionState.Connected) {
    throw new Error('Realtime connection chưa sẵn sàng.')
  }

  return connection.invoke<TResult>(methodName, ...args)
}

function joinConversation(conversationId: string) {
  return invoke<ConversationJoinAck>('JoinConversation', conversationId)
}

function leaveConversation(conversationId: string) {
  return invoke('LeaveConversation', conversationId)
}

function joinWorkspace(workspaceId: string) {
  return invoke('JoinWorkspace', workspaceId)
}

function leaveWorkspace(workspaceId: string) {
  return invoke('LeaveWorkspace', workspaceId)
}

function heartbeatWorkspace(workspaceId: string) {
  return invoke('HeartbeatWorkspace', workspaceId)
}

function joinPage(pageId: string) {
  return invoke('JoinPage', pageId)
}

function leavePage(pageId: string) {
  return invoke('LeavePage', pageId)
}

function heartbeatPage(pageId: string) {
  return invoke('HeartbeatPage', pageId)
}

function sendCursor(payload: PageCursorRequest) {
  return invoke<void>('SendCursor', payload)
}

function sendMousePointer(payload: PageMousePointerRequest) {
  return invoke<void>('SendMousePointer', payload)
}

function sendBlockDraft(payload: BlockDraftRequest) {
  return invoke<void>('SendBlockDraft', payload)
}

function sendBlockEditingState(payload: BlockEditingStateRequest) {
  return invoke<void>('SendBlockEditingState', payload)
}

export const realtimeClient = {
  state,

  status: computed(() => state.status),
  error: computed(() => state.error),
  isConnected: computed(() => state.status === 'connected'),
  isConnecting: computed(
    () => state.status === 'connecting' || state.status === 'reconnecting'
  ),

  start,
  stop,
  on,
  off,
  invoke,

  joinConversation,
  leaveConversation,

  joinWorkspace,
  leaveWorkspace,
  heartbeatWorkspace,

  joinPage,
  leavePage,
  heartbeatPage,

  sendCursor,
  sendMousePointer,
  sendBlockDraft,
  sendBlockEditingState,
}



