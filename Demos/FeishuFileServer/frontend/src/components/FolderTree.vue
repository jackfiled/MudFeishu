<template>
  <div class="folder-tree" v-loading="folderStore.loading">
    <el-tree
      :data="folderStore.folders"
      :props="treeProps"
      node-key="folderToken"
      :expand-on-click-node="false"
      :default-expanded-keys="Array.from(folderStore.expandedFolders)"
      @node-click="handleNodeClick"
      @node-expand="handleExpand"
      @node-collapse="handleCollapse"
    >
      <template #default="{ node, data }">
        <div class="tree-node" @contextmenu.prevent="showContextMenu($event, data)">
          <el-icon class="folder-icon"><Folder /></el-icon>
          <span class="folder-name">{{ node.label }}</span>
        </div>
      </template>
    </el-tree>

    <el-empty v-if="!folderStore.loading && folderStore.folders.length === 0" description="暂无文件夹" />

    <div
      v-show="contextMenu.visible"
      class="context-menu"
      :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }"
    >
      <div class="context-menu-item" @click="handleCreateSubFolder">
        <el-icon><FolderAdd /></el-icon>
        <span>新建子文件夹</span>
      </div>
      <div class="context-menu-item" @click="handleRename">
        <el-icon><Edit /></el-icon>
        <span>重命名</span>
      </div>
      <div class="context-menu-item" @click="handleDelete">
        <el-icon><Delete /></el-icon>
        <span>删除</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useFolderStore } from '@/stores/folderStore'
import { folderApi } from '@/api'
import type { FolderResponse } from '@/api/types'

const emit = defineEmits<{
  select: [folder: FolderResponse]
}>()

const folderStore = useFolderStore()

const treeProps = {
  children: 'children',
  label: 'folderName'
}

const contextMenu = ref({
  visible: false,
  x: 0,
  y: 0,
  folder: null as FolderResponse | null
})

const handleNodeClick = (data: FolderResponse) => {
  emit('select', data)
}

const handleExpand = (data: FolderResponse) => {
  folderStore.toggleExpand(data.folderToken)
}

const handleCollapse = (data: FolderResponse) => {
  folderStore.toggleExpand(data.folderToken)
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

const handleCreateSubFolder = async () => {
  hideContextMenu()
  const parentToken = contextMenu.value.folder?.folderToken
  // 可以在这里打开创建子文件夹对话框
}

const handleRename = async () => {
  hideContextMenu()
  // 可以在这里打开重命名对话框
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
  padding: 8px;
}

.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 0;
  width: 100%;

  .folder-icon {
    color: #ffc107;
  }

  .folder-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.context-menu {
  position: fixed;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-light);
  border-radius: 4px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  z-index: 9999;
  min-width: 150px;

  .context-menu-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 16px;
    cursor: pointer;
    transition: background-color 0.2s;

    &:hover {
      background-color: var(--el-fill-color-light);
    }
  }
}
</style>
