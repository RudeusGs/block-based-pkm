import axios, {
  type AxiosInstance,
  type AxiosResponse,
} from 'axios'
import Cookies from 'js-cookie'

const apiClient: AxiosInstance = axios.create({
  baseURL: 'https://localhost:7286/api/v1/',
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
})

const getToken = (): string | undefined => {
  return Cookies.get('token')
}

apiClient.interceptors.request.use(
  (config) => {
    const token = getToken()

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

    const message =
      responseData?.message ||
      responseData?.title ||
      responseData?.error ||
      error?.message ||
      'Unknown error'

    console.error('[API ERROR]', message)

    return Promise.reject({
      message,
      status: error?.response?.status,
      data: responseData,
    })
  }
)

const api = {
  get: <T = any>(url: string, params?: any): Promise<T> => {
    return apiClient.get(url, { params }) as Promise<T>
  },

  post: <T = any>(url: string, body?: any): Promise<T> => {
    return apiClient.post(url, body) as Promise<T>
  },

  put: <T = any>(url: string, body?: any): Promise<T> => {
    return apiClient.put(url, body) as Promise<T>
  },

  delete: <T = any>(url: string): Promise<T> => {
    return apiClient.delete(url) as Promise<T>
  },

  postForm: <T = any>(url: string, formData: FormData): Promise<T> => {
    return apiClient.post(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }) as Promise<T>
  },
}

export default api