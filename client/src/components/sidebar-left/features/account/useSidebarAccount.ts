import { useRouter } from 'vue-router'
import { authController } from '@/api/services/auth.api'
import { useMyProfile } from '@/modules/account/composables/useMyProfile'
import { clearAuthToken } from '@/modules/auth/utils/auth-token.util'

export function useSidebarAccount() {
  const router = useRouter()

  const {
    profileDisplayName,
    profileSubtitle,
    profileAvatarUrl,
    profileInitial,
    isLoadingProfile,
    fetchMyProfile,
  } = useMyProfile()

  async function logout() {
    try {
      await authController.logoutAll()
    } catch {
    } finally {
      clearAuthToken()
      await router.replace('/login')
    }
  }

  return {
    profileDisplayName,
    profileSubtitle,
    profileAvatarUrl,
    profileInitial,
    isLoadingProfile,
    fetchMyProfile,
    logout,
  }
}
