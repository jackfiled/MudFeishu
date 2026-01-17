import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器：添加token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('access_token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// 响应拦截器：处理错误
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('access_token')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export interface AuthUrlResponse {
  success: boolean
  message?: string
  url?: string
  state?: string
}

export interface UserInfo {
  openId: string
  unionId: string
  name: string
  avatar?: string
  email?: string
}

export interface LoginResponse {
  success: boolean
  message?: string
  token?: string
  user?: UserInfo
}

export const authApi = {
  // 获取飞书授权URL
  getAuthUrl: async (): Promise<AuthUrlResponse> => {
    const response = await api.get<AuthUrlResponse>('/oauth/feishu/url')
    return response.data
  },

  // 处理飞书OAuth回调
  handleCallback: async (code: string, state: string): Promise<LoginResponse> => {
    const response = await api.post<LoginResponse>('/oauth/feishu/callback', { code, state })
    return response.data
  },

  // 验证token
  validateToken: async (token: string): Promise<{ success: boolean; message?: string; user?: UserInfo }> => {
    const response = await api.post('/oauth/validate-token', token, {
      headers: { 'Content-Type': 'application/json' }
    })
    return response.data
  },

  // 获取当前用户信息
  getCurrentUser: async () => {
    const response = await api.get('/oauth/user/me')
    return response.data
  },

  // 登出
  logout: async () => {
    const response = await api.post('/oauth/logout')
    return response.data
  }
}

export default api
