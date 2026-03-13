<template>
  <el-dialog
    v-model="visible"
    :title="`版本历史 - ${fileName}`"
    width="700px"
    @close="handleClose"
  >
    <div class="version-history" v-loading="loading">
      <el-empty v-if="!loading && versions.length === 0" description="暂无版本历史" />

      <div v-else class="version-list">
        <div
          v-for="version in versions"
          :key="version.versionToken"
          class="version-item"
        >
          <div class="version-info">
            <div class="version-header">
              <span class="version-number">版本 {{ version.versionNumber }}</span>
              <el-tag v-if="version.isCurrentVersion" type="success" size="small">当前版本</el-tag>
            </div>
            <div class="version-meta">
              <span>{{ formatDateTime(version.createdTime) }}</span>
              <span>{{ formatFileSize(version.fileSize) }}</span>
            </div>
          </div>

          <div class="version-actions">
            <el-button size="small" @click="handleDownload(version)">
              <el-icon><Download /></el-icon>
              下载
            </el-button>
            <el-button 
              v-if="!version.isCurrentVersion" 
              size="small" 
              @click="handleRestore(version)"
            >
              <el-icon><RefreshRight /></el-icon>
              恢复
            </el-button>
            <el-button 
              v-if="!version.isCurrentVersion" 
              size="small" 
              type="danger" 
              plain
              @click="handleDelete(version)"
            >
              <el-icon><Delete /></el-icon>
              删除
            </el-button>
          </div>
        </div>
      </div>
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { versionApi } from '@/api'
import type { VersionResponse } from '@/api/types'
import { formatDateTime, formatFileSize, downloadBlob } from '@/utils/format'

const props = defineProps<{
  fileToken: string
  fileName: string
}>()

const emit = defineEmits<{
  close: []
}>()

const visible = ref(true)
const loading = ref(false)
const versions = ref<VersionResponse[]>([])

const loadVersions = async () => {
  loading.value = true
  try {
    const response = await versionApi.getList(props.fileToken)
    versions.value = response.versions || []
  } catch (error) {
    ElMessage.error('加载版本历史失败')
  } finally {
    loading.value = false
  }
}

const handleDownload = async (version: VersionResponse) => {
  try {
    const response = await versionApi.download(props.fileToken, version.versionToken)
    const blob = new Blob([response as any])
    const filename = `${props.fileName}_v${version.versionNumber}`
    downloadBlob(blob, filename)
    ElMessage.success('下载成功')
  } catch (error) {
    ElMessage.error('下载失败')
  }
}

const handleRestore = async (version: VersionResponse) => {
  try {
    await ElMessageBox.confirm('确定要恢复此版本吗？恢复后将成为当前版本。', '提示', {
      type: 'warning'
    })
    await versionApi.restore(props.fileToken, version.versionToken)
    ElMessage.success('恢复成功')
    loadVersions()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('恢复失败')
    }
  }
}

const handleDelete = async (version: VersionResponse) => {
  try {
    await ElMessageBox.confirm(`确定要删除版本 ${version.versionNumber} 吗？`, '提示', {
      type: 'warning'
    })
    await versionApi.delete(props.fileToken, version.versionToken)
    ElMessage.success('删除成功')
    loadVersions()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleClose = () => {
  emit('close')
}

onMounted(() => {
  loadVersions()
})
</script>

<style scoped lang="scss">
.version-history {
  min-height: 200px;
}

.version-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.version-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  transition: all 0.2s;

  &:hover {
    background-color: var(--el-fill-color-light);
  }

  .version-info {
    flex: 1;

    .version-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 8px;

      .version-number {
        font-weight: 600;
        font-size: 14px;
      }
    }

    .version-meta {
      display: flex;
      gap: 16px;
      font-size: 12px;
      color: var(--el-text-color-secondary);
    }
  }

  .version-actions {
    display: flex;
    gap: 8px;
  }
}
</style>
