import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

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

export interface TokenExpirationInfo {
  accessTokenExpiresAt?: string
  refreshTokenExpiresAt?: string
  accessTokenExpired: boolean
  refreshTokenExpired: boolean
}

export interface TokenStatusResponse {
  success: boolean
  message?: string
  hasValidToken: boolean
  canRefresh: boolean
  tokenInfo?: TokenExpirationInfo
}

export interface RefreshTokenResponse {
  success: boolean
  message?: string
  accessToken?: string
  refreshToken?: string
}

export interface LogoutResponse {
  success: boolean
  message?: string
}

export const authApi = {
  getAuthUrl: async (): Promise<AuthUrlResponse> => {
    const response = await api.get<AuthUrlResponse>('/oauth/feishu/url')
    return response.data
  },

  handleCallback: async (code: string, state: string): Promise<LoginResponse> => {
    const response = await api.post<LoginResponse>('/oauth/feishu/callback', { code, state })
    return response.data
  },

  validateToken: async (token: string): Promise<{ success: boolean; message?: string; user?: UserInfo }> => {
    const response = await api.post('/oauth/validate-token', token, {
      headers: { 'Content-Type': 'application/json' }
    })
    return response.data
  },

  getCurrentUser: async () => {
    const response = await api.get('/oauth/user/me')
    return response.data
  },

  getTokenStatus: async (): Promise<TokenStatusResponse> => {
    const response = await api.get<TokenStatusResponse>('/oauth/token-status')
    return response.data
  },

  refreshToken: async (): Promise<RefreshTokenResponse> => {
    const response = await api.post<RefreshTokenResponse>('/oauth/refresh')
    return response.data
  },

  logout: async (): Promise<LogoutResponse> => {
    const response = await api.post<LogoutResponse>('/oauth/logout')
    return response.data
  }
}

export default api
