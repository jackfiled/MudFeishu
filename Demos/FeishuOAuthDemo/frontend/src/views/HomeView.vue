<template>
  <div class="home-container">
    <el-card class="welcome-card">
      <template #header>
        <div class="card-header">
          <el-icon size="30" color="#67C23A"><SuccessFilled /></el-icon>
          <span>登录成功！</span>
        </div>
      </template>
      
      <el-descriptions :column="2" border>
        <el-descriptions-item label="用户名">
          {{ userStore.user?.name }}
        </el-descriptions-item>
        <el-descriptions-item label="OpenID">
          {{ userStore.user?.openId }}
        </el-descriptions-item>
        <el-descriptions-item label="UnionID">
          {{ userStore.user?.unionId }}
        </el-descriptions-item>
        <el-descriptions-item label="邮箱">
          {{ userStore.user?.email || '未设置' }}
        </el-descriptions-item>
      </el-descriptions>

      <div class="user-avatar">
        <el-avatar :size="120" :src="userStore.user?.avatar">
          {{ userStore.user?.name?.charAt(0) }}
        </el-avatar>
      </div>

      <el-divider />

      <div class="token-status-section">
        <h3>
          <el-icon><Key /></el-icon>
          飞书令牌状态
        </h3>
        
        <el-alert
          v-if="tokenStatus"
          :title="tokenStatus.message"
          :type="tokenStatus.hasValidToken ? 'success' : (tokenStatus.canRefresh ? 'warning' : 'error')"
          :closable="false"
          show-icon
          class="status-alert"
        >
          <template #default>
            <div class="status-details">
              <span>访问令牌: {{ tokenStatus.hasValidToken ? '有效' : '已过期' }}</span>
              <span>刷新令牌: {{ tokenStatus.canRefresh ? '可用' : '已过期' }}</span>
            </div>
          </template>
        </el-alert>

        <el-descriptions v-if="tokenInfo" :column="2" border class="token-info">
          <el-descriptions-item label="访问令牌过期时间">
            {{ formatDateTime(tokenInfo.accessTokenExpiresAt) }}
            <el-tag :type="tokenInfo.accessTokenExpired ? 'danger' : 'success'" size="small">
              {{ tokenInfo.accessTokenExpired ? '已过期' : '有效' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="刷新令牌过期时间">
            {{ formatDateTime(tokenInfo.refreshTokenExpiresAt) }}
            <el-tag :type="tokenInfo.refreshTokenExpired ? 'danger' : 'success'" size="small">
              {{ tokenInfo.refreshTokenExpired ? '已过期' : '有效' }}
            </el-tag>
          </el-descriptions-item>
        </el-descriptions>

        <div class="action-buttons">
          <el-button 
            type="primary" 
            :loading="refreshing"
            :disabled="!tokenStatus?.canRefresh"
            @click="handleRefreshToken"
          >
            <el-icon><Refresh /></el-icon>
            刷新令牌
          </el-button>
          <el-button 
            type="info" 
            :loading="loadingStatus"
            @click="handleFetchStatus"
          >
            <el-icon><Search /></el-icon>
            刷新状态
          </el-button>
          <el-button 
            type="danger" 
            @click="handleLogout"
          >
            <el-icon><SwitchButton /></el-icon>
            登出
          </el-button>
        </div>
      </div>

      <el-divider />

      <div class="token-info">
        <h3>JWT Token</h3>
        <el-input
          v-model="userStore.token"
          type="textarea"
          :rows="4"
          readonly
          placeholder="Token信息"
        />
        <el-button type="primary" @click="copyToken" class="copy-btn">
          <el-icon><DocumentCopy /></el-icon>
          复制Token
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { ElMessage } from 'element-plus'
import { Key, Refresh, Search, SwitchButton } from '@element-plus/icons-vue'

const router = useRouter()
const userStore = useUserStore()

const tokenStatus = ref<typeof userStore.tokenStatus>(null)
const tokenInfo = ref<typeof userStore.tokenInfo>(null)
const loadingStatus = ref(false)
const refreshing = ref(false)

onMounted(async () => {
  await userStore.validateToken()
  await handleFetchStatus()
})

const handleFetchStatus = async () => {
  loadingStatus.value = true
  try {
    const status = await userStore.fetchTokenStatus()
    if (status) {
      tokenStatus.value = status
      tokenInfo.value = status.tokenInfo || null
    }
  } finally {
    loadingStatus.value = false
  }
}

const handleRefreshToken = async () => {
  refreshing.value = true
  try {
    const result = await userStore.refreshFeishuToken()
    if (result.success) {
      ElMessage.success(result.message)
      await handleFetchStatus()
    } else {
      ElMessage.error(result.message)
    }
  } finally {
    refreshing.value = false
  }
}

const handleLogout = async () => {
  try {
    await userStore.logout()
    ElMessage.success('已登出')
    router.push('/login')
  } catch (error) {
    ElMessage.error('登出失败')
  }
}

const formatDateTime = (dateStr?: string) => {
  if (!dateStr) return '未知'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

const copyToken = () => {
  navigator.clipboard.writeText(userStore.token)
  ElMessage.success('Token已复制到剪贴板')
}
</script>

<style scoped>
.home-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
}

.welcome-card {
  width: 100%;
  max-width: 800px;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 20px;
  font-weight: 600;
}

.user-avatar {
  display: flex;
  justify-content: center;
  margin: 30px 0;
}

.token-status-section {
  margin-top: 20px;
}

.token-status-section h3 {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
  color: #303133;
}

.status-alert {
  margin-bottom: 16px;
}

.status-details {
  display: flex;
  gap: 20px;
  margin-top: 8px;
  font-size: 14px;
}

.token-info {
  margin-bottom: 16px;
}

.action-buttons {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.token-info {
  margin-top: 20px;
}

.token-info h3 {
  margin-bottom: 12px;
  color: #303133;
}

.copy-btn {
  margin-top: 12px;
  width: 100%;
}
</style>
