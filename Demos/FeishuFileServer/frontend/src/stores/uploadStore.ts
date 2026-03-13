import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export type UploadStatus = 'pending' | 'uploading' | 'paused' | 'completed' | 'failed'

export interface UploadTask {
  id: string
  file: File
  fileToken?: string
  folderToken?: string
  status: UploadStatus
  progress: number
  uploadedSize: number
  totalSize: number
  error?: string
  startTime?: number
  endTime?: number
}

export const useUploadStore = defineStore('upload', () => {
  const tasks = ref<UploadTask[]>([])
  const uploadingCount = computed(() => tasks.value.filter(t => t.status === 'uploading').length)
  const pendingCount = computed(() => tasks.value.filter(t => t.status === 'pending').length)
  const completedCount = computed(() => tasks.value.filter(t => t.status === 'completed').length)
  const failedCount = computed(() => tasks.value.filter(t => t.status === 'failed').length)
  const maxConcurrent = ref(3)
  const chunkSize = ref(5 * 1024 * 1024)

  function generateId(): string {
    return `upload_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  }

  function addTask(file: File, folderToken?: string): UploadTask {
    const task: UploadTask = {
      id: generateId(),
      file,
      folderToken,
      status: 'pending',
      progress: 0,
      uploadedSize: 0,
      totalSize: file.size
    }
    tasks.value.push(task)
    return task
  }

  function updateTask(id: string, updates: Partial<UploadTask>) {
    const task = tasks.value.find(t => t.id === id)
    if (task) {
      Object.assign(task, updates)
    }
  }

  function removeTask(id: string) {
    const index = tasks.value.findIndex(t => t.id === id)
    if (index > -1) {
      tasks.value.splice(index, 1)
    }
  }

  function getTask(id: string): UploadTask | undefined {
    return tasks.value.find(t => t.id === id)
  }

  function clearCompleted() {
    tasks.value = tasks.value.filter(t => t.status !== 'completed')
  }

  function clearAll() {
    tasks.value = []
  }

  function pauseTask(id: string) {
    const task = tasks.value.find(t => t.id === id)
    if (task && task.status === 'uploading') {
      task.status = 'paused'
    }
  }

  function resumeTask(id: string) {
    const task = tasks.value.find(t => t.id === id)
    if (task && task.status === 'paused') {
      task.status = 'pending'
    }
  }

  function retryTask(id: string) {
    const task = tasks.value.find(t => t.id === id)
    if (task && task.status === 'failed') {
      task.status = 'pending'
      task.progress = 0
      task.uploadedSize = 0
      task.error = undefined
    }
  }

  return {
    tasks,
    uploadingCount,
    pendingCount,
    completedCount,
    failedCount,
    maxConcurrent,
    chunkSize,
    addTask,
    updateTask,
    removeTask,
    getTask,
    clearCompleted,
    clearAll,
    pauseTask,
    resumeTask,
    retryTask
  }
})
