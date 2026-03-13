<template>
  <div class="login-container">
    <div class="login-box">
      <div class="login-header">
        <h2>{{ isLogin ? '用户登录' : '用户注册' }}</h2>
        <p>飞书文件管理系统</p>
      </div>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        class="login-form"
        @keyup.enter="handleSubmit"
      >
        <el-form-item prop="username">
          <el-input
            v-model="form.username"
            placeholder="用户名"
            :prefix-icon="User"
            size="large"
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="密码"
            :prefix-icon="Lock"
            size="large"
            show-password
          />
        </el-form-item>

        <template v-if="!isLogin">
          <el-form-item prop="confirmPassword">
            <el-input
              v-model="form.confirmPassword"
              type="password"
              placeholder="确认密码"
              :prefix-icon="Lock"
              size="large"
              show-password
            />
          </el-form-item>

          <el-form-item prop="email">
            <el-input
              v-model="form.email"
              placeholder="邮箱（可选）"
              :prefix-icon="Message"
              size="large"
            />
          </el-form-item>

          <el-form-item prop="displayName">
            <el-input
              v-model="form.displayName"
              placeholder="显示名称（可选）"
              :prefix-icon="UserFilled"
              size="large"
            />
          </el-form-item>
        </template>

        <el-form-item>
          <el-button
            type="primary"
            size="large"
            :loading="authStore.loading"
            class="submit-btn"
            @click="handleSubmit"
          >
            {{ isLogin ? '登录' : '注册' }}
          </el-button>
        </el-form-item>
      </el-form>

      <div class="login-footer">
        <el-link type="primary" @click="toggleMode">
          {{ isLogin ? '还没有账号？立即注册' : '已有账号？立即登录' }}
        </el-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { User, Lock, Message, UserFilled } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/authStore'

const router = useRouter()
const authStore = useAuthStore()
const formRef = ref<FormInstance>()
const isLogin = ref(true)

const form = reactive({
  username: '',
  password: '',
  confirmPassword: '',
  email: '',
  displayName: ''
})

const validateConfirmPassword = (_rule: any, value: string, callback: any) => {
  if (!isLogin.value && value !== form.password) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const rules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度应为3-50个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 100, message: '密码长度至少为6个字符', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: !isLogin.value, message: '请确认密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ]
}

const toggleMode = () => {
  isLogin.value = !isLogin.value
  formRef.value?.resetFields()
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      let success = false
      if (isLogin.value) {
        success = await authStore.login({
          username: form.username,
          password: form.password
        })
      } else {
        success = await authStore.register({
          username: form.username,
          password: form.password,
          email: form.email || undefined,
          displayName: form.displayName || undefined
        })
      }

      if (success) {
        router.push('/')
      }
    }
  })
}
</script>

<style scoped lang="scss">
.login-container {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.login-box {
  width: 100%;
  max-width: 420px;
  background: white;
  border-radius: 12px;
  padding: 40px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.login-header {
  text-align: center;
  margin-bottom: 30px;

  h2 {
    margin: 0 0 8px 0;
    font-size: 28px;
    color: #303133;
  }

  p {
    margin: 0;
    color: #909399;
    font-size: 14px;
  }
}

.login-form {
  .submit-btn {
    width: 100%;
  }
}

.login-footer {
  text-align: center;
  margin-top: 20px;
}
</style>
