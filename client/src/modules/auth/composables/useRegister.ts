import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { authController } from '@/api'
import { useToast } from '@/components/composables/useToast'
import type { RegisterRequest } from '@/api/models/auth.model'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { saveAuthToken } from '@/modules/auth/utils/auth-token.util'

export function useRegister() {
  const router = useRouter()
  const toast = useToast()

  const showPassword = ref(false)
  const isSubmitting = ref(false)

  const form = reactive<RegisterRequest>({
    fullName: '',
    userName: '',
    email: '',
    password: '',
  })

  const canSubmit = computed(() => {
    return (
      form.fullName.trim().length > 0 &&
      form.userName.trim().length > 0 &&
      form.email.trim().length > 0 &&
      form.password.length > 0
    )
  })

  const togglePassword = () => {
    showPassword.value = !showPassword.value
  }

  const handleRegister = async () => {
    if (isSubmitting.value || !canSubmit.value) return

    isSubmitting.value = true

    try {
      const payload: RegisterRequest = {
        fullName: form.fullName.trim(),
        userName: form.userName.trim(),
        email: form.email.trim(),
        password: form.password,
      }

      const registerResponse = await authController.register(payload)

      if (!registerResponse.isSuccess) {
        throw new Error(
          getApiResultErrorMessage(
            registerResponse,
            'Registration failed. Please try again.'
          )
        )
      }

      toast.success(
        'Account created',
        'Your account has been created. Signing you in...'
      )

      const loginResponse = await authController.login({
        userName: payload.userName,
        password: payload.password,
      })

      const token = loginResponse.data?.accessToken

      if (!loginResponse.isSuccess || !token) {
        throw new Error(
          getApiResultErrorMessage(
            loginResponse,
            'Account created, but automatic login failed because no token was returned.'
          )
        )
      }

      saveAuthToken(token)

      toast.success('Signed in successfully', 'Welcome to your workspace.')
      await router.replace('/app')
    } catch (error) {
      console.error('Register failed:', error)

      const message = getApiErrorMessage(
        error,
        'Registration failed. Please try again.'
      )

      toast.error('Registration failed', message)
    } finally {
      isSubmitting.value = false
    }
  }

  return {
    form,
    showPassword,
    isSubmitting,
    canSubmit,
    handleRegister,
    togglePassword,
  }
}
