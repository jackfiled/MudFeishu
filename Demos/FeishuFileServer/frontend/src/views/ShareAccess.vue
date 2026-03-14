<template>
  <div class="share-access">
    <el-card v-if="loading" style="text-align: center; padding: 40px;">
      <el-icon class="is-loading" :size="40"><Loading /></el-icon>
      <p>加载中...</p>
    </el-card>

    <el-card v-else-if="error">
      <el-result icon="error" title="访问失败" :sub-title="error">
        <template #extra>
          <el-button type="primary" @click="router.push('/')">返回首页</el-button>
        </template>
      </el-result>
    </el-card>

    <el-card v-else-if="requirePassword && !authenticated">
      <template #header>
        <span>{{ shareContent?.resourceName || '访问分享' }}</span>
      </template>
      <el-form @submit.prevent="handlePasswordSubmit">
        <el-form-item label="访问密码">
          <el-input
            v-model="password"
            type="password"
            placeholder="请输入访问密码"
            show-password
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" native-type="submit" :loading="submitting">
            确认
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card v-else>
      <template #header>
        <div class="card-header">
          <span>{{ shareContent?.resourceName }}</span>
          <div class="actions">
            <el-button
              v-if="shareContent?.allowDownload && shareContent?.resourceType === 'File'"
              type="primary"
              @click="handleDownload"
            >
              下载
            </el-button>
          </div>
        </div>
      </template>

      <div v-if="shareContent?.resourceType === 'File' && shareContent?.file">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="文件名">{{ shareContent.file.fileName }}</el-descriptions-item>
          <el-descriptions-item label="大小">{{ formatFileSize(shareContent.file.fileSize) }}</el-descriptions-item>
          <el-descriptions-item label="类型">{{ shareContent.file.mimeType }}</el-descriptions-item>
          <el-descriptions-item label="上传时间">{{ formatDate(shareContent.file.uploadTime) }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div v-else-if="shareContent?.resourceType === 'Folder' && shareContent?.folderContents">
        <el-tabs>
          <el-tab-pane label="文件">
            <el-table :data="shareContent.folderContents.files" style="width: 100%">
              <el-table-column prop="fileName" label="文件名" min-width="200" />
              <el-table-column prop="fileSize" label="大小" width="120">
                <template #default="{ row }">
                  {{ formatFileSize(row.fileSize) }}
                </template>
              </el-table-column>
              <el-table-column prop="uploadTime" label="上传时间" width="180">
                <template #default="{ row }">
                  {{ formatDate(row.uploadTime) }}
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
          <el-tab-pane label="文件夹">
            <el-table :data="shareContent.folderContents.folders" style="width: 100%">
              <el-table-column prop="folderName" label="文件夹名" min-width="200" />
              <el-table-column prop="createdTime" label="创建时间" width="180">
                <template #default="{ row }">
                  {{ formatDate(row.createdTime) }}
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
        </el-tabs>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import { shareApi } from '@/api'
import type { ShareContentResponse } from '@/api/types'

const route = useRoute()
const router = useRouter()

const shareCode = computed(() => route.params.shareCode as string)
const loading = ref(true)
const submitting = ref(false)
const error = ref('')
const password = ref('')
const requirePassword = ref(false)
const authenticated = ref(false)
const shareContent = ref<ShareContentResponse | null>(null)

const loadShare = async (pwd?: string) => {
  try {
    loading.value = true
    error.value = ''
    shareContent.value = await shareApi.access(shareCode.value, pwd)
    authenticated.value = true
  } catch (err: any) {
    if (err.response?.status === 401) {
      requirePassword.value = true
      if (pwd) {
        ElMessage.error('密码错误')
      }
    } else if (err.response?.status === 404) {
      error.value = '分享不存在或已失效'
    } else if (err.response?.status === 400) {
      error.value = err.response?.data?.message || '分享已过期或访问次数已达上限'
    } else {
      error.value = '加载分享失败'
    }
  } finally {
    loading.value = false
  }
}

const handlePasswordSubmit = async () => {
  if (!password.value) {
    ElMessage.warning('请输入访问密码')
    return
  }
  submitting.value = true
  await loadShare(password.value)
  submitting.value = false
}

const handleDownload = async () => {
  try {
    const response = await shareApi.download(shareCode.value, password.value || undefined)
    const blob = response.data as Blob
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = shareContent.value?.resourceName || 'download'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    window.URL.revokeObjectURL(url)
  } catch (err) {
    ElMessage.error('下载失败')
  }
}

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

const formatDate = (dateStr: string): string => {
  return new Date(dateStr).toLocaleString('zh-CN')
}

onMounted(() => {
  loadShare()
})
</script>

<style scoped>
.share-access {
  max-width: 1000px;
  margin: 20px auto;
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.actions {
  display: flex;
  gap: 10px;
}
</style>
