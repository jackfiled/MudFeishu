<template>
  <div class="folder-tree" v-loading="folderStore.loading">
    <div 
      class="root-folder-node"
      :class="{ 'is-active': currentFolderToken === null }"
      @click="handleRootClick"
    >
      <el-icon class="folder-icon"><HomeFilled /></el-icon>
      <span class="folder-name">根目录</span>
    </div>

    <div class="folder-list">
      <div 
        v-for="folder in folderStore.folders" 
        :key="folder.folderToken"
        class="folder-item"
        :class="{ 'is-active': currentFolderToken === folder.folderToken }"
        @click="handleNodeClick(folder)"
        @contextmenu.prevent="showContextMenu($event, folder)"
      >
        <el-icon class="folder-icon"><Folder /></el-icon>
        <span class="folder-name">{{ folder.folderName }}</span>
        <div class="folder-actions">
          <el-button 
            type="primary" 
            size="small" 
            text 
            @click.stop="handleCreateSubFolder(folder)"
          >
            <el-icon><Plus /></el-icon>
          </el-button>
        </div>
      </div>
    </div>

    <el-empty v-if="!folderStore.loading && folderStore.folders.length === 0" description="暂无文件夹" />

    <div
      v-show="contextMenu.visible"
      class="context-menu glass-effect"
      :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }"
    >
      <div class="context-menu-item" @click="handleCreateSubFolder(contextMenu.folder)">
        <el-icon><FolderAdd /></el-icon>
        <span>新建子文件夹</span>
      </div>
      <div class="context-menu-item" @click="handleRename">
        <el-icon><Edit /></el-icon>
        <span>重命名</span>
      </div>
      <div class="context-menu-item danger" @click="handleDelete">
        <el-icon><Delete /></el-icon>
        <span>删除</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { HomeFilled, Folder, FolderAdd, Edit, Delete, Plus } from '@element-plus/icons-vue'
import { useFolderStore } from '@/stores/folderStore'
import { folderApi } from '@/api'
import type { FolderResponse } from '@/api/types'

const props = defineProps<{
  currentFolderToken?: string | null
}>()

const emit = defineEmits<{
  select: [folder: FolderResponse | null]
}>()

const folderStore = useFolderStore()

const contextMenu = ref({
  visible: false,
  x: 0,
  y: 0,
  folder: null as FolderResponse | null
})

const handleRootClick = () => {
  emit('select', null)
}

const handleNodeClick = (data: FolderResponse) => {
  emit('select', data)
}

const showContextMenu = (event: MouseEvent, folder: FolderResponse) => {
  contextMenu.value = {
    visible: true,
    x: event.clientX,
    y: event.clientY,
    folder
  }
}

const hideContextMenu = () => {
  contextMenu.value.visible = false
}

const handleCreateSubFolder = (_folder: FolderResponse | null) => {
  hideContextMenu()
}

const handleRename = async () => {
  hideContextMenu()
}

const handleDelete = async () => {
  hideContextMenu()
  const folder = contextMenu.value.folder
  if (!folder) return

  try {
    await ElMessageBox.confirm(`确定要删除文件夹 "${folder.folderName}" 吗？`, '提示', {
      type: 'warning'
    })
    await folderApi.delete(folder.folderToken)
    folderStore.removeFolder(folder.folderToken)
    ElMessage.success('删除成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

onMounted(async () => {
  document.addEventListener('click', hideContextMenu)
})

onUnmounted(() => {
  document.removeEventListener('click', hideContextMenu)
})
</script>

<style scoped lang="scss">
.folder-tree {
  height: 100%;
  overflow-y: auto;
  padding: var(--spacing-sm);
}

.root-folder-node {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-md);
  cursor: pointer;
  border-radius: var(--radius-md);
  margin-bottom: var(--spacing-xs);
  transition: all var(--transition-fast);
  background: var(--bg-secondary);

  &:hover {
    background: var(--bg-tertiary);
    transform: translateX(2px);
  }

  &.is-active {
    background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
    color: white;
    box-shadow: var(--shadow-sm), 0 2px 8px rgba(99, 102, 241, 0.3);

    .folder-icon {
      color: white;
    }
  }

  .folder-icon {
    color: var(--primary-color);
    font-size: 18px;
    transition: all var(--transition-fast);
  }

  .folder-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-weight: 500;
    font-size: 14px;
  }
}

.folder-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.folder-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-md);
  cursor: pointer;
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
  position: relative;

  &:hover {
    background: var(--bg-secondary);
    transform: translateX(2px);

    .folder-actions {
      opacity: 1;
    }
  }

  &.is-active {
    background: var(--primary-light);
    color: var(--primary-color);

    .folder-icon {
      color: var(--primary-color);
    }
  }

  .folder-icon {
    color: #ffc107;
    font-size: 18px;
    transition: all var(--transition-fast);
  }

  .folder-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 14px;
  }

  .folder-actions {
    opacity: 0;
    transition: opacity var(--transition-fast);
    display: flex;
    gap: var(--spacing-xs);
  }
}

.context-menu {
  position: fixed;
  background: var(--bg-color);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
  z-index: 9999;
  min-width: 160px;
  padding: var(--spacing-xs);
  backdrop-filter: blur(12px);

  .context-menu-item {
    display: flex;
    align-items: center;
    gap: var(--spacing-sm);
    padding: var(--spacing-sm) var(--spacing-md);
    cursor: pointer;
    border-radius: var(--radius-md);
    transition: all var(--transition-fast);
    font-size: 14px;

    &:hover {
      background: var(--bg-secondary);
    }

    &.danger {
      color: var(--danger-color);

      &:hover {
        background: rgba(239, 68, 68, 0.1);
      }
    }

    .el-icon {
      font-size: 16px;
    }
  }
}

:deep(.el-empty) {
  padding: var(--spacing-xl);

  .el-empty__description {
    color: var(--text-secondary);
    font-size: 13px;
  }
}
</style>
