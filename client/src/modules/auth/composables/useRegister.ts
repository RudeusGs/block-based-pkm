import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authController } from '@/api'
import { useToast } from '@/components/composables/useToast'
import type { RegisterRequest } from '@/api/models/auth.model'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { saveAuthToken } from '@/modules/auth/utils/auth-token.util'
import { getSafeAuthRedirect } from '@/modules/auth/utils/redirect.util'

export function useRegister() {
  const router = useRouter()
  const route = useRoute()
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
            'Đăng ký thất bại. Vui lòng thử lại.'
          )
        )
      }

      toast.success(
        'Tạo tài khoản thành công',
        'Tài khoản đã được tạo. Đang đăng nhập cho bạn...'
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
            'Tài khoản đã tạo, nhưng đăng nhập tự động thất bại vì hệ thống không trả token.'
          )
        )
      }

      saveAuthToken(token)

      toast.success('Đăng nhập thành công', 'Chào mừng bạn đến với không gian làm việc.')
      await router.replace(getSafeAuthRedirect(route.query.redirect))
    } catch (error) {
      console.error('Register failed:', error)

      const message = getApiErrorMessage(
        error,
        'Đăng ký thất bại. Vui lòng thử lại.'
      )

      toast.error('Đăng ký thất bại', message)
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
