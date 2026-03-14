<template>
  <el-dialog
    v-model="visible"
    title="移动到"
    width="500px"
    @close="handleClose"
  >
    <div class="folder-tree-container">
      <el-tree
        ref="treeRef"
        :data="folders"
        :props="treeProps"
        node-key="folderToken"
        :expand-on-click-node="false"
        :default-expand-all="true"
        @node-click="handleNodeClick"
      >
        <template #default="{ node }">
          <div class="tree-node">
            <el-icon class="folder-icon"><Folder /></el-icon>
            <span>{{ node.label }}</span>
          </div>
        </template>
      </el-tree>
    </div>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElTree } from 'element-plus'
import { folderApi } from '@/api'
import type { FolderResponse } from '@/api/types'

const props = defineProps<{
  itemToken: string
  itemType: 'file' | 'folder' | 'batch'
}>()

const emit = defineEmits<{
  close: []
  success: []
}>()

const visible = ref(true)
const loading = ref(false)
const folders = ref<FolderResponse[]>([])
const selectedFolder = ref<string | null>(null)

const treeProps = {
  children: 'children',
  label: 'folderName'
}

const loadFolders = async () => {
  try {
    const response = await folderApi.getList()
    folders.value = buildTree(response.folders)
  } catch (error) {
    ElMessage.error('加载文件夹失败')
  }
}

const buildTree = (folderList: FolderResponse[]): any[] => {
  const map = new Map<string, any>()
  const roots: any[] = []

  folderList.forEach(folder => {
    map.set(folder.folderToken, { ...folder, children: [] })
  })

  folderList.forEach(folder => {
    const node = map.get(folder.folderToken)
    if (folder.parentFolderToken) {
      const parent = map.get(folder.parentFolderToken)
      if (parent) {
        parent.children.push(node)
      } else {
        roots.push(node)
      }
    } else {
      roots.push(node)
    }
  })

  return roots
}

const handleNodeClick = (data: FolderResponse) => {
  selectedFolder.value = data.folderToken
}

const handleSubmit = async () => {
  loading.value = true
  try {
    await folderApi.update(props.itemToken, {
      parentFolderToken: selectedFolder.value || undefined
    })
    ElMessage.success('移动成功')
    emit('success')
    handleClose()
  } catch (error) {
    ElMessage.error('移动失败')
  } finally {
    loading.value = false
  }
}

const handleClose = () => {
  emit('close')
}

onMounted(() => {
  loadFolders()
})
</script>

<style scoped lang="scss">
.folder-tree-container {
  height: 300px;
  overflow-y: auto;
  border: 1px solid var(--el-border-color-light);
  border-radius: 4px;
  padding: 8px;
}

.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;

  .folder-icon {
    color: #ffc107;
  }
}
</style>
