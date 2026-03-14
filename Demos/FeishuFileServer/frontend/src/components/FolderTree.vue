<template>
  <div class="folder-tree" v-loading="folderStore.loading">
    <div 
      class="root-folder-node"
      :class="{ 'is-active': currentFolderToken === null }"
      @click="handleRootClick"
    >
      <el-icon class="folder-icon"><HomeFilled /></el-icon>
      <span class="folder-name">根目录</span>
      <div class="folder-actions" @click.stop>
        <el-button 
          type="primary" 
          size="small" 
          text 
          @click="handleCreateFolder(null)"
        >
          <el-icon><Plus /></el-icon>
        </el-button>
      </div>
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
        <div class="folder-actions" @click.stop>
          <el-button 
            type="primary" 
            size="small" 
            text 
            @click="handleCreateFolder(folder)"
            title="新建子文件夹"
          >
            <el-icon><Plus /></el-icon>
          </el-button>
          <el-button 
            type="warning" 
            size="small" 
            text 
            @click="handleRenameFolder(folder)"
            title="重命名"
          >
            <el-icon><Edit /></el-icon>
          </el-button>
          <el-button 
            type="danger" 
            size="small" 
            text 
            @click="handleDeleteFolder(folder)"
            title="删除"
          >
            <el-icon><Delete /></el-icon>
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
      <div class="context-menu-item" @click="handleCreateFolder(contextMenu.folder)">
        <el-icon><FolderAdd /></el-icon>
        <span>新建子文件夹</span>
      </div>
      <div class="context-menu-item" @click="handleRenameFolder(contextMenu.folder)">
        <el-icon><Edit /></el-icon>
        <span>重命名</span>
      </div>
      <div class="context-menu-item danger" @click="handleDeleteFolder(contextMenu.folder)">
        <el-icon><Delete /></el-icon>
        <span>删除</span>
      </div>
    </div>

    <el-dialog
      v-model="createFolderDialogVisible"
      :title="dialogTitle"
      width="400px"
      append-to-body
      @close="resetCreateFolderForm"
    >
      <el-form :model="createFolderForm" :rules="createFolderRules" ref="createFolderFormRef">
        <el-form-item label="文件夹名称" prop="name">
          <el-input 
            v-model="createFolderForm.name" 
            placeholder="请输入文件夹名称"
            @keyup.enter="submitCreateFolder"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createFolderDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitCreateFolder" :loading="createFolderLoading">
          创建
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="renameDialogVisible"
      title="重命名文件夹"
      width="400px"
      append-to-body
      @close="resetRenameForm"
    >
      <el-form :model="renameForm" :rules="renameRules" ref="renameFormRef">
        <el-form-item label="文件夹名称" prop="name">
          <el-input 
            v-model="renameForm.name" 
            placeholder="请输入新的文件夹名称"
            @keyup.enter="submitRename"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="renameDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitRename" :loading="renameLoading">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted, computed } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { HomeFilled, Folder, FolderAdd, Edit, Delete, Plus } from '@element-plus/icons-vue'
import { useFolderStore } from '@/stores/folderStore'
import { folderApi } from '@/api'
import type { FolderResponse } from '@/api/types'

const props = defineProps<{
  currentFolderToken?: string | null
}>()

const emit = defineEmits<{
  select: [folder: FolderResponse | null]
  folderCreated: []
  folderUpdated: []
}>()

const folderStore = useFolderStore()

const dialogTitle = computed(() => {
  return createFolderParent.value 
    ? `在"${createFolderParent.value.folderName}"中新建文件夹` 
    : '新建文件夹'
})

const contextMenu = ref({
  visible: false,
  x: 0,
  y: 0,
  folder: null as FolderResponse | null
})

const createFolderDialogVisible = ref(false)
const createFolderLoading = ref(false)
const createFolderParent = ref<FolderResponse | null>(null)
const createFolderFormRef = ref<FormInstance>()
const createFolderForm = reactive({
  name: ''
})

const createFolderRules: FormRules = {
  name: [
    { required: true, message: '请输入文件夹名称', trigger: 'blur' },
    { min: 1, max: 100, message: '文件夹名称长度为1-100个字符', trigger: 'blur' }
  ]
}

const renameDialogVisible = ref(false)
const renameLoading = ref(false)
const renameFolder = ref<FolderResponse | null>(null)
const renameFormRef = ref<FormInstance>()
const renameForm = reactive({
  name: ''
})

const renameRules: FormRules = {
  name: [
    { required: true, message: '请输入文件夹名称', trigger: 'blur' },
    { min: 1, max: 100, message: '文件夹名称长度为1-100个字符', trigger: 'blur' }
  ]
}

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

const handleCreateFolder = (parentFolder: FolderResponse | null) => {
  hideContextMenu()
  createFolderParent.value = parentFolder
  createFolderDialogVisible.value = true
}

const resetCreateFolderForm = () => {
  createFolderForm.name = ''
  createFolderParent.value = null
  createFolderFormRef.value?.resetFields()
}

const submitCreateFolder = async () => {
  if (!createFolderFormRef.value) return
  
  try {
    await createFolderFormRef.value.validate()
    createFolderLoading.value = true
    
    await folderApi.create({
      name: createFolderForm.name,
      parentFolderToken: createFolderParent.value?.folderToken
    })
    
    ElMessage.success('文件夹创建成功')
    createFolderDialogVisible.value = false
    resetCreateFolderForm()
    emit('folderCreated')
  } catch (error: any) {
    if (error.response?.data?.message) {
      ElMessage.error(error.response.data.message)
    } else if (error !== false) {
      ElMessage.error('创建文件夹失败')
    }
  } finally {
    createFolderLoading.value = false
  }
}

const handleRenameFolder = (folder: FolderResponse | null) => {
  hideContextMenu()
  if (!folder) return
  renameFolder.value = folder
  renameForm.name = folder.folderName
  renameDialogVisible.value = true
}

const resetRenameForm = () => {
  renameForm.name = ''
  renameFolder.value = null
  renameFormRef.value?.resetFields()
}

const submitRename = async () => {
  if (!renameFormRef.value || !renameFolder.value) return
  
  try {
    await renameFormRef.value.validate()
    renameLoading.value = true
    
    await folderApi.update(renameFolder.value.folderToken, {
      name: renameForm.name
    })
    
    ElMessage.success('文件夹重命名成功')
    renameDialogVisible.value = false
    resetRenameForm()
    emit('folderUpdated')
  } catch (error: any) {
    if (error.response?.data?.message) {
      ElMessage.error(error.response.data.message)
    } else if (error !== false) {
      ElMessage.error('重命名失败')
    }
  } finally {
    renameLoading.value = false
  }
}

const handleDeleteFolder = async (folder: FolderResponse | null) => {
  hideContextMenu()
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

    .folder-actions {
      opacity: 1;
    }
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

  .folder-actions {
    opacity: 0;
    transition: opacity var(--transition-fast);
    display: flex;
    gap: var(--spacing-xs);
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
    gap: 2px;
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
