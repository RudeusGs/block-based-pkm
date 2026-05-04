import axios, { type AxiosInstance, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import Cookies from 'js-cookie'

const apiClient: AxiosInstance = axios.create({
  // Đã sửa lại cổng thành 7135 và route thành api/
  baseURL: 'https://localhost:7135/api/', 
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
})

const getToken = (): string | undefined => {
  return Cookies.get('token')
}

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getToken()
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error: any) => Promise.reject(error)
)

apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response.data
  },
  (error: any) => {
    const message = error?.response?.data?.message || error?.message || 'Unknown error'
    console.error('[API ERROR]', message)

    return Promise.reject({
      message,
      status: error?.response?.status,
      data: error?.response?.data,
    })
  }
)

const api = {
  get: <T = any>(url: string, params?: any): Promise<T> => apiClient.get(url, { params }),
  post: <T = any>(url: string, body?: any): Promise<T> => apiClient.post(url, body),
  put: <T = any>(url: string, body?: any): Promise<T> => apiClient.put(url, body),
  delete: <T = any>(url: string): Promise<T> => apiClient.delete(url),
  postForm: <T = any>(url: string, formData: FormData): Promise<T> => {
    return apiClient.post(url, formData, { headers: { 'Content-Type': 'multipart/form-data' } })
  },
}

export default api