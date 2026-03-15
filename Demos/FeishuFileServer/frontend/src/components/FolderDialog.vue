<template>
  <el-dialog
    v-model="visible"
    :title="dialogTitle"
    width="400px"
    append-to-body
    @close="handleClose"
  >
    <el-form :model="form" :rules="rules" ref="formRef">
      <el-form-item label="文件夹名称" prop="name">
        <el-input 
          v-model="form.name" 
          placeholder="请输入文件夹名称"
          @keyup.enter="handleSubmit"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">
        {{ submitText }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { folderApi } from '@/api'
import type { FolderResponse } from '@/api/types'

export type FolderDialogMode = 'create' | 'rename'

interface Props {
  modelValue: boolean
  mode: FolderDialogMode
  folder?: FolderResponse | null
  parentFolder?: FolderResponse | null
  parentToken?: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const dialogTitle = computed(() => {
  if (props.mode === 'create') {
    if (props.parentFolder) {
      return `在"${props.parentFolder.folderName}"中新建文件夹`
    }
    return '新建文件夹'
  }
  return '重命名文件夹'
})

const submitText = computed(() => {
  return props.mode === 'create' ? '创建' : '确定'
})

const formRef = ref<FormInstance>()
const loading = ref(false)
const form = reactive({
  name: ''
})

const rules: FormRules = {
  name: [
    { required: true, message: '请输入文件夹名称', trigger: 'blur' },
    { min: 1, max: 100, message: '文件夹名称长度为1-100个字符', trigger: 'blur' }
  ]
}

watch(() => props.modelValue, (val) => {
  if (val) {
    if (props.mode === 'rename' && props.folder) {
      form.name = props.folder.folderName
    } else {
      form.name = ''
    }
  }
})

const handleClose = () => {
  emit('update:modelValue', false)
  formRef.value?.resetFields()
}

const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    loading.value = true

    if (props.mode === 'create') {
      const parentFolderToken = props.parentFolder?.folderToken ?? props.parentToken
      await folderApi.create({
        name: form.name,
        parentFolderToken
      })
      ElMessage.success('文件夹创建成功')
    } else if (props.folder) {
      await folderApi.update(props.folder.folderToken, {
        name: form.name
      })
      ElMessage.success('文件夹重命名成功')
    }

    emit('success')
    handleClose()
  } catch (error: any) {
    if (error.response?.data?.message) {
      ElMessage.error(error.response.data.message)
    } else if (error !== false) {
      ElMessage.error(props.mode === 'create' ? '创建文件夹失败' : '重命名失败')
    }
  } finally {
    loading.value = false
  }
}
</script>
