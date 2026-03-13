<template>
  <el-dialog
    v-model="visible"
    title="新建文件夹"
    width="500px"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="文件夹名称" prop="name">
        <el-input v-model="form.name" placeholder="请输入文件夹名称" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage, FormInstance } from 'element-plus'
import { folderApi } from '@/api'

const props = defineProps<{
  parentToken?: string
}>()

const emit = defineEmits<{
  close: []
  success: []
}>()

const visible = ref(true)
const loading = ref(false)
const formRef = ref<FormInstance>()

const form = reactive({
  name: ''
})

const rules = {
  name: [
    { required: true, message: '请输入文件夹名称', trigger: 'blur' },
    { max: 100, message: '文件夹名称不能超过100个字符', trigger: 'blur' }
  ]
}

const handleSubmit = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        await folderApi.create({
          name: form.name,
          parentFolderToken: props.parentToken
        })
        ElMessage.success('创建成功')
        emit('success')
        handleClose()
      } catch (error) {
        ElMessage.error('创建失败')
      } finally {
        loading.value = false
      }
    }
  })
}

const handleClose = () => {
  emit('close')
}
</script>
