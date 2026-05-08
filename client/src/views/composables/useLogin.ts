import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import Cookies from 'js-cookie'
import { AuthenticateAPI } from '@/api/authenticate.api'
import { useToast } from '@/components/composables/useToast'

type LoginApiResponse = {
  token?: string
  accessToken?: string
  refreshToken?: string
  data?: {
    token?: string
    accessToken?: string
    refreshToken?: string
  }
}

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

  const getAccessToken = (response: LoginApiResponse): string | undefined => {
    return (
      response?.token ||
      response?.accessToken ||
      response?.data?.token ||
      response?.data?.accessToken
    )
  }

  const handleLogin = async () => {
    if (isSubmitting.value || !canSubmit.value) return

    isSubmitting.value = true
    errorMessage.value = ''

    try {
      const response = await AuthenticateAPI.login({
        userName: form.userName.trim(),
        password: form.password,
      }) as LoginApiResponse

      const token = getAccessToken(response)

      if (!token) {
        throw new Error('Login succeeded but no token was returned.')
      }

      Cookies.set('token', token, {
        expires: 7,
        path: '/',
        sameSite: 'Lax',
        secure: window.location.protocol === 'https:',
      })

      toast.success('Signed in successfully', 'Welcome back.')

      await router.replace('/app')
    } catch (error: any) {
      console.error('Login failed:', error)

      errorMessage.value =
        error?.message ||
        error?.data?.message ||
        error?.data?.title ||
        'Đăng nhập thất bại. Vui lòng thử lại.'
      toast.error('Login failed',
        error?.message ||
          error?.data?.message ||
          error?.data?.title ||
          'Đăng nhập thất bại. Vui lòng thử lại.'
      )
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