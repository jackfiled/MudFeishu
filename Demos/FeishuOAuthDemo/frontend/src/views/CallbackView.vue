<template>
  <div class="callback-container">
    <el-result
      :icon="statusIcon"
      :title="statusTitle"
      :sub-title="statusMessage"
    >
      <template #extra>
        <el-button v-if="!loading" type="primary" @click="goHome">
          进入首页
        </el-button>
        <el-button v-if="error && !loading" @click="retryLogin">
          重新登录
        </el-button>
      </template>
    </el-result>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { authApi } from '@/api/auth'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const loading = ref(true)
const error = ref(false)
const errorMessage = ref('')

const statusIcon = computed(() => {
  if (loading.value) return 'loading'
  return error.value ? 'error' : 'success'
})

const statusTitle = computed(() => {
  if (loading.value) return '正在登录...'
  return error.value ? '登录失败' : '登录成功'
})

const statusMessage = computed(() => {
  if (loading.value) return '正在处理您的登录请求，请稍候'
  if (error.value) return errorMessage.value || '登录过程中出现错误'
  return '欢迎回来！正在跳转...'
})

onMounted(async () => {
  try {
    // 从URL查询参数中获取code和state
    const code = route.query.code as string
    const state = route.query.state as string

    if (!code || !state) {
      throw new Error('缺少必要参数')
    }

    // 验证state是否与之前存储的一致
    const storedState = localStorage.getItem('oauth_state')
    if (!storedState || state !== storedState) {
      throw new Error('State验证失败，可能存在CSRF攻击')
    }

    // 调用后端API处理OAuth回调
    const response = await authApi.handleCallback(code, state)

    if (response.success && response.token && response.user) {
      // 保存token和用户信息
      userStore.setToken(response.token)
      userStore.setUser(response.user)

      // 清除存储的state
      localStorage.removeItem('oauth_state')

      ElMessage.success('登录成功！')

      // 延迟跳转，让用户看到成功消息
      setTimeout(() => {
        router.push('/')
      }, 1000)
    } else {
      throw new Error(response.message || '登录失败')
    }
  } catch (err: any) {
    console.error('OAuth回调处理失败:', err)
    error.value = true
    errorMessage.value = err.response?.data?.message || err.message || '登录过程中出现错误'
    ElMessage.error(errorMessage.value)
  } finally {
    loading.value = false
  }
})

const goHome = () => {
  router.push('/')
}

const retryLogin = () => {
  router.push('/login')
}
</script>

<style scoped>
.callback-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 500px;
  padding: 20px;
}
</style>
