<template>
  <div class="sync-panel">
    <div class="sync-header">
      <h3>飞书云空间同步</h3>
      <p class="sync-description">将飞书云空间的文件夹和文件同步到本地系统</p>
    </div>

    <div class="sync-status" v-if="syncStatus">
      <div class="status-item">
        <el-icon :class="{ 'syncing': syncStatus.isSyncing }">
          <Loading v-if="syncStatus.isSyncing" />
          <Check v-else />
        </el-icon>
        <span class="status-text">
          {{ syncStatus.isSyncing ? '正在同步...' : '同步完成' }}
        </span>
      </div>
      
      <div class="progress-info" v-if="syncStatus.isSyncing">
        <el-progress 
          :percentage="syncStatus.progress" 
          :stroke-width="8"
          :show-text="true"
        />
        <p class="current-item" v-if="syncStatus.currentItem">
          {{ syncStatus.currentItem }}
        </p>
      </div>

      <div class="sync-stats" v-if="!syncStatus.isSyncing && lastSyncResult">
        <div class="stat-item">
          <span class="stat-value">{{ lastSyncResult.syncedFolders }}</span>
          <span class="stat-label">同步文件夹</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ lastSyncResult.syncedFiles }}</span>
          <span class="stat-label">同步文件</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ lastSyncResult.addedFolders }}</span>
          <span class="stat-label">新增文件夹</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ lastSyncResult.addedFiles }}</span>
          <span class="stat-label">新增文件</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ lastSyncResult.updatedFolders }}</span>
          <span class="stat-label">更新文件夹</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ lastSyncResult.updatedFiles }}</span>
          <span class="stat-label">更新文件</span>
        </div>
      </div>
    </div>

    <div class="sync-actions">
      <el-button 
        type="primary" 
        @click="handleSyncAll"
        :loading="isSyncing"
        :disabled="isSyncing"
      >
        <el-icon><Refresh /></el-icon>
        开始同步
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, Check, Loading } from '@element-plus/icons-vue'
import { syncApi } from '@/api'
import type { SyncResult, SyncStatus } from '@/api/types'

const emit = defineEmits<{
  'sync-complete': []
}>()

const syncStatus = ref<SyncStatus | null>(null)
const lastSyncResult = ref<SyncResult | null>(null)
const isSyncing = ref(false)

const loadSyncStatus = async () => {
  try {
    const status = await syncApi.getStatus()
    syncStatus.value = status
    isSyncing.value = status.isSyncing
  } catch (error) {
    console.error('Failed to load sync status:', error)
  }
}

const handleSyncAll = async () => {
  if (isSyncing.value) return
  
  try {
    isSyncing.value = true
    const result = await syncApi.syncAll()
    
    if (result.success) {
      lastSyncResult.value = result
      ElMessage.success('同步完成')
      emit('sync-complete')
    } else {
      ElMessage.error(result.errorMessage || '同步失败')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '同步失败')
  } finally {
    isSyncing.value = false
    await loadSyncStatus()
  }
}

onMounted(() => {
  loadSyncStatus()
})
</script>

<style scoped lang="scss">
.sync-panel {
  padding: var(--spacing-lg);
  background: var(--bg-color);
  border-radius: var(--radius-lg);
}

.sync-header {
  margin-bottom: var(--spacing-lg);
  text-align: center;

  h3 {
    margin: 0 0 var(--spacing-sm);
    color: var(--text-primary);
  }

  .sync-description {
    color: var(--text-secondary);
    font-size: 14px;
  }
}

.sync-status {
  margin-bottom: var(--spacing-lg);
}

.status-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md);
  background: var(--bg-secondary);
  border-radius: var(--radius-md);

  .syncing {
    color: var(--primary-color);
    animation: spin 1s linear infinite;
  }
}

.status-text {
  font-weight: 500;
}

.progress-info {
  margin-top: var(--spacing-md);
}

.current-item {
  margin-top: var(--spacing-sm);
  font-size: 12px;
  color: var(--text-secondary);
  text-align: center;
}

.sync-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-md);
  margin-top: var(--spacing-lg);
}

.stat-item {
  text-align: center;
  padding: var(--spacing-md);
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
}

.stat-value {
  display: block;
  font-size: 24px;
  font-weight: 600;
  color: var(--primary-color);
}

.stat-label {
  font-size: 12px;
  color: var(--text-secondary);
}

.sync-actions {
  display: flex;
  justify-content: center;
  margin-top: var(--spacing-lg);
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>
