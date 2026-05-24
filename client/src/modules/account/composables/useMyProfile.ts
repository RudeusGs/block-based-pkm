import { computed, ref } from 'vue'
import { meController } from '@/api/services/me.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { UserProfileResponse } from '@/api/models/me.model'
import { normalizeImageUrl } from '@/utils/image-url.util'

export function useMyProfile() {
  const profile = ref<UserProfileResponse | null>(null)
  const isLoadingProfile = ref(false)
  const profileError = ref<string | null>(null)

  const profileDisplayName = computed(() => {
    const currentProfile = profile.value

    if (!currentProfile) return 'Người dùng'

    return (
      currentProfile.fullName?.trim() ||
      currentProfile.userName?.trim() ||
      currentProfile.email?.trim() ||
      'Người dùng'
    )
  })

  const profileSubtitle = computed(() => {
    const currentProfile = profile.value

    if (!currentProfile) return 'Hồ sơ'

    return currentProfile.email || currentProfile.status || 'Hồ sơ'
  })

  const profileAvatarUrl = computed(() => {
    return normalizeImageUrl(profile.value?.avatarUrl)
  })

  const profileInitial = computed(() => {
    const name = profileDisplayName.value.trim()

    if (!name) return 'U'

    return name.charAt(0).toUpperCase()
  })

  async function fetchMyProfile() {
    if (isLoadingProfile.value) return

    isLoadingProfile.value = true
    profileError.value = null

    try {
      const result = await meController.getMyProfile()

      if (!result.isSuccess || !result.data) {
        profileError.value = getApiResultErrorMessage(
          result,
          'Không thể tải thông tin người dùng.'
        )

        return
      }

      profile.value = {
        ...result.data,
        avatarUrl: normalizeImageUrl(result.data.avatarUrl),
      }
    } catch (error) {
      profileError.value = getApiErrorMessage(
        error,
        'Không thể tải thông tin người dùng.'
      )
    } finally {
      isLoadingProfile.value = false
    }
  }

  function clearProfileError() {
    profileError.value = null
  }

  return {
    profile,
    profileDisplayName,
    profileSubtitle,
    profileAvatarUrl,
    profileInitial,
    isLoadingProfile,
    profileError,
    fetchMyProfile,
    clearProfileError,
  }
}
