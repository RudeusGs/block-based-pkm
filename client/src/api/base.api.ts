import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
} from 'axios'
import { clearAuthToken, getAuthToken } from '@/modules/auth/utils/auth-token.util'

const apiClient: AxiosInstance = axios.create({
  baseURL:
    import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7286/api/v1/',
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use(
  (config) => {
    const token = getAuthToken()

    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  (error) => Promise.reject(error)
)

apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response.data
  },
  (error) => {
    const responseData = error?.response?.data
    const status = error?.response?.status

    if (status === 401) {
      clearAuthToken()

      const currentPath = window.location.pathname

      if (currentPath.startsWith('/app')) {
        window.location.replace('/login')
      }
    }

    const message =
      responseData?.message ||
      responseData?.title ||
      responseData?.error ||
      error?.message ||
      'Unknown error'

    console.error('[API ERROR]', message)

    return Promise.reject({
      message,
      status,
      data: responseData,
    })
  }
)

const api = {
  get: <T = any>(
    url: string,
    params?: any,
    config?: AxiosRequestConfig
  ): Promise<T> => {
    return apiClient.get(url, { ...config, params }) as Promise<T>
  },

  post: <T = any>(
    url: string,
    body?: any,
    config?: AxiosRequestConfig
  ): Promise<T> => {
    return apiClient.post(url, body, config) as Promise<T>
  },

  put: <T = any>(
    url: string,
    body?: any,
    config?: AxiosRequestConfig
  ): Promise<T> => {
    return apiClient.put(url, body, config) as Promise<T>
  },

  patch: <T = any>(
    url: string,
    body?: any,
    config?: AxiosRequestConfig
  ): Promise<T> => {
    return apiClient.patch(url, body, config) as Promise<T>
  },

  delete: <T = any>(
    url: string,
    params?: any,
    config?: AxiosRequestConfig
  ): Promise<T> => {
    return apiClient.delete(url, { ...config, params }) as Promise<T>
  },

  postForm: <T = any>(
    url: string,
    formData: FormData,
    config?: AxiosRequestConfig
  ): Promise<T> => {
    return apiClient.post(url, formData, {
      ...config,
      headers: {
        ...config?.headers,
        'Content-Type': 'multipart/form-data',
      },
    }) as Promise<T>
  },
}

export default api