import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import Cookies from 'js-cookie'
import { AuthenticateAPI } from '@/api/authenticate.api'
import { useToast } from '@/components/composables/useToast'
import type { RegisterPayload } from '@/models/auth.model'

type AuthTokenApiResponse = {
  token?: string
  accessToken?: string
  refreshToken?: string
  data?: {
    token?: string
    accessToken?: string
    refreshToken?: string
  }
}

type ApiError = {
  message?: string
  status?: number
  data?: {
    message?: string
    title?: string
    error?: string
    errors?: Record<string, string[]>
  }
}

export function useRegister() {
  const router = useRouter()
  const toast = useToast()

  const showPassword = ref(false)
  const isSubmitting = ref(false)

  const form = reactive<RegisterPayload>({
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

  const getAccessToken = (response: AuthTokenApiResponse): string | undefined => {
    return (
      response?.token ||
      response?.accessToken ||
      response?.data?.token ||
      response?.data?.accessToken
    )
  }

  const getApiErrorMessage = (error: ApiError) => {
    const firstValidationError = error?.data?.errors
      ? Object.values(error.data.errors)[0]?.[0]
      : undefined

    return (
      error?.message ||
      error?.data?.message ||
      error?.data?.title ||
      error?.data?.error ||
      firstValidationError ||
      'Registration failed. Please try again.'
    )
  }

  const saveToken = (token: string) => {
    Cookies.set('token', token, {
      expires: 7,
      path: '/',
      sameSite: 'Lax',
      secure: window.location.protocol === 'https:',
    })
  }

  const togglePassword = () => {
    showPassword.value = !showPassword.value
  }

  const handleRegister = async () => {
    if (isSubmitting.value || !canSubmit.value) return

    isSubmitting.value = true

    try {
      const payload: RegisterPayload = {
        fullName: form.fullName.trim(),
        userName: form.userName.trim(),
        email: form.email.trim(),
        password: form.password,
      }

      await AuthenticateAPI.register(payload)

      toast.success(
        'Account created',
        'Your account has been created. Signing you in...'
      )

      const loginResponse = (await AuthenticateAPI.login({
        userName: payload.userName,
        password: payload.password,
      })) as AuthTokenApiResponse

      const token = getAccessToken(loginResponse)

      if (!token) {
        throw new Error(
          'Account created, but automatic login failed because no token was returned.'
        )
      }

      saveToken(token)

      toast.success(
        'Signed in successfully',
        'Welcome to your workspace.'
      )

      await router.replace('/app')
    } catch (error: any) {
      console.error('Register failed:', error)

      const message = getApiErrorMessage(error)

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