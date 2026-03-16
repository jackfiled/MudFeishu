import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authApi, type UserInfo, type TokenStatusResponse, type TokenExpirationInfo } from '@/api/auth'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>('')
  const user = ref<UserInfo | null>(null)
  const isLoggedIn = ref(false)
  const tokenStatus = ref<TokenStatusResponse | null>(null)
  const tokenInfo = ref<TokenExpirationInfo | null>(null)

  const setToken = (newToken: string) => {
    token.value = newToken
    isLoggedIn.value = true
    localStorage.setItem('access_token', newToken)
  }

  const setUser = (newUser: UserInfo) => {
    user.value = newUser
  }

  const validateToken = async () => {
    const storedToken = localStorage.getItem('access_token')
    if (!storedToken) return false

    try {
      const result = await authApi.validateToken(storedToken)
      if (result.success && result.user) {
        token.value = storedToken
        user.value = result.user
        isLoggedIn.value = true
        return true
      } else {
        logout()
        return false
      }
    } catch (error) {
      console.error('Token验证失败:', error)
      logout()
      return false
    }
  }

  const fetchTokenStatus = async () => {
    try {
      const status = await authApi.getTokenStatus()
      tokenStatus.value = status
      tokenInfo.value = status.tokenInfo || null
      return status
    } catch (error) {
      console.error('获取令牌状态失败:', error)
      return null
    }
  }

  const refreshFeishuToken = async () => {
    try {
      const result = await authApi.refreshToken()
      if (result.success) {
        await fetchTokenStatus()
        return { success: true, message: result.message || '令牌刷新成功' }
      } else {
        return { success: false, message: result.message || '令牌刷新失败' }
      }
    } catch (error: any) {
      console.error('刷新令牌失败:', error)
      return { success: false, message: error.response?.data?.message || '令牌刷新失败' }
    }
  }

  const logout = async () => {
    try {
      await authApi.logout()
    } catch (error) {
      console.error('登出请求失败:', error)
    } finally {
      token.value = ''
      user.value = null
      isLoggedIn.value = false
      tokenStatus.value = null
      tokenInfo.value = null
      localStorage.removeItem('access_token')
    }
  }

  const initFromStorage = () => {
    const storedToken = localStorage.getItem('access_token')
    if (storedToken) {
      token.value = storedToken
    }
  }

  return {
    token,
    user,
    isLoggedIn,
    tokenStatus,
    tokenInfo,
    setToken,
    setUser,
    validateToken,
    fetchTokenStatus,
    refreshFeishuToken,
    logout,
    initFromStorage
  }
})
