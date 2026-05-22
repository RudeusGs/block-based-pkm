import {
  computed,
  onBeforeUnmount,
  onMounted,
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
import { normalizeImageUrl } from '@/utils/image-url.util'
import type { RealtimeEnvelope } from '@/realtime/realtime.types'
import type { Guid } from '@/api/models/common.model'
import type {
  WorkspaceMemberResponse,
  WorkspaceRoleValue,
} from '@/api/models/workspace.model'

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

const WORKSPACE_HEARTBEAT_INTERVAL_MS = 15_000
const MEMBERS_BACKGROUND_REFRESH_INTERVAL_MS = 30_000

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
  const isMutatingMember = ref(false)
  const mutatingMemberId = ref<Guid | null>(null)
  const memberActionError = ref<string | null>(null)

  let requestId = 0
  let unsubscribePresenceChanged: (() => void) | null = null
  let workspaceHeartbeatTimer: number | null = null
  let membersRefreshTimer: number | null = null

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

  const currentWorkspaceMember = computed(() => {
    return members.value.find((member) => member.isCurrentUser) ?? null
  })

  const canManageMembers = computed(() => {
    const currentMember = currentWorkspaceMember.value

    if (!currentMember) return false

    const role = currentMember.role?.trim().toLowerCase()

    return currentMember.isOwner || role === 'owner' || role === 'manager'
  })

  function memberAvailability(member: WorkspaceMemberResponse): WorkspaceMemberAvailability {
    return onlineUserIds.value.has(member.userId) ? 'online' : 'offline'
  }

  function mapMember(member: WorkspaceMemberResponse): WorkspaceMemberListItem {
    return {
      ...member,
      avatarUrl: normalizeImageUrl(member.avatarUrl),
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

  function stopWorkspaceHeartbeat() {
    if (workspaceHeartbeatTimer !== null) {
      window.clearInterval(workspaceHeartbeatTimer)
      workspaceHeartbeatTimer = null
    }
  }

  function stopMembersBackgroundRefresh() {
    if (membersRefreshTimer !== null) {
      window.clearInterval(membersRefreshTimer)
      membersRefreshTimer = null
    }
  }

  async function heartbeatWorkspace(targetWorkspaceId = joinedWorkspaceId.value) {
    if (!targetWorkspaceId) return

    try {
      const presence = await realtimeClient.heartbeatWorkspace(targetWorkspaceId)

      realtimeError.value = null
      applyPresencePayload(presence as WorkspacePresencePayload)
    } catch (heartbeatError) {
      realtimeError.value = getApiErrorMessage(
        heartbeatError,
        'Không thể cập nhật trạng thái online.'
      )
    }
  }

  function startWorkspaceHeartbeat(targetWorkspaceId: Guid) {
    stopWorkspaceHeartbeat()

    workspaceHeartbeatTimer = window.setInterval(() => {
      if (joinedWorkspaceId.value === targetWorkspaceId) {
        void heartbeatWorkspace(targetWorkspaceId)
      }
    }, WORKSPACE_HEARTBEAT_INTERVAL_MS)
  }

  function startMembersBackgroundRefresh(targetWorkspaceId: Guid) {
    stopMembersBackgroundRefresh()

    membersRefreshTimer = window.setInterval(() => {
      if (
        workspaceId.value === targetWorkspaceId &&
        !isLoading.value
      ) {
        void fetchMembers(targetWorkspaceId, { silent: true })
      }
    }, MEMBERS_BACKGROUND_REFRESH_INTERVAL_MS)
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
        startWorkspaceHeartbeat(targetWorkspaceId)
        startMembersBackgroundRefresh(targetWorkspaceId)
        return
      }

      const ack = await realtimeClient.joinWorkspace(targetWorkspaceId)

      joinedWorkspaceId.value = targetWorkspaceId
      joinedConnectionId.value = realtimeClient.state.connectionId
      realtimeError.value = null

      applyPresencePayload(getPresenceFromJoinAck(ack))
      startWorkspaceHeartbeat(targetWorkspaceId)
      startMembersBackgroundRefresh(targetWorkspaceId)
    } catch (realtimeConnectError) {
      realtimeError.value = getApiErrorMessage(
        realtimeConnectError,
        'Không thể kết nối realtime workspace.'
      )
    }
  }

  async function leaveJoinedWorkspace(targetWorkspaceId = joinedWorkspaceId.value) {
    if (!targetWorkspaceId) return

    stopWorkspaceHeartbeat()
    stopMembersBackgroundRefresh()

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

  async function fetchMembers(
    targetWorkspaceId = workspaceId.value,
    options: { silent?: boolean } = {}
  ) {
    if (!targetWorkspaceId) {
      members.value = []
      loadedWorkspaceId.value = null

      if (!options.silent) {
        error.value = 'Không tìm thấy workspace hiện tại.'
      }

      return
    }

    const currentRequestId = ++requestId

    if (!options.silent) {
      isLoading.value = true
    }

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

      if (!options.silent) {
        error.value = getApiErrorMessage(
          fetchError,
          'Không thể tải danh sách thành viên.'
        )
      }

      members.value = options.silent ? members.value : []
    } finally {
      if (currentRequestId === requestId && !options.silent) {
        isLoading.value = false
      }
    }
  }

  function open() {
    isOpen.value = true

    void connectWorkspaceRealtime()

    void fetchMembers(workspaceId.value, {
      silent:
        loadedWorkspaceId.value === workspaceId.value &&
        members.value.length > 0,
    })
  }

  function close() {
    isOpen.value = false
  }

  function refresh() {
    void connectWorkspaceRealtime(workspaceId.value, true)
    void fetchMembers()
  }


  function assertCanMutateMember(targetMember: WorkspaceMemberListItem) {
    if (!workspaceId.value) {
      memberActionError.value = 'Không tìm thấy workspace hiện tại.'
      return false
    }

    if (!canManageMembers.value) {
      memberActionError.value = 'Bạn không có quyền quản lý thành viên workspace này.'
      return false
    }

    if (targetMember.isCurrentUser) {
      memberActionError.value = 'Bạn không thể tự thay đổi quyền hoặc xóa chính mình khỏi workspace.'
      return false
    }

    if (targetMember.isOwner) {
      memberActionError.value = 'Không thể thay đổi hoặc xóa owner của workspace.'
      return false
    }

    if (isMutatingMember.value) {
      return false
    }

    return true
  }

  async function changeMemberRole(
    targetMember: WorkspaceMemberListItem,
    role: WorkspaceRoleValue
  ) {
    const targetWorkspaceId = workspaceId.value ?? targetMember.workspaceId

    if (!assertCanMutateMember(targetMember) || !targetWorkspaceId) return

    const normalizedRole = role.trim().toLowerCase() as WorkspaceRoleValue
    const currentRole = targetMember.role?.trim().toLowerCase()

    if (currentRole === normalizedRole) return

    isMutatingMember.value = true
    mutatingMemberId.value = targetMember.userId
    memberActionError.value = null

    try {
      const result = await workspaceController.changeMemberRole(
        targetWorkspaceId,
        targetMember.userId,
        {
          role: normalizedRole,
        }
      )

      if (!result.isSuccess || !result.data) {
        memberActionError.value = getApiResultErrorMessage(
          result,
          'Không thể thay đổi role của thành viên.'
        )
        return
      }

      const updatedMember = mapMember(result.data)

      members.value = members.value
        .map((member) =>
          member.userId === updatedMember.userId ? updatedMember : member
        )
        .sort(compareMembers)

      window.dispatchEvent(
        new CustomEvent('pkm:workspace-members-updated', {
          detail: {
            workspaceId: targetWorkspaceId,
            userId: targetMember.userId,
            action: 'role-changed',
            role: normalizedRole,
          },
        })
      )

      void fetchMembers(targetWorkspaceId, { silent: true })
    } catch (mutationError) {
      memberActionError.value = getApiErrorMessage(
        mutationError,
        'Không thể thay đổi role của thành viên.'
      )
    } finally {
      isMutatingMember.value = false
      mutatingMemberId.value = null
    }
  }

  async function removeMember(targetMember: WorkspaceMemberListItem) {
    const targetWorkspaceId = workspaceId.value ?? targetMember.workspaceId

    if (!assertCanMutateMember(targetMember) || !targetWorkspaceId) return

    isMutatingMember.value = true
    mutatingMemberId.value = targetMember.userId
    memberActionError.value = null

    try {
      const result = await workspaceController.removeMember(
        targetWorkspaceId,
        targetMember.userId
      )

      if (!result.isSuccess) {
        memberActionError.value = getApiResultErrorMessage(
          result,
          'Không thể xóa thành viên khỏi workspace.'
        )
        return
      }

      members.value = members.value.filter(
        (member) => member.userId !== targetMember.userId
      )

      window.dispatchEvent(
        new CustomEvent('pkm:workspace-members-updated', {
          detail: {
            workspaceId: targetWorkspaceId,
            userId: targetMember.userId,
            action: 'removed',
          },
        })
      )

      void fetchMembers(targetWorkspaceId, { silent: true })
    } catch (mutationError) {
      memberActionError.value = getApiErrorMessage(
        mutationError,
        'Không thể xóa thành viên khỏi workspace.'
      )
    } finally {
      isMutatingMember.value = false
      mutatingMemberId.value = null
    }
  }

  function refreshMembersAfterProfileChange() {
    if (!workspaceId.value) return

    void fetchMembers(workspaceId.value, { silent: true })
  }

  function refreshMembersWhenPageVisible() {
    if (document.visibilityState !== 'visible' || !workspaceId.value) return

    void heartbeatWorkspace(workspaceId.value)
    void fetchMembers(workspaceId.value, { silent: true })
  }

  watch(
    workspaceId,
    (nextWorkspaceId, previousWorkspaceId) => {
      members.value = []
      error.value = null
      realtimeError.value = null
      memberActionError.value = null
      loadedWorkspaceId.value = null
      onlineUserIds.value = new Set()

      if (previousWorkspaceId && previousWorkspaceId !== nextWorkspaceId) {
        void leaveJoinedWorkspace(previousWorkspaceId)
      }

      if (nextWorkspaceId) {
        void connectWorkspaceRealtime(nextWorkspaceId, true)
        void fetchMembers(nextWorkspaceId, { silent: true })
      }
    },
    { immediate: true }
  )

  watch(realtimeClient.status, (status) => {
    if (status === 'connected' && workspaceId.value) {
      void connectWorkspaceRealtime(workspaceId.value, true)
    }

    if (status === 'disconnected' || status === 'error') {
      stopWorkspaceHeartbeat()
    }
  })

  onMounted(() => {
    window.addEventListener('pkm:profile-updated', refreshMembersAfterProfileChange)
    document.addEventListener('visibilitychange', refreshMembersWhenPageVisible)
    window.addEventListener('focus', refreshMembersWhenPageVisible)
  })

  onBeforeUnmount(() => {
    window.removeEventListener('pkm:profile-updated', refreshMembersAfterProfileChange)
    document.removeEventListener('visibilitychange', refreshMembersWhenPageVisible)
    window.removeEventListener('focus', refreshMembersWhenPageVisible)

    unsubscribePresenceChanged?.()
    unsubscribePresenceChanged = null

    stopWorkspaceHeartbeat()
    stopMembersBackgroundRefresh()
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
    isMutatingMember,
    mutatingMemberId,
    memberActionError,
    canManageMembers,
    memberCountLabel,
    open,
    close,
    refresh,
    fetchMembers,
    connectWorkspaceRealtime,
    changeMemberRole,
    removeMember,
  }
}



