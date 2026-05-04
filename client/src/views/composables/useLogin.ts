import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import Cookies from 'js-cookie';
import { AuthenticateAPI } from '@/api/authenticate.api';

export function useLogin() {
  const router = useRouter();
  const showPassword = ref(false);
  const isSubmitting = ref(false);
  const errorMessage = ref('');
  
  const form = reactive({
    userName: '',
    password: ''
  });

const handleLogin = async () => {
    isSubmitting.value = true;
    errorMessage.value = '';

    try {
      const response: any = await AuthenticateAPI.login(form);
      
      if (response.isSuccess && response.data?.token) {
        
        Cookies.set('token', response.data.token, { expires: 7 });
        router.push('/app');
      } else {
        errorMessage.value = response.message || 'Đăng nhập thất bại.';
      }
    } catch (error: any) {
      console.error('Login failed:', error);
      errorMessage.value = error?.message || 'Thông tin đăng nhập không chính xác.';
    } finally {
      isSubmitting.value = false;
    }
  };

  const togglePassword = () => {
    showPassword.value = !showPassword.value;
  };

  return {
    form,
    showPassword,
    isSubmitting,
    errorMessage,
    handleLogin,
    togglePassword
  };
}