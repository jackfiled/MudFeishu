import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api'
import type { UserInfo, LoginRequest, RegisterRequest, UpdateProfileRequest, ChangePasswordRequest } from '@/api/types'
import { ElMessage } from 'element-plus'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const user = ref<UserInfo | null>(null)
  const loading = ref(false)

  const isLoggedIn = computed(() => !!token.value && !!user.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')
  const displayName = computed(() => user.value?.displayName || user.value?.username || '')

  async function login(credentials: LoginRequest) {
    loading.value = true
    try {
      const response = await authApi.login(credentials)
      token.value = response.token
      user.value = response.user
      localStorage.setItem('token', response.token)
      localStorage.setItem('user', JSON.stringify(response.user))
      ElMessage.success('登录成功')
      return true
    } catch (error) {
      return false
    } finally {
      loading.value = false
    }
  }

  async function register(data: RegisterRequest) {
    loading.value = true
    try {
      const response = await authApi.register(data)
      token.value = response.token
      user.value = response.user
      localStorage.setItem('token', response.token)
      localStorage.setItem('user', JSON.stringify(response.user))
      ElMessage.success('注册成功')
      return true
    } catch (error) {
      return false
    } finally {
      loading.value = false
    }
  }

  async function fetchProfile() {
    if (!token.value) return
    try {
      const userInfo = await authApi.getProfile()
      user.value = userInfo
      localStorage.setItem('user', JSON.stringify(userInfo))
    } catch (error) {
      logout()
    }
  }

  async function updateProfile(data: UpdateProfileRequest) {
    loading.value = true
    try {
      const userInfo = await authApi.updateProfile(data)
      user.value = userInfo
      localStorage.setItem('user', JSON.stringify(userInfo))
      ElMessage.success('个人信息更新成功')
      return true
    } catch (error) {
      return false
    } finally {
      loading.value = false
    }
  }

  async function changePassword(data: ChangePasswordRequest) {
    loading.value = true
    try {
      await authApi.changePassword(data)
      ElMessage.success('密码修改成功')
      return true
    } catch (error) {
      return false
    } finally {
      loading.value = false
    }
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  function init() {
    const storedToken = localStorage.getItem('token')
    const storedUser = localStorage.getItem('user')
    if (storedToken && storedUser) {
      token.value = storedToken
      try {
        user.value = JSON.parse(storedUser)
      } catch {
        logout()
      }
    }
  }

  return {
    token,
    user,
    loading,
    isLoggedIn,
    isAdmin,
    displayName,
    login,
    register,
    fetchProfile,
    updateProfile,
    changePassword,
    logout,
    init
  }
})
