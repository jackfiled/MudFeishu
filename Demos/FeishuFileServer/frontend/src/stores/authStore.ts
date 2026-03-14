import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api'
import type { UserInfo, LoginRequest, RegisterRequest, UpdateProfileRequest, ChangePasswordRequest } from '@/api/types'
import { ElMessage } from 'element-plus'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const user = ref<UserInfo | null>(null)
  const loading = ref(false)

  const isAuthenticated = computed(() => !!token.value)
  const isLoggedIn = computed(() => !!token.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')

  const setAuth = (newToken: string, newRefreshToken: string, newUser: UserInfo) => {
    token.value = newToken
    refreshToken.value = newRefreshToken
    user.value = newUser
    localStorage.setItem('token', newToken)
    localStorage.setItem('refreshToken', newRefreshToken)
  }

  const clearAuth = () => {
    token.value = null
    refreshToken.value = null
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
  }

  const init = async () => {
    if (token.value && !user.value) {
      await fetchProfile()
    }
  }

  const login = async (credentials: LoginRequest) => {
    try {
      loading.value = true
      const response = await authApi.login(credentials)
      setAuth(response.token, response.refreshToken, response.user)
      ElMessage.success('登录成功')
      return true
    } catch (error: any) {
      ElMessage.error(error.response?.data?.message || '登录失败')
      return false
    } finally {
      loading.value = false
    }
  }

  const register = async (credentials: RegisterRequest) => {
    try {
      loading.value = true
      const response = await authApi.register(credentials)
      setAuth(response.token, response.refreshToken, response.user)
      ElMessage.success('注册成功')
      return true
    } catch (error: any) {
      ElMessage.error(error.response?.data?.message || '注册失败')
      return false
    } finally {
      loading.value = false
    }
  }

  const logout = async () => {
    if (refreshToken.value) {
      try {
        await authApi.revokeToken(refreshToken.value)
      } catch (error) {
        console.error('Revoke token failed:', error)
      }
    }
    clearAuth()
    ElMessage.success('已退出登录')
  }

  const refreshAccessToken = async () => {
    if (!refreshToken.value) {
      clearAuth()
      return false
    }

    try {
      const response = await authApi.refreshToken(refreshToken.value)
      setAuth(response.token, response.refreshToken, response.user)
      return true
    } catch (error) {
      clearAuth()
      return false
    }
  }

  const fetchProfile = async () => {
    if (!token.value) return

    try {
      loading.value = true
      user.value = await authApi.getProfile()
    } catch (error) {
      const refreshed = await refreshAccessToken()
      if (!refreshed) {
        clearAuth()
      }
    } finally {
      loading.value = false
    }
  }

  const updateProfile = async (data: UpdateProfileRequest) => {
    try {
      loading.value = true
      user.value = await authApi.updateProfile(data)
      ElMessage.success('资料更新成功')
      return true
    } catch (error: any) {
      ElMessage.error(error.response?.data?.message || '更新失败')
      return false
    } finally {
      loading.value = false
    }
  }

  const changePassword = async (data: ChangePasswordRequest) => {
    try {
      loading.value = true
      await authApi.changePassword(data)
      ElMessage.success('密码修改成功')
      return true
    } catch (error: any) {
      ElMessage.error(error.response?.data?.message || '密码修改失败')
      return false
    } finally {
      loading.value = false
    }
  }

  return {
    token,
    refreshToken,
    user,
    loading,
    isAuthenticated,
    isLoggedIn,
    isAdmin,
    setAuth,
    clearAuth,
    init,
    login,
    register,
    logout,
    refreshAccessToken,
    fetchProfile,
    updateProfile,
    changePassword
  }
})
