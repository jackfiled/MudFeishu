<template>
  <div class="file-manager">
    <div class="main-content">
      <aside class="sidebar" :class="{ collapsed: appStore.sidebarCollapsed }">
        <div class="sidebar-header">
          <h3>文件管理器</h3>
          <el-button text @click="appStore.toggleSidebar">
            <el-icon><Fold v-if="!appStore.sidebarCollapsed" /></el-icon>
          </el-button>
        </div>
        <FolderTree @select="handleFolderSelect" />
      </aside>

      <main class="content-area">
        <div class="toolbar">
          <div class="toolbar-left">
            <el-button v-if="appStore.sidebarCollapsed" text @click="appStore.toggleSidebar">
              <el-icon><Expand /></el-icon>
            </el-button>
            <el-button type="primary" @click="handleUpload">
              <el-icon><Upload /></el-icon>
              上传文件
            </el-button>
            <el-button @click="handleCreateFolder">
              <el-icon><FolderAdd /></el-icon>
              新建文件夹
            </el-button>
          </div>
          <div class="toolbar-right">
            <el-input
              v-model="searchKeyword"
              placeholder="搜索文件..."
              clearable
              style="width: 200px"
              @input="handleSearch"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
            <el-radio-group v-model="fileStore.viewMode" size="small">
              <el-radio-button value="list">
                <el-icon><List /></el-icon>
              </el-radio-button>
              <el-radio-button value="grid">
                <el-icon><Grid /></el-icon>
              </el-radio-button>
            </el-radio-group>
            <el-dropdown @command="handleUserCommand">
              <el-button text>
                <el-icon><User /></el-icon>
                {{ authStore.displayName }}
                <el-icon class="el-icon--right"><ArrowDown /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="profile">
                    <el-icon><UserFilled /></el-icon>个人信息
                  </el-dropdown-item>
                  <el-dropdown-item command="password">
                    <el-icon><Lock /></el-icon>修改密码
                  </el-dropdown-item>
                  <el-dropdown-item divided command="logout">
                    <el-icon><SwitchButton /></el-icon>退出登录
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>

        <div class="breadcrumb">
          <el-breadcrumb separator="/">
            <el-breadcrumb-item :to="{ path: '/' }">
              <el-icon><HomeFilled /></el-icon>
            </el-breadcrumb-item>
            <el-breadcrumb-item v-for="(item, index) in breadcrumbList" :key="index">
              {{ item.folderName }}
            </el-breadcrumb-item>
          </el-breadcrumb>
        </div>

        <div class="file-list-container" v-loading="fileStore.loading">
          <FileList
            :files="fileStore.files"
            :view-mode="fileStore.viewMode"
            :selected-files="fileStore.selectedFiles"
            @select="handleFileSelect"
            @preview="handleFilePreview"
            @download="handleFileDownload"
            @rename="handleFileRename"
            @move="handleFileMove"
            @delete="handleFileDelete"
            @versions="handleVersionHistory"
          />

          <el-empty v-if="!fileStore.loading && fileStore.files.length === 0" description="暂无文件" />

          <div class="pagination" v-if="fileStore.totalCount > 0">
            <el-pagination
              v-model:current-page="fileStore.page"
              v-model:page-size="fileStore.pageSize"
              :total="fileStore.totalCount"
              :page-sizes="[10, 20, 50, 100]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="handleSizeChange"
              @current-change="handlePageChange"
            />
          </div>
        </div>
      </main>
    </div>

    <FileUpload
      v-if="appStore.showUploadPanel"
      :folder-token="currentFolderToken"
      @close="appStore.setUploadPanel(false)"
      @success="handleUploadSuccess"
    />

    <CreateFolderDialog
      v-if="showCreateFolderDialog"
      :parent-token="currentFolderToken"
      @close="showCreateFolderDialog = false"
      @success="handleCreateFolderSuccess"
    />

    <RenameDialog
      v-if="renameDialog.visible"
      :current-name="renameDialog.currentName"
      :type="renameDialog.type"
      @close="renameDialog.visible = false"
      @confirm="handleRenameConfirm"
    />

    <MoveDialog
      v-if="moveDialog.visible"
      :item-token="moveDialog.itemToken"
      :item-type="moveDialog.itemType"
      @close="moveDialog.visible = false"
      @success="handleMoveSuccess"
    />

    <VersionHistory
      v-if="versionHistoryVisible"
      :file-token="currentFileToken"
      :file-name="currentFileName"
      @close="versionHistoryVisible = false"
    />

    <UserProfileDialog
      v-if="userProfileVisible"
      @close="userProfileVisible = false"
    />

    <ChangePasswordDialog
      v-if="changePasswordVisible"
      @close="changePasswordVisible = false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Upload, FolderAdd, Search, List, Grid, HomeFilled, Fold, Expand, User, ArrowDown, UserFilled, Lock, SwitchButton } from '@element-plus/icons-vue'
import { useFileStore } from '@/stores/fileStore'
import { useFolderStore } from '@/stores/folderStore'
import { useAppStore } from '@/stores/appStore'
import { useAuthStore } from '@/stores/authStore'
import { folderApi, fileApi } from '@/api'
import FolderTree from '@/components/FolderTree.vue'
import FileList from '@/components/FileList.vue'
import FileUpload from '@/components/FileUpload.vue'
import CreateFolderDialog from '@/components/CreateFolderDialog.vue'
import RenameDialog from '@/components/RenameDialog.vue'
import MoveDialog from '@/components/MoveDialog.vue'
import VersionHistory from '@/components/VersionHistory.vue'
import UserProfileDialog from '@/components/UserProfileDialog.vue'
import ChangePasswordDialog from '@/components/ChangePasswordDialog.vue'

