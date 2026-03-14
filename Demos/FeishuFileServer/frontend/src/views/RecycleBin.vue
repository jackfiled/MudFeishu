<template>
  <div class="recycle-bin">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>回收站</span>
          <el-button type="danger" @click="handleEmptyRecycleBin" :disabled="totalItems === 0">
            清空回收站
          </el-button>
        </div>
      </template>

      <el-tabs v-model="activeTab">
        <el-tab-pane label="文件" name="files">
          <el-table :data="files" v-loading="loading" style="width: 100%">
            <el-table-column prop="fileName" label="文件名" min-width="200" />
            <el-table-column prop="fileSize" label="大小" width="120">
              <template #default="{ row }">
                {{ formatFileSize(row.fileSize) }}
              </template>
            </el-table-column>
            <el-table-column prop="uploadTime" label="删除时间" width="180">
              <template #default="{ row }">
                {{ formatDate(row.uploadTime) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right">
              <template #default="{ row }">
                <el-button type="primary" size="small" @click="handleRestoreFile(row)">
                  恢复
                </el-button>
                <el-button type="danger" size="small" @click="handleDeleteFilePermanently(row)">
                  永久删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="文件夹" name="folders">
          <el-table :data="folders" v-loading="loading" style="width: 100%">
            <el-table-column prop="folderName" label="文件夹名" min-width="200" />
            <el-table-column prop="createdTime" label="删除时间" width="180">
              <template #default="{ row }">
                {{ formatDate(row.createdTime) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right">
              <template #default="{ row }">
                <el-button type="primary" size="small" @click="handleRestoreFolder(row)">
                  恢复
                </el-button>
                <el-button type="danger" size="small" @click="handleDeleteFolderPermanently(row)">
                  永久删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>

      <el-pagination
        v-model:current-page="page"
        :page-size="pageSize"
        :total="activeTab === 'files' ? totalFiles : totalFolders"
        layout="total, prev, pager, next"
        @current-change="handlePageChange"
        style="margin-top: 20px; justify-content: flex-end;"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { recycleBinApi } from '@/api'
import type { FileInfoResponse, FolderResponse } from '@/api/types'

const activeTab = ref('files')
const loading = ref(false)
const page = ref(1)
const pageSize = ref(20)

const files = ref<FileInfoResponse[]>([])
const folders = ref<FolderResponse[]>([])
const totalFiles = ref(0)
const totalFolders = ref(0)

const totalItems = computed(() => totalFiles.value + totalFolders.value)

const loadDeletedFiles = async () => {
  try {
    loading.value = true
    const response = await recycleBinApi.getDeletedFiles(page.value, pageSize.value)
    files.value = response.files
    totalFiles.value = response.totalCount
  } catch (error) {
    ElMessage.error('加载已删除文件失败')
  } finally {
    loading.value = false
  }
}

const loadDeletedFolders = async () => {
  try {
    loading.value = true
    const response = await recycleBinApi.getDeletedFolders(page.value, pageSize.value)
    folders.value = response.folders
    totalFolders.value = response.totalCount
  } catch (error) {
    ElMessage.error('加载已删除文件夹失败')
  } finally {
    loading.value = false
  }
}

const handleRestoreFile = async (file: FileInfoResponse) => {
  try {
    await recycleBinApi.restoreFile(file.fileToken)
    ElMessage.success('文件已恢复')
    loadDeletedFiles()
  } catch (error) {
    ElMessage.error('恢复文件失败')
  }
}

const handleRestoreFolder = async (folder: FolderResponse) => {
  try {
    await recycleBinApi.restoreFolder(folder.folderToken)
    ElMessage.success('文件夹已恢复')
    loadDeletedFolders()
  } catch (error) {
    ElMessage.error('恢复文件夹失败')
  }
}

const handleDeleteFilePermanently = async (file: FileInfoResponse) => {
  try {
    await ElMessageBox.confirm('永久删除后无法恢复，确定要删除吗？', '警告', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await recycleBinApi.permanentlyDeleteFile(file.fileToken)
    ElMessage.success('文件已永久删除')
    loadDeletedFiles()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('永久删除文件失败')
    }
  }
}

const handleDeleteFolderPermanently = async (folder: FolderResponse) => {
  try {
    await ElMessageBox.confirm('永久删除后无法恢复，确定要删除吗？', '警告', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await recycleBinApi.permanentlyDeleteFolder(folder.folderToken)
    ElMessage.success('文件夹已永久删除')
    loadDeletedFolders()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('永久删除文件夹失败')
    }
  }
}

const handleEmptyRecycleBin = async () => {
  try {
    await ElMessageBox.confirm('清空回收站后所有文件将无法恢复，确定要清空吗？', '警告', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await recycleBinApi.emptyRecycleBin()
    ElMessage.success('回收站已清空')
    loadDeletedFiles()
    loadDeletedFolders()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('清空回收站失败')
    }
  }
}

const handlePageChange = () => {
  if (activeTab.value === 'files') {
    loadDeletedFiles()
  } else {
    loadDeletedFolders()
  }
}

watch(activeTab, () => {
  page.value = 1
  if (activeTab.value === 'files') {
    loadDeletedFiles()
  } else {
    loadDeletedFolders()
  }
})

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
  loadDeletedFiles()
})
</script>

<style scoped>
.recycle-bin {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
