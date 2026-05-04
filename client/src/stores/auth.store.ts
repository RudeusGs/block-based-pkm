import { defineStore } from 'pinia'
import { useRouter } from 'vue-router'
import Cookies from 'js-cookie'

export const useAuthStore = defineStore('auth', () => {
  const router = useRouter()

  const logout = () => {
    // 1. Xóa token khỏi Cookie
    Cookies.remove('token')
    
    // 2. Có thể xóa thêm thông tin workspace hiện tại nếu cần
    // localStorage.removeItem('currentWorkspace')

    // 3. Đẩy người dùng về lại màn hình Login
    router.push('/login')
  }

  return { logout }
})