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
import { onMounted } from 'vue'
import { useUserStore } from '@/stores/user'
import { ElMessage } from 'element-plus'

const userStore = useUserStore()

onMounted(async () => {
  await userStore.validateToken()
})

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
