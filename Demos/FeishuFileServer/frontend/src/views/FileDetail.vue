<template>
  <div class="file-detail" v-loading="loading">
    <div class="detail-header">
      <el-button text @click="goBack">
        <el-icon><ArrowLeft /></el-icon>
        返回
      </el-button>
    </div>

    <el-card v-if="fileInfo">
      <template #header>
        <div class="card-header">
          <el-icon :class="getFileIconClass(fileInfo.fileName)" size="32">
            <component :is="getFileIcon(fileInfo.fileName)" />
          </el-icon>
          <span class="file-name">{{ fileInfo.fileName }}</span>
        </div>
      </template>

      <div class="detail-content">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="文件大小">
            {{ formatFileSize(fileInfo.fileSize) }}
          </el-descriptions-item>
          <el-descriptions-item label="文件类型">
            {{ fileInfo.mimeType || '未知' }}
          </el-descriptions-item>
          <el-descriptions-item label="上传时间">
            {{ formatDateTime(fileInfo.uploadTime) }}
          </el-descriptions-item>
          <el-descriptions-item label="文件Token">
            {{ fileInfo.fileToken }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="detail-actions">
        <el-button type="primary" @click="handleDownload">
          <el-icon><Download /></el-icon>
          下载
        </el-button>
        <el-button @click="handleVersions">
          <el-icon><Clock /></el-icon>
          版本历史
        </el-button>
        <el-button type="danger" plain @click="handleDelete">
          <el-icon><Delete /></el-icon>
          删除
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { fileApi } from '@/api'
import type { FileInfoResponse } from '@/api/types'
import { formatFileSize, formatDateTime, getFileIcon, getFileIconClass, downloadBlob } from '@/utils/format'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const fileInfo = ref<FileInfoResponse | null>(null)

const loadFileInfo = async () => {
  const fileToken = route.params.fileToken as string
  if (!fileToken) return

  loading.value = true
  try {
    const response = await fileApi.getInfo(fileToken)
    fileInfo.value = response
  } catch (error) {
    ElMessage.error('加载文件信息失败')
  } finally {
    loading.value = false
  }
}

const goBack = () => {
  router.back()
}

const handleDownload = async () => {
  if (!fileInfo.value) return
  
  try {
    const response = await fileApi.download(fileInfo.value.fileToken)
    const blob = new Blob([response as any])
    downloadBlob(blob, fileInfo.value.fileName)
    ElMessage.success('下载成功')
  } catch (error) {
    ElMessage.error('下载失败')
  }
}

const handleVersions = () => {
  // 可以在这里打开版本历史对话框
}

const handleDelete = async () => {
  if (!fileInfo.value) return

  try {
    await ElMessageBox.confirm(`确定要删除 "${fileInfo.value.fileName}" 吗？`, '提示', {
      type: 'warning'
    })
    await fileApi.delete(fileInfo.value.fileToken)
    ElMessage.success('删除成功')
    router.back()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

onMounted(() => {
  loadFileInfo()
})
</script>

<style scoped lang="scss">
.file-detail {
  padding: 20px;
  height: 100vh;
  overflow-y: auto;
}

.detail-header {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 12px;

  .file-name {
    font-size: 18px;
    font-weight: 600;
  }
}

.detail-content {
  margin: 20px 0;
}

.detail-actions {
  display: flex;
  gap: 12px;
  justify-content: center;
  margin-top: 20px;
}
</style>