const route = useRoute()
const router = useRouter()
const fileStore = useFileStore()
const folderStore = useFolderStore()
const appStore = useAppStore()
const authStore = useAuthStore()

const searchKeyword = ref('')
const showCreateFolderDialog = ref(false)
const currentFolderToken = ref<string | undefined>(undefined)
const currentFileToken = ref('')
const currentFileName = ref('')
const versionHistoryVisible = ref(false)
const userProfileVisible = ref(false)
const changePasswordVisible = ref(false)

const breadcrumbList = computed(() => folderStore.currentFolderPath)

const renameDialog = ref<{
  visible: boolean
  currentName: string
  type: 'file' | 'folder'
  token: string
}>({
  visible: false,
  currentName: '',
  type: 'file',
  token: ''
})

const moveDialog = ref<{
  visible: boolean
  itemToken: string
  itemType: 'file' | 'folder'
}>({
  visible: false,
  itemToken: '',
  itemType: 'file'
})

const loadFolders = async () => {
  try {
    folderStore.setLoading(true)
    const response = await folderApi.getList()
    folderStore.setFolders(response.folders)
  } catch (error) {
    ElMessage.error('加载文件夹失败')
  } finally {
    folderStore.setLoading(false)
  }
}

const loadFiles = async () => {
  try {
    fileStore.setLoading(true)
    const response = await fileApi.getList(currentFolderToken.value, fileStore.page, fileStore.pageSize)
    fileStore.setFiles(response.files, response.totalCount)
  } catch (error) {
    ElMessage.error('加载文件列表失败')
  } finally {
    fileStore.setLoading(false)
  }
}

const handleFolderSelect = (folder: any) => {
  currentFolderToken.value = folder.folderToken
  folderStore.setCurrentFolder(folder)
  fileStore.page = 1
  loadFiles()
}

const handleFileSelect = (fileToken: string) => {
  fileStore.toggleSelect(fileToken)
}

const handleFilePreview = (file: any) => {
  currentFileToken.value = file.fileToken
}

const handleFileDownload = async (file: any) => {
  try {
    const response = await fileApi.download(file.fileToken)
    const blob = new Blob([response.data as any])
    const link = document.createElement('a')
    link.href = window.URL.createObjectURL(blob)
    link.download = file.fileName
    link.click()
    ElMessage.success('下载成功')
  } catch (error) {
    ElMessage.error('下载失败')
  }
}

const handleFileRename = (file: any) => {
  renameDialog.value = {
    visible: true,
    currentName: file.fileName,
    type: 'file',
    token: file.fileToken
  }
}

const handleFileMove = (file: any) => {
  moveDialog.value = {
    visible: true,
    itemToken: file.fileToken,
    itemType: 'file'
  }
}

const handleFileDelete = async (file: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除 "${file.fileName}" 吗？`, '提示', {
      type: 'warning'
    })
    await fileApi.delete(file.fileToken)
    fileStore.removeFile(file.fileToken)
    ElMessage.success('删除成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleVersionHistory = (file: any) => {
  currentFileToken.value = file.fileToken
  currentFileName.value = file.fileName
  versionHistoryVisible.value = true
}

const handleUpload = () => {
  appStore.setUploadPanel(true)
}

const handleCreateFolder = () => {
  showCreateFolderDialog.value = true
}

const handleUploadSuccess = () => {
  loadFiles()
}

const handleCreateFolderSuccess = () => {
  showCreateFolderDialog.value = false
  loadFolders()
}

const handleRenameConfirm = async (newName: string) => {
  try {
    if (renameDialog.value.type === 'file') {
      await fileApi.getInfo(renameDialog.value.token)
    } else {
      await folderApi.update(renameDialog.value.token, { name: newName })
    }
    renameDialog.value.visible = false
    ElMessage.success('重命名成功')
    loadFiles()
    loadFolders()
  } catch (error) {
    ElMessage.error('重命名失败')
  }
}

const handleMoveSuccess = () => {
  moveDialog.value.visible = false
  ElMessage.success('移动成功')
  loadFiles()
  loadFolders()
}

const handleSearch = () => {
  fileStore.page = 1
  loadFiles()
}

const handleSizeChange = () => {
  loadFiles()
}

const handlePageChange = () => {
  loadFiles()
}

const handleUserCommand = async (command: string) => {
  switch (command) {
    case 'profile':
      userProfileVisible.value = true
      break
    case 'password':
      changePasswordVisible.value = true
      break
    case 'logout':
      try {
        await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
          type: 'warning'
        })
        authStore.logout()
        router.push('/login')
        ElMessage.success('已退出登录')
      } catch (error: any) {
        if (error !== 'cancel') {
          console.error('退出登录失败:', error)
        }
      }
      break
  }
}

watch(() => route.params.folderToken, (newToken) => {
  currentFolderToken.value = newToken as string | undefined
  loadFiles()
})

onMounted(() => {
  loadFolders()
  loadFiles()
})
</script>

<style scoped lang="scss">
.file-manager {
  width: 100%;
  height: 100vh;
  display: flex;
  flex-direction: column;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px;
  border-bottom: 1px solid var(--el-border-color-light);

  h3 {
    margin: 0;
    font-size: 16px;
    font-weight: 600;
  }
}

.pagination {
  display: flex;
  justify-content: center;
  padding: 16px;
  border-top: 1px solid var(--el-border-color-light);
}
</style>
