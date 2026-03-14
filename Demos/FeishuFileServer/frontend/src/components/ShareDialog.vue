<template>
  <el-dialog
    v-model="visible"
    title="创建分享"
    width="500px"
    @close="handleClose"
  >
    <el-form :model="form" label-width="100px">
      <el-form-item label="资源类型">
        <el-input :value="form.resourceType === 'File' ? '文件' : '文件夹'" disabled />
      </el-form-item>
      <el-form-item label="资源名称">
        <el-input :value="resourceName" disabled />
      </el-form-item>
      <el-form-item label="访问密码">
        <el-input
          v-model="form.password"
          placeholder="留空则不需要密码"
          show-password
          clearable
        />
      </el-form-item>
      <el-form-item label="过期时间">
        <el-date-picker
          v-model="expireTimeDate"
          type="datetime"
          placeholder="留空则永不过期"
          :disabled-date="disabledDate"
        />
      </el-form-item>
      <el-form-item label="访问次数">
        <el-input-number
          v-model="form.maxAccessCount"
          :min="1"
          placeholder="留空则无限制"
          clearable
        />
      </el-form-item>
      <el-form-item label="允许下载">
        <el-switch v-model="form.allowDownload" />
      </el-form-item>
    </el-form>

    <template #footer v-if="!shareResult">
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleCreate" :loading="loading">
        创建分享
      </el-button>
    </template>

    <template #footer v-else>
      <el-button type="primary" @click="handleCopyLink">复制链接</el-button>
      <el-button @click="handleClose">关闭</el-button>
    </template>
  </el-dialog>

  <el-dialog
    v-model="showResult"
    title="分享成功"
    width="500px"
    :close-on-click-modal="false"
  >
    <el-form label-width="100px">
      <el-form-item label="分享链接">
        <el-input :value="shareLink" readonly>
          <template #append>
            <el-button @click="handleCopyLink">复制</el-button>
          </template>
        </el-input>
      </el-form-item>
      <el-form-item label="分享码">
        <el-input :value="shareResult?.shareCode" readonly />
      </el-form-item>
      <el-form-item label="需要密码">
        <el-tag :type="shareResult?.requirePassword ? 'warning' : 'success'">
          {{ shareResult?.requirePassword ? '是' : '否' }}
        </el-tag>
      </el-form-item>
      <el-form-item label="过期时间" v-if="shareResult?.expireTime">
        <el-text>{{ formatDate(shareResult.expireTime) }}</el-text>
      </el-form-item>
    </el-form>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { shareApi } from '@/api'
import type { ShareResponse, CreateShareRequest } from '@/api/types'

interface Props {
  modelValue: boolean
  resourceType: 'File' | 'Folder'
  resourceToken: string
  resourceName: string
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue', 'created'])

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const loading = ref(false)
const shareResult = ref<ShareResponse | null>(null)
const showResult = ref(false)
const expireTimeDate = ref<Date | null>(null)

const form = ref<Omit<CreateShareRequest, 'expireTime'> & { expireTime?: string }>({
  resourceType: 'File',
  resourceToken: '',
  password: '',
  maxAccessCount: undefined,
  allowDownload: true
})

const shareLink = computed(() => {
  if (!shareResult.value) return ''
  return `${window.location.origin}/share/${shareResult.value.shareCode}`
})

watch(() => props.modelValue, (val) => {
  if (val) {
    form.value = {
      resourceType: props.resourceType,
      resourceToken: props.resourceToken,
      password: '',
      maxAccessCount: undefined,
      allowDownload: true
    }
    expireTimeDate.value = null
    shareResult.value = null
    showResult.value = false
  }
})

const disabledDate = (date: Date) => {
  return date.getTime() < Date.now() - 86400000
}

const handleCreate = async () => {
  try {
    loading.value = true
    const request: CreateShareRequest = {
      resourceType: form.value.resourceType,
      resourceToken: form.value.resourceToken,
      password: form.value.password || undefined,
      expireTime: expireTimeDate.value ? expireTimeDate.value.toISOString() : undefined,
      maxAccessCount: form.value.maxAccessCount || undefined,
      allowDownload: form.value.allowDownload
    }
    shareResult.value = await shareApi.create(request)
    showResult.value = true
    ElMessage.success('分享创建成功')
    emit('created', shareResult.value)
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '创建分享失败')
  } finally {
    loading.value = false
  }
}

const handleCopyLink = async () => {
  try {
    await navigator.clipboard.writeText(shareLink.value)
    ElMessage.success('链接已复制到剪贴板')
  } catch (error) {
    ElMessage.error('复制失败')
  }
}

const handleClose = () => {
  visible.value = false
  showResult.value = false
}

const formatDate = (dateStr: string): string => {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>
