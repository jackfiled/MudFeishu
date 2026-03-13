<template>
  <el-dialog
    v-model="visible"
    :title="type === 'file' ? '重命名文件' : '重命名文件夹'"
    width="500px"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="新名称" prop="name">
        <el-input v-model="form.name" :placeholder="`请输入新${type === 'file' ? '文件' : '文件夹'}名称`" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { ElMessage, FormInstance } from 'element-plus'

const props = defineProps<{
  currentName: string
  type: 'file' | 'folder'
}>()

const emit = defineEmits<{
  close: []
  confirm: [newName: string]
}>()

const visible = ref(true)
const loading = ref(false)
const formRef = ref<FormInstance>()

const form = reactive({
  name: ''
})

const rules = {
  name: [
    { required: true, message: '请输入名称', trigger: 'blur' },
    { max: 100, message: '名称不能超过100个字符', trigger: 'blur' }
  ]
}

watch(() => props.currentName, (newName) => {
  form.name = newName
}, { immediate: true })

const handleSubmit = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        emit('confirm', form.name)
        handleClose()
      } catch (error) {
        ElMessage.error('操作失败')
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
