import { computed, ref, watch, type ComputedRef } from 'vue'
import { workspaceController } from '@/api/services/workspace.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { WorkspaceMemberResponse } from '@/api/models/workspace.model'

export type WorkspaceMemberAvailability = 'online' | 'offline'

export interface WorkspaceMemberListItem extends WorkspaceMemberResponse {
  displayName: string
  initials: string
  availability: WorkspaceMemberAvailability
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

function memberAvailability(member: WorkspaceMemberResponse): WorkspaceMemberAvailability {
  const status = member.userStatus.trim().toLowerCase()

  return status === 'active' || status === 'online' ? 'online' : 'offline'
}

function mapMember(member: WorkspaceMemberResponse): WorkspaceMemberListItem {
  return {
    ...member,
    displayName: memberDisplayName(member),
    initials: memberInitials(member),
    availability: memberAvailability(member),
  }
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

export function useWorkspaceMembersSidebar(
  workspaceId: ComputedRef<Guid | null>
) {
  const isOpen = ref(false)
  const members = ref<WorkspaceMemberListItem[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const loadedWorkspaceId = ref<Guid | null>(null)
  let requestId = 0

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

    if (loadedWorkspaceId.value !== workspaceId.value || !members.value.length) {
      void fetchMembers()
    }
  }

  function close() {
    isOpen.value = false
  }

  function refresh() {
    void fetchMembers()
  }

  watch(workspaceId, (nextWorkspaceId) => {
    members.value = []
    error.value = null
    loadedWorkspaceId.value = null

    if (isOpen.value && nextWorkspaceId) {
      void fetchMembers(nextWorkspaceId)
    }
  })

  return {
    isOpen,
    members,
    onlineMembers,
    offlineMembers,
    isLoading,
    error,
    memberCountLabel,
    open,
    close,
    refresh,
    fetchMembers,
  }
}
