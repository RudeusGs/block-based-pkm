import {
  computed,
  onBeforeUnmount,
  ref,
  watch,
  type ComputedRef,
} from 'vue'
import { workspaceController } from '@/api/services/workspace.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { realtimeClient } from '@/realtime/realtime.client'
import type { RealtimeEnvelope } from '@/realtime/realtime.types'
import type { Guid } from '@/api/models/common.model'
import type { WorkspaceMemberResponse } from '@/api/models/workspace.model'

export type WorkspaceMemberAvailability = 'online' | 'offline'

export interface WorkspaceMemberListItem extends WorkspaceMemberResponse {
  displayName: string
  initials: string
  availability: WorkspaceMemberAvailability
}

interface WorkspacePresenceUserPayload {
  userId?: Guid
  UserId?: Guid
  userName?: string | null
  UserName?: string | null
  connectionCount?: number
  ConnectionCount?: number
  lastSeenUtc?: string
  LastSeenUtc?: string
}

interface WorkspacePresencePayload {
  workspaceId?: Guid
  WorkspaceId?: Guid
  activeUsers?: WorkspacePresenceUserPayload[]
  ActiveUsers?: WorkspacePresenceUserPayload[]
}

interface WorkspaceJoinAckPayload {
  workspaceId?: Guid
  WorkspaceId?: Guid
  groupName?: string
  GroupName?: string
  presence?: WorkspacePresencePayload
  Presence?: WorkspacePresencePayload
}

function normalizeString(value: unknown) {
  return typeof value === 'string' ? value.trim() : ''
}

function normalizeGuid(value: unknown): Guid | null {
  const text = normalizeString(value)

  return text ? text : null
}

function memberDisplayName(member: WorkspaceMemberResponse) {
  return member.fullName?.trim() || member.userName?.trim() || member.email
}

function memberInitials(member: WorkspaceMemberResponse) {
  const nameParts = memberDisplayName(member)
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)

  const initials = nameParts.map((part) => part[0]?.toUpperCase()).join('')

  return initials || '?'
}

function compareMembers(
  firstMember: WorkspaceMemberListItem,
  secondMember: WorkspaceMemberListItem
) {
  if (firstMember.availability !== secondMember.availability) {
    return firstMember.availability === 'online' ? -1 : 1
  }

  if (firstMember.isCurrentUser !== secondMember.isCurrentUser) {
    return firstMember.isCurrentUser ? -1 : 1
  }

  if (firstMember.isOwner !== secondMember.isOwner) {
    return firstMember.isOwner ? -1 : 1
  }

  return firstMember.displayName.localeCompare(secondMember.displayName)
}

function getPresenceFromJoinAck(raw: unknown): WorkspacePresencePayload | null {
  if (!raw || typeof raw !== 'object') return null

  const value = raw as WorkspaceJoinAckPayload

  return value.presence ?? value.Presence ?? null
}

function getPresenceFromEnvelope(
  envelope: RealtimeEnvelope<unknown>
): WorkspacePresencePayload | null {
  const raw = envelope.payload

  if (!raw || typeof raw !== 'object') return null

  return raw as WorkspacePresencePayload
}

function getOnlineUserIdsFromPresence(payload: WorkspacePresencePayload) {
  const activeUsers = payload.activeUsers ?? payload.ActiveUsers ?? []
  const onlineUserIds = new Set<Guid>()

  for (const user of activeUsers) {
    const userId = normalizeGuid(user.userId ?? user.UserId)
    const connectionCount = Number(user.connectionCount ?? user.ConnectionCount ?? 1)

    if (userId && Number.isFinite(connectionCount) && connectionCount > 0) {
      onlineUserIds.add(userId)
    }
  }

  return onlineUserIds
}

