<template>
  <div class="chunk-uploader">
    <el-upload
      ref="uploadRef"
      :auto-upload="false"
      :show-file-list="false"
      :on-change="handleFileChange"
      multiple
    >
      <template #trigger>
        <el-button type="primary">
          <el-icon><Upload /></el-icon>
          选择文件
        </el-button>
      </template>
    </el-upload>

    <div v-if="uploadTasks.length > 0" class="upload-tasks">
      <div v-for="task in uploadTasks" :key="task.uploadId" class="upload-task">
        <div class="task-header">
          <el-icon class="file-icon"><Document /></el-icon>
          <div class="file-info">
            <span class="file-name">{{ task.fileName }}</span>
            <span class="file-size">{{ formatFileSize(task.fileSize) }}</span>
          </div>
          <div class="task-actions">
            <el-button 
              v-if="task.status === 'uploading'" 
              size="small" 
              @click="pauseTask(task)"
            >
              暂停
            </el-button>
            <el-button 
              v-if="task.status === 'paused'" 
              size="small" 
              type="primary"
              @click="resumeTask(task)"
            >
              继续
            </el-button>
            <el-button 
              size="small" 
              type="danger"
              @click="cancelTask(task)"
            >
              取消
            </el-button>
          </div>
        </div>
        
        <el-progress 
          :percentage="task.progress" 
          :status="task.status === 'completed' ? 'success' : undefined"
          :stroke-width="8"
        />
        
        <div class="task-status">
          <span v-if="task.status === 'pending'">等待上传</span>
          <span v-else-if="task.status === 'uploading'">
            上传中 {{ task.uploadedChunks }}/{{ task.totalChunks }} 分片
          </span>
          <span v-else-if="task.status === 'paused'">已暂停</span>
          <span v-else-if="task.status === 'completed'" class="success">上传完成</span>
          <span v-else-if="task.status === 'error'" class="error">{{ task.error }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Upload, Document } from '@element-plus/icons-vue'
import { chunkUploadApi } from '@/api'
import type { UploadFile } from 'element-plus'

interface UploadTask {
  uploadId: string
  fileName: string
  fileSize: number
  chunkSize: number
  totalChunks: number
  uploadedChunks: number
  progress: number
  status: 'pending' | 'uploading' | 'paused' | 'completed' | 'error'
  error?: string
  file: File
  abortController?: AbortController
}

const props = defineProps<{
  folderToken?: string
}>()

const emit = defineEmits<{
  uploaded: [fileToken: string, fileName: string]
}>()

const uploadTasks = ref<UploadTask[]>([])
const CHUNK_SIZE = 4 * 1024 * 1024

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

const handleFileChange = async (uploadFile: UploadFile) => {
  const file = uploadFile.raw
  if (!file) return

  const task = await initUpload(file)
  if (task) {
    uploadTasks.value.push(task)
    startUpload(task)
  }
}

const initUpload = async (file: File): Promise<UploadTask | null> => {
  try {
    const response = await chunkUploadApi.init({
      fileName: file.name,
      fileSize: file.size,
      chunkSize: CHUNK_SIZE,
      folderToken: props.folderToken
    })

    return {
      uploadId: response.uploadId,
      fileName: file.name,
      fileSize: file.size,
      chunkSize: response.chunkSize,
      totalChunks: response.totalChunks,
      uploadedChunks: 0,
      progress: 0,
      status: 'pending',
      file: file
    }
  } catch (error: any) {
    ElMessage.error(`初始化上传失败: ${error.response?.data?.message || error.message}`)
    return null
  }
}

const startUpload = async (task: UploadTask) => {
  task.status = 'uploading'
  task.abortController = new AbortController()

  for (let i = task.uploadedChunks; i < task.totalChunks; i++) {
    if (task.status !== 'uploading') {
      return
    }

    try {
      const start = i * task.chunkSize
      const end = Math.min(start + task.chunkSize, task.fileSize)
      const chunk = task.file.slice(start, end)

      await chunkUploadApi.uploadChunk(
        task.uploadId,
        i,
        chunk,
        (progressEvent) => {
          const chunkProgress = (progressEvent.loaded / progressEvent.total) * 100
          const overallProgress = ((i + chunkProgress / 100) / task.totalChunks) * 100
          task.progress = Math.round(overallProgress * 100) / 100
        }
      )

      task.uploadedChunks = i + 1
      task.progress = Math.round(((i + 1) / task.totalChunks) * 100 * 100) / 100
    } catch (error: any) {
      task.status = 'error'
      task.error = error.response?.data?.message || error.message
      return
    }
  }

  try {
    const result = await chunkUploadApi.complete(task.uploadId)
    task.status = 'completed'
    task.progress = 100
    if (result.fileToken) {
      emit('uploaded', result.fileToken, task.fileName)
    }
    ElMessage.success(`${task.fileName} 上传成功`)
  } catch (error: any) {
    task.status = 'error'
    task.error = error.response?.data?.message || error.message
  }
}

const pauseTask = (task: UploadTask) => {
  task.status = 'paused'
  task.abortController?.abort()
}

const resumeTask = (task: UploadTask) => {
  startUpload(task)
}

const cancelTask = async (task: UploadTask) => {
  task.abortController?.abort()
  
  try {
    await chunkUploadApi.cancel(task.uploadId)
  } catch {
    // ignore
  }

  const index = uploadTasks.value.findIndex(t => t.uploadId === task.uploadId)
  if (index > -1) {
    uploadTasks.value.splice(index, 1)
  }
}

defineExpose({
  handleFileChange
})
</script>

<style scoped lang="scss">
.chunk-uploader {
  width: 100%;
}

.upload-tasks {
  margin-top: var(--spacing-md);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.upload-task {
  padding: var(--spacing-md);
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-color);
}

.task-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-sm);
}

.file-icon {
  font-size: 24px;
  color: var(--primary-color);
}

.file-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.file-name {
  font-weight: 500;
  font-size: 14px;
}

.file-size {
  font-size: 12px;
  color: var(--text-secondary);
}

.task-actions {
  display: flex;
  gap: var(--spacing-xs);
}

.task-status {
  margin-top: var(--spacing-sm);
  font-size: 12px;
  color: var(--text-secondary);

  .success {
    color: var(--success-color);
  }

  .error {
    color: var(--danger-color);
  }
}
</style>
