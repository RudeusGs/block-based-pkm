import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { authController } from '@/api'
import { useToast } from '@/components/composables/useToast'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { saveAuthToken } from '@/modules/auth/utils/auth-token.util'

export function useLogin() {
  const router = useRouter()
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
        toast.error('Login failed', errorMessage.value)
        return
      }

      saveAuthToken(token)

      toast.success('Signed in successfully', 'Welcome back.')
      await router.replace('/app')
    } catch (error) {
      console.error('Login failed:', error)

      errorMessage.value = getApiErrorMessage(
        error,
        'Đăng nhập thất bại. Vui lòng thử lại.'
      )
      toast.error('Login failed', errorMessage.value)
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
