<template>
  <el-drawer
    v-model="visible"
    title="上传文件"
    direction="btt"
    size="400px"
    :show-close="true"
    @close="handleClose"
  >
    <div class="upload-container">
      <div
        class="upload-drop-zone"
        :class="{ dragover: isDragover }"
        @dragover.prevent="handleDragover"
        @dragleave.prevent="handleDragleave"
        @drop.prevent="handleDrop"
        @click="handleClick"
      >
        <el-icon class="upload-icon"><UploadFilled /></el-icon>
        <p class="upload-text">拖拽文件到此处或点击选择文件</p>
        <p class="upload-hint">支持 {{ allowedExtensions.join(', ') }} 文件</p>
        <p class="upload-hint">最大文件大小: {{ maxFileSize }}MB</p>
      </div>

      <input
        ref="fileInput"
        type="file"
        multiple
        :accept="acceptedTypes"
        style="display: none"
        @change="handleFileSelect"
      />

      <div class="upload-queue" v-if="uploadStore.tasks.length > 0">
        <div class="queue-header">
          <span>上传队列 ({{ uploadStore.tasks.length }})</span>
          <el-button text @click="uploadStore.clearCompleted">清除已完成</el-button>
        </div>

        <div class="queue-list">
          <div
            v-for="task in uploadStore.tasks"
            :key="task.id"
            class="queue-item"
          >
            <div class="queue-item-info">
              <el-icon class="file-icon"><Document /></el-icon>
              <div class="file-info">
                <div class="file-name">{{ task.file.name }}</div>
                <div class="file-size">{{ formatFileSize(task.file.size) }}</div>
              </div>
            </div>

            <div class="queue-item-progress">
              <el-progress
                v-if="task.status === 'uploading'"
                :percentage="task.progress"
                :status="undefined"
              />
              <div v-else class="status-text">
                <el-tag v-if="task.status === 'pending'" type="info" size="small">等待中</el-tag>
                <el-tag v-else-if="task.status === 'completed'" type="success" size="small">已完成</el-tag>
                <el-tag v-else-if="task.status === 'failed'" type="danger" size="small">失败</el-tag>
                <el-tag v-else-if="task.status === 'paused'" type="warning" size="small">已暂停</el-tag>
              </div>
            </div>

            <div class="queue-item-actions">
              <el-button
                v-if="task.status === 'uploading'"
                text
                size="small"
                @click="uploadStore.pauseTask(task.id)"
              >
                <el-icon><VideoPause /></el-icon>
              </el-button>
              <el-button
                v-else-if="task.status === 'paused'"
                text
                size="small"
                @click="uploadStore.resumeTask(task.id)"
              >
                <el-icon><VideoPlay /></el-icon>
              </el-button>
              <el-button
                v-if="task.status === 'failed'"
                text
                size="small"
                @click="uploadStore.retryTask(task.id)"
              >
                <el-icon><Refresh /></el-icon>
              </el-button>
              <el-button
                text
                size="small"
                @click="uploadStore.removeTask(task.id)"
              >
                <el-icon><Close /></el-icon>
              </el-button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useUploadStore } from '@/stores/uploadStore'
import { fileApi } from '@/api'
import { formatFileSize } from '@/utils/format'

const props = defineProps<{
  folderToken?: string
}>()

const emit = defineEmits<{
  close: []
  success: []
}>()

const uploadStore = useUploadStore()

const visible = ref(true)
const fileInput = ref<HTMLInputElement | null>(null)
const isDragover = ref(false)

const allowedExtensions = ref(['.docx', '.xlsx', '.pptx', '.png', '.jpg', '.jpeg', '.tiff', '.pdf'])
const maxFileSize = ref(100)

const acceptedTypes = computed(() => allowedExtensions.value.join(','))

const isValidFile = (file: File): boolean => {
  const ext = '.' + file.name.split('.').pop()?.toLowerCase()
  if (!allowedExtensions.value.includes(ext)) {
    ElMessage.error(`不支持的文件类型: ${ext}`)
    return false
  }
  if (file.size > maxFileSize.value * 1024 * 1024) {
    ElMessage.error(`文件大小不能超过 ${maxFileSize.value}MB`)
    return false
  }
  return true
}

const uploadFile = async (task: any) => {
  const formData = new FormData()
  formData.append('file', task.file)
  if (task.folderToken) {
    formData.append('folderToken', task.folderToken)
  }

  uploadStore.updateTask(task.id, { status: 'uploading', startTime: Date.now() })

  try {
    await fileApi.upload(formData, task.folderToken, (progressEvent) => {
      const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total)
      uploadStore.updateTask(task.id, { progress, uploadedSize: progressEvent.loaded })
    })
    uploadStore.updateTask(task.id, { 
      status: 'completed', 
      progress: 100, 
      endTime: Date.now() 
    })
    ElMessage.success(`${task.file.name} 上传成功`)
    emit('success')
  } catch (error: any) {
    uploadStore.updateTask(task.id, { 
      status: 'failed', 
      error: error.message || '上传失败',
      endTime: Date.now()
    })
    ElMessage.error(`${task.file.name} 上传失败`)
  }
}

const handleFiles = (files: FileList) => {
  Array.from(files).forEach(file => {
    if (isValidFile(file)) {
      const task = uploadStore.addTask(file, props.folderToken)
      uploadFile(task)
    }
  })
}

const handleDragover = () => {
  isDragover.value = true
}

const handleDragleave = () => {
  isDragover.value = false
}

const handleDrop = (event: DragEvent) => {
  isDragover.value = false
  if (event.dataTransfer?.files) {
    handleFiles(event.dataTransfer.files)
  }
}

const handleClick = () => {
  fileInput.value?.click()
}

const handleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files) {
    handleFiles(target.files)
  }
}

const handleClose = () => {
  emit('close')
}

watch(() => props.folderToken, (newToken) => {
  if (newToken) {
    uploadStore.tasks.forEach(task => {
      if (task.status === 'pending') {
        task.folderToken = newToken
      }
    })
  }
})
</script>

<style scoped lang="scss">
.upload-container {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.upload-drop-zone {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px;
  border: 2px dashed var(--el-border-color);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;

  &:hover, &.dragover {
    border-color: var(--el-color-primary);
    background-color: var(--el-color-primary-light-9);
  }

  .upload-icon {
    font-size: 48px;
    color: var(--el-color-primary);
    margin-bottom: 16px;
  }

  .upload-text {
    font-size: 16px;
    margin-bottom: 8px;
  }

  .upload-hint {
    font-size: 12px;
    color: var(--el-text-color-secondary);
    margin-top: 4px;
  }
}

.upload-queue {
  flex: 1;
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  overflow: hidden;

  .queue-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 0;
    margin-bottom: 8px;
    font-weight: 500;
  }

  .queue-list {
    flex: 1;
    overflow-y: auto;
  }

  .queue-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px;
    border: 1px solid var(--el-border-color-light);
    border-radius: 4px;
    margin-bottom: 8px;

    .queue-item-info {
      display: flex;
      align-items: center;
      gap: 8px;
      flex: 1;
      min-width: 0;

      .file-icon {
        font-size: 24px;
        color: var(--el-color-primary);
      }

      .file-info {
        flex: 1;
        min-width: 0;

        .file-name {
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }

        .file-size {
          font-size: 12px;
          color: var(--el-text-color-secondary);
        }
      }
    }

    .queue-item-progress {
      width: 120px;

      .status-text {
        display: flex;
        justify-content: center;
      }
    }

    .queue-item-actions {
      display: flex;
      gap: 4px;
    }
  }
}
</style>
