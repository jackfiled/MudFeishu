import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { FileInfoResponse } from '@/api/types'

export const useFileStore = defineStore('files', () => {
  const files = ref<FileInfoResponse[]>([])
  const currentFile = ref<FileInfoResponse | null>(null)
  const selectedFiles = ref<string[]>([])
  const viewMode = ref<'list' | 'grid'>('list')
  const sortField = ref<string>('uploadTime')
  const sortOrder = ref<'asc' | 'desc'>('desc')
  const loading = ref(false)
  const totalCount = ref(0)
  const page = ref(1)
  const pageSize = ref(20)

  const hasSelected = computed(() => selectedFiles.value.length > 0)
  const selectedCount = computed(() => selectedFiles.value.length)

  function setFiles(newFiles: FileInfoResponse[], total: number) {
    files.value = newFiles
    totalCount.value = total
  }

  function addFile(file: FileInfoResponse) {
    files.value.unshift(file)
    totalCount.value++
  }

  function removeFile(fileToken: string) {
    const index = files.value.findIndex(f => f.fileToken === fileToken)
    if (index > -1) {
      files.value.splice(index, 1)
      totalCount.value--
    }
  }

  function setCurrentFile(file: FileInfoResponse | null) {
    currentFile.value = file
  }

  function toggleSelect(fileToken: string) {
    const index = selectedFiles.value.indexOf(fileToken)
    if (index > -1) {
      selectedFiles.value.splice(index, 1)
    } else {
      selectedFiles.value.push(fileToken)
    }
  }

  function selectAll() {
    selectedFiles.value = files.value.map(f => f.fileToken)
  }

  function clearSelection() {
    selectedFiles.value = []
  }

  function setViewMode(mode: 'list' | 'grid') {
    viewMode.value = mode
  }

  function setSort(field: string, order: 'asc' | 'desc') {
    sortField.value = field
    sortOrder.value = order
  }

  function setPage(newPage: number) {
    page.value = newPage
  }

  function setLoading(isLoading: boolean) {
    loading.value = isLoading
  }

  return {
    files,
    currentFile,
    selectedFiles,
    viewMode,
    sortField,
    sortOrder,
    loading,
    totalCount,
    page,
    pageSize,
    hasSelected,
    selectedCount,
    setFiles,
    addFile,
    removeFile,
    setCurrentFile,
    toggleSelect,
    selectAll,
    clearSelection,
    setViewMode,
    setSort,
    setPage,
    setLoading
  }
})
