import axios from 'axios'
import { ElMessage } from 'element-plus'
import type { AxiosInstance, AxiosResponse } from 'axios'
import { useAuthStore } from '@/stores/authStore'
import router from '@/router'

interface ApiResponse<T = any> {
  success: boolean
  message?: string
  data: T
  code: number
}

const service: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 30000
})

service.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    console.error('请求错误:', error)
    return Promise.reject(error)
  }
)

service.interceptors.response.use(
  (response: AxiosResponse<ApiResponse>) => {
    const apiResponse = response.data
    if (apiResponse.success) {
      return apiResponse.data
    } else {
      ElMessage.error(apiResponse.message || '请求失败')
      return Promise.reject(new Error(apiResponse.message || '请求失败'))
    }
  },
  (error) => {
    const apiResponse = error.response?.data as ApiResponse | undefined
    const message = apiResponse?.message || error.message || '请求失败'
    
    if (error.response?.status === 401) {
      const authStore = useAuthStore()
      authStore.logout()
      router.push('/login')
      ElMessage.error('登录已过期，请重新登录')
    } else {
      ElMessage.error(message)
    }
    
    return Promise.reject(error)
  }
)

export default service
