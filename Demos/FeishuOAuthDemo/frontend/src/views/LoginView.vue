<template>
  <div class="login-container">
    <el-card class="login-card">
      <template #header>
        <div class="card-header">
          <el-icon size="32" color="#409EFF"><Promotion /></el-icon>
          <h2>飞书OAuth登录</h2>
        </div>
      </template>

      <div class="login-content">
        <p class="description">
          使用飞书账号快速登录本系统，享受便捷的访问体验。
        </p>

        <el-button
          type="primary"
          size="large"
          :loading="loading"
          @click="handleFeishuLogin"
          class="feishu-login-btn"
        >
          <el-icon class="btn-icon"><Location /></el-icon>
          使用飞书登录
        </el-button>

        <div class="features">
          <el-alert
            title="安全便捷"
            type="info"
            :closable="false"
            show-icon
          >
            <template #default>
              <ul>
                <li>无需记住额外密码</li>
                <li>基于飞书企业身份认证</li>
                <li>支持CSRF防护</li>
              </ul>
            </template>
          </el-alert>
        </div>

        <div class="info-text">
          <p>
            <el-icon><InfoFilled /></el-icon>
            首次登录将自动创建账户
          </p>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { authApi } from '@/api/auth'
import { ElMessage } from 'element-plus'

const router = useRouter()
const loading = ref(false)

const handleFeishuLogin = async () => {
  try {
    loading.value = true
    const response = await authApi.getAuthUrl()

    if (response.success && response.url && response.state) {
      // 将state存储到localStorage，用于回调验证
      localStorage.setItem('oauth_state', response.state)
      // 重定向到飞书授权页面
      window.location.href = response.url
    } else {
      ElMessage.error(response.message || '获取授权URL失败')
    }
  } catch (error: any) {
    console.error('获取授权URL失败:', error)
    ElMessage.error(error.response?.data?.message || '获取授权URL失败，请稍后重试')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 500px;
  padding: 20px;
}

.login-card {
  width: 100%;
  max-width: 500px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.card-header {
  display: flex;
  align-items: center;
  gap: 12px;
}

.card-header h2 {
  margin: 0;
  color: #303133;
}

.login-content {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.description {
  color: #606266;
  text-align: center;
  margin: 0;
  line-height: 1.6;
}

.feishu-login-btn {
  width: 100%;
  height: 48px;
  font-size: 16px;
  font-weight: 600;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
  transition: all 0.3s;
}

.feishu-login-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.feishu-login-btn:active {
  transform: translateY(0);
}

.btn-icon {
  font-size: 20px;
}

.features ul {
  margin: 8px 0 0 20px;
  padding: 0;
  color: #606266;
}

.features li {
  margin-bottom: 4px;
}

.info-text {
  text-align: center;
  color: #909399;
  font-size: 14px;
}

.info-text p {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  margin: 0;
}
</style>
