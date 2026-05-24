import { computed, ref } from 'vue'
import { workspaceController } from '@/api/services/workspace.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type {
  WorkspaceInvitationResponse,
  WorkspaceRoleValue,
} from '@/api/models/workspace.model'

const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function useInviteWorkspaceMember() {
  const email = ref('')
  const role = ref<WorkspaceRoleValue>('member')
  const isInviting = ref(false)
  const inviteError = ref<string | null>(null)
  const invitation = ref<WorkspaceInvitationResponse | null>(null)

  const normalizedEmail = computed(() => email.value.trim().toLowerCase())

  const canSubmit = computed(() => {
    return emailRegex.test(normalizedEmail.value) && !isInviting.value
  })

  function resetInviteForm() {
    email.value = ''
    role.value = 'member'
    inviteError.value = null
    invitation.value = null
  }

  async function inviteMember(workspaceId: Guid | null) {
    if (!workspaceId) {
      inviteError.value = 'Không tìm thấy không gian hiện tại.'
      return null
    }

    if (!emailRegex.test(normalizedEmail.value)) {
      inviteError.value = 'Email không hợp lệ.'
      return null
    }

    if (isInviting.value) return null

    isInviting.value = true
    inviteError.value = null
    invitation.value = null

    try {
      const result = await workspaceController.addMember(workspaceId, {
        email: normalizedEmail.value,
        role: role.value,
      })

      if (!result.isSuccess || !result.data) {
        inviteError.value = getApiResultErrorMessage(
          result,
          'Không thể gửi lời mời thành viên.'
        )

        return null
      }

      invitation.value = result.data
      return result.data
    } catch (error) {
      inviteError.value = getApiErrorMessage(
        error,
        'Không thể gửi lời mời thành viên.'
      )

      return null
    } finally {
      isInviting.value = false
    }
  }

  return {
    email,
    role,
    isInviting,
    inviteError,
    invitation,
    normalizedEmail,
    canSubmit,
    inviteMember,
    resetInviteForm,
  }
}
