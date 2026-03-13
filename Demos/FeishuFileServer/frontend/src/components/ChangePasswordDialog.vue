<template>
  <el-dialog
    title="修改密码"
    :model-value="true"
    width="450px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="当前密码" prop="currentPassword">
        <el-input
          v-model="form.currentPassword"
          type="password"
          placeholder="请输入当前密码"
          show-password
        />
      </el-form-item>
      <el-form-item label="新密码" prop="newPassword">
        <el-input
          v-model="form.newPassword"
          type="password"
          placeholder="请输入新密码"
          show-password
        />
      </el-form-item>
      <el-form-item label="确认新密码" prop="confirmPassword">
        <el-input
          v-model="form.confirmPassword"
          type="password"
          placeholder="请再次输入新密码"
          show-password
        />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="authStore.loading" @click="handleSubmit">
        确认修改
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/authStore'

const emit = defineEmits<{
  close: []
}>()

const authStore = useAuthStore()
const formRef = ref<FormInstance>()

const form = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const validateConfirmPassword = (_rule: any, value: string, callback: any) => {
  if (value !== form.newPassword) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const rules: FormRules = {
  currentPassword: [
    { required: true, message: '请输入当前密码', trigger: 'blur' }
  ],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度至少为6个字符', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

const handleClose = () => {
  emit('close')
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      const success = await authStore.changePassword({
        currentPassword: form.currentPassword,
        newPassword: form.newPassword
      })
      if (success) {
        form.currentPassword = ''
        form.newPassword = ''
        form.confirmPassword = ''
        handleClose()
      }
    }
  })
}
</script>
