import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { AuthenticateAPI } from '@/api/authenticate.api';

// Bọc toàn bộ logic vào export function
export function useRegister() {
  const router = useRouter();

  const form = reactive({
    fullName: '',
    userName: '',
    email: '',
    password: ''
  });

  const showPassword = ref(false);
  const isSubmitting = ref(false);
  const errorMessage = ref('');

  const handleRegister = async () => {
    try {
      isSubmitting.value = true;
      errorMessage.value = '';

      // Gọi API đăng ký
      const response: any = await AuthenticateAPI.register(form);

      if (response.isSuccess) {
        // Đăng ký thành công, đẩy về trang login
        router.push('/login');
      } else {
        errorMessage.value = response.message || 'Registration failed.';
      }
    } catch (error: any) {
      console.error('Registration Error:', error);
      errorMessage.value = error?.message || 'Registration failed. Please try again.';
    } finally {
      isSubmitting.value = false;
    }
  };

  // Return lại các biến và hàm để file .vue có thể dùng được
  return {
    form,
    showPassword,
    isSubmitting,
    errorMessage,
    handleRegister
  };
}