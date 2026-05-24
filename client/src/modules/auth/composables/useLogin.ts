import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authController } from '@/api'
import { useToast } from '@/components/composables/useToast'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { saveAuthToken } from '@/modules/auth/utils/auth-token.util'
import { getSafeAuthRedirect } from '@/modules/auth/utils/redirect.util'

export function useLogin() {
  const router = useRouter()
  const route = useRoute()
  const showPassword = ref(false)
  const isSubmitting = ref(false)
  const errorMessage = ref('')
  const toast = useToast()

  const form = reactive({
    userName: '',
    password: '',
  })

  const canSubmit = computed(() => {
    return form.userName.trim().length > 0 && form.password.length > 0
  })

  const handleLogin = async () => {
    if (isSubmitting.value || !canSubmit.value) return

    isSubmitting.value = true
    errorMessage.value = ''

    try {
      const response = await authController.login({
        userName: form.userName.trim(),
        password: form.password,
      })

      const token = response.data?.accessToken

      if (!response.isSuccess || !token) {
        errorMessage.value = getApiResultErrorMessage(
          response,
          'Đăng nhập thất bại. Vui lòng thử lại.'
        )
        toast.error('Đăng nhập thất bại', errorMessage.value)
        return
      }

      saveAuthToken(token)

      toast.success('Đăng nhập thành công', 'Chào mừng bạn trở lại.')
      await router.replace(getSafeAuthRedirect(route.query.redirect))
    } catch (error) {
      console.error('Login failed:', error)

      errorMessage.value = getApiErrorMessage(
        error,
        'Đăng nhập thất bại. Vui lòng thử lại.'
      )
      toast.error('Đăng nhập thất bại', errorMessage.value)
    } finally {
      isSubmitting.value = false
    }
  }

  const togglePassword = () => {
    showPassword.value = !showPassword.value
  }

  return {
    form,
    showPassword,
    isSubmitting,
    errorMessage,
    canSubmit,
    handleLogin,
    togglePassword,
  }
}
