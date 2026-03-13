<template>
  <el-dialog
    title="个人信息"
    :model-value="true"
    width="450px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="80px"
    >
      <el-form-item label="用户名">
        <el-input v-model="form.username" disabled />
      </el-form-item>
      <el-form-item label="显示名称" prop="displayName">
        <el-input v-model="form.displayName" placeholder="请输入显示名称" />
      </el-form-item>
      <el-form-item label="邮箱" prop="email">
        <el-input v-model="form.email" placeholder="请输入邮箱" />
      </el-form-item>
      <el-form-item label="角色">
        <el-tag :type="authStore.isAdmin ? 'danger' : 'info'">
          {{ authStore.isAdmin ? '管理员' : '普通用户' }}
        </el-tag>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="authStore.loading" @click="handleSubmit">
        保存
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/authStore'

const emit = defineEmits<{
  close: []
}>()

const authStore = useAuthStore()
const formRef = ref<FormInstance>()

const form = reactive({
  username: '',
  displayName: '',
  email: ''
})

const rules: FormRules = {
  email: [
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ]
}

onMounted(() => {
  if (authStore.user) {
    form.username = authStore.user.username
    form.displayName = authStore.user.displayName || ''
    form.email = authStore.user.email || ''
  }
})

const handleClose = () => {
  emit('close')
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      const success = await authStore.updateProfile({
        displayName: form.displayName || undefined,
        email: form.email || undefined
      })
      if (success) {
        handleClose()
      }
    }
  })
}
</script>