export function useWorkspaceMembersSidebar(
  workspaceId: ComputedRef<Guid | null>
) {
  const isOpen = ref(false)
  const members = ref<WorkspaceMemberListItem[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const realtimeError = ref<string | null>(null)
  const loadedWorkspaceId = ref<Guid | null>(null)
  const joinedWorkspaceId = ref<Guid | null>(null)
  const joinedConnectionId = ref<string | null>(null)
  const onlineUserIds = ref<Set<Guid>>(new Set())

  let requestId = 0
  let unsubscribePresenceChanged: (() => void) | null = null

  const onlineMembers = computed(() => {
    return members.value.filter((member) => member.availability === 'online')
  })

  const offlineMembers = computed(() => {
    return members.value.filter((member) => member.availability === 'offline')
  })

  const memberCountLabel = computed(() => {
    const total = members.value.length

    return total === 1 ? '1 member' : `${total} members`
  })

  function memberAvailability(member: WorkspaceMemberResponse): WorkspaceMemberAvailability {
    return onlineUserIds.value.has(member.userId) ? 'online' : 'offline'
  }

  function mapMember(member: WorkspaceMemberResponse): WorkspaceMemberListItem {
    return {
      ...member,
      displayName: memberDisplayName(member),
      initials: memberInitials(member),
      availability: memberAvailability(member),
    }
  }

  function remapMemberAvailability() {
    members.value = members.value
      .map((member) => ({
        ...member,
        availability: memberAvailability(member),
      }))
      .sort(compareMembers)
  }

  function applyPresencePayload(payload: WorkspacePresencePayload | null) {
    if (!payload) return

    const payloadWorkspaceId = normalizeGuid(payload.workspaceId ?? payload.WorkspaceId)

    if (
      payloadWorkspaceId &&
      workspaceId.value &&
      payloadWorkspaceId !== workspaceId.value
    ) {
      return
    }

    onlineUserIds.value = getOnlineUserIdsFromPresence(payload)
    remapMemberAvailability()
  }

  function bindRealtime() {
    if (unsubscribePresenceChanged) return

    unsubscribePresenceChanged = realtimeClient.on(
      'WorkspacePresenceChanged',
      (envelope) => {
        applyPresencePayload(getPresenceFromEnvelope(envelope))
      }
    )
  }

  async function connectWorkspaceRealtime(
    targetWorkspaceId = workspaceId.value,
    force = false
  ) {
    if (!targetWorkspaceId) return

    bindRealtime()

    try {
      await realtimeClient.start()

      const currentConnectionId = realtimeClient.state.connectionId

      if (
        !force &&
        joinedWorkspaceId.value === targetWorkspaceId &&
        joinedConnectionId.value === currentConnectionId
      ) {
        return
      }

      const ack = await realtimeClient.joinWorkspace(targetWorkspaceId)

      joinedWorkspaceId.value = targetWorkspaceId
      joinedConnectionId.value = realtimeClient.state.connectionId
      realtimeError.value = null

      applyPresencePayload(getPresenceFromJoinAck(ack))
    } catch (realtimeConnectError) {
      realtimeError.value = getApiErrorMessage(
        realtimeConnectError,
        'Không thể kết nối realtime workspace.'
      )
    }
  }

  async function leaveJoinedWorkspace(targetWorkspaceId = joinedWorkspaceId.value) {
    if (!targetWorkspaceId) return

    try {
      await realtimeClient.leaveWorkspace(targetWorkspaceId)
    } catch {
      // Silent fallback: server sẽ cleanup khi connection disconnect/expire.
    } finally {
      if (joinedWorkspaceId.value === targetWorkspaceId) {
        joinedWorkspaceId.value = null
        joinedConnectionId.value = null
      }

      onlineUserIds.value = new Set()
      remapMemberAvailability()
    }
  }

  async function fetchMembers(targetWorkspaceId = workspaceId.value) {
    if (!targetWorkspaceId) {
      members.value = []
      loadedWorkspaceId.value = null
      error.value = 'Không tìm thấy workspace hiện tại.'
      return
    }

    const currentRequestId = ++requestId

    isLoading.value = true
    error.value = null

    try {
      const result = await workspaceController.listMembers(targetWorkspaceId)

      if (currentRequestId !== requestId) return

      if (!result.isSuccess || !result.data) {
        error.value = getApiResultErrorMessage(
          result,
          'Không thể tải danh sách thành viên.'
        )

        members.value = []
        return
      }

      members.value = result.data.map(mapMember).sort(compareMembers)
      loadedWorkspaceId.value = targetWorkspaceId
    } catch (fetchError) {
      if (currentRequestId !== requestId) return

      error.value = getApiErrorMessage(
        fetchError,
        'Không thể tải danh sách thành viên.'
      )
      members.value = []
    } finally {
      if (currentRequestId === requestId) {
        isLoading.value = false
      }
    }
  }

  function open() {
    isOpen.value = true

    void connectWorkspaceRealtime()

    if (loadedWorkspaceId.value !== workspaceId.value || !members.value.length) {
      void fetchMembers()
    }
  }

  function close() {
    isOpen.value = false
  }

  function refresh() {
    void connectWorkspaceRealtime(workspaceId.value, true)
    void fetchMembers()
  }

  watch(
    workspaceId,
    (nextWorkspaceId, previousWorkspaceId) => {
      members.value = []
      error.value = null
      realtimeError.value = null
      loadedWorkspaceId.value = null
      onlineUserIds.value = new Set()

      if (previousWorkspaceId && previousWorkspaceId !== nextWorkspaceId) {
        void leaveJoinedWorkspace(previousWorkspaceId)
      }

      if (nextWorkspaceId) {
        void connectWorkspaceRealtime(nextWorkspaceId, true)

        if (isOpen.value) {
          void fetchMembers(nextWorkspaceId)
        }
      }
    },
    { immediate: true }
  )

  watch(realtimeClient.status, (status) => {
    if (status === 'connected' && workspaceId.value) {
      void connectWorkspaceRealtime(workspaceId.value, true)
    }
  })

  onBeforeUnmount(() => {
    unsubscribePresenceChanged?.()
    unsubscribePresenceChanged = null

    void leaveJoinedWorkspace()
  })

  return {
    isOpen,
    members,
    onlineMembers,
    offlineMembers,
    isLoading,
    error,
    realtimeError,
    memberCountLabel,
    open,
    close,
    refresh,
    fetchMembers,
    connectWorkspaceRealtime,
  }
}