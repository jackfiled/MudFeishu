import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authApi, type UserInfo } from '@/api/auth'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>('')
  const user = ref<UserInfo | null>(null)
  const isLoggedIn = ref(false)

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

  const logout = () => {
    token.value = ''
    user.value = null
    isLoggedIn.value = false
    localStorage.removeItem('access_token')
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
    setToken,
    setUser,
    validateToken,
    logout,
    initFromStorage
  }
})
