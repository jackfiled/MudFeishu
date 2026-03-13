<template>
  <div class="file-list">
    <el-table
      v-if="viewMode === 'list'"
      :data="files"
      @selection-change="handleSelectionChange"
      @row-click="handleRowClick"
      @row-dblclick="handleRowDblClick"
      style="width: 100%"
      row-key="fileToken"
      :row-class-name="getRowClassName"
    >
      <el-table-column type="selection" width="40" />
      <el-table-column label="文件名" min-width="300">
        <template #default="{ row }">
          <div class="file-name-cell">
            <el-icon :class="getFileIconClass(row.fileName)">
              <component :is="getFileIcon(row.fileName)" />
            </el-icon>
            <span class="file-name">{{ row.fileName }}</span>
          </div>
        </template>
      </el-table-column>
      <el-table-column prop="fileSize" label="大小" width="120">
        <template #default="{ row }">
          {{ formatFileSize(row.fileSize) }}
        </template>
      </el-table-column>
      <el-table-column prop="uploadTime" label="修改时间" width="180">
        <template #default="{ row }">
          {{ formatDate(row.uploadTime) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="{ row }">
          <el-button-group>
            <el-button size="small" @click.stop="handleDownload(row)">
              <el-icon><Download /></el-icon>
            </el-button>
            <el-button size="small" @click.stop="handleVersions(row)">
              <el-icon><Clock /></el-icon>
            </el-button>
            <el-dropdown @command="(cmd: string) => handleCommand(cmd, row)">
              <el-button size="small">
                <el-icon><MoreFilled /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="preview">预览</el-dropdown-item>
                  <el-dropdown-item command="rename">重命名</el-dropdown-item>
                  <el-dropdown-item command="move">移动到</el-dropdown-item>
                  <el-dropdown-item command="delete" divided>删除</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </el-button-group>
        </template>
      </el-table-column>
    </el-table>

    <div v-else class="grid-view">
      <div
        v-for="file in files"
        :key="file.fileToken"
        class="grid-item"
        :class="{ selected: selectedFiles.includes(file.fileToken) }"
        @click="handleGridClick(file)"
        @dblclick="handleGridDblClick(file)"
      >
        <el-checkbox
          :model-value="selectedFiles.includes(file.fileToken)"
          @click.stop
          @change="handleGridSelect(file.fileToken)"
        />
        <el-icon :class="getFileIconClass(file.fileName)" class="grid-icon">
          <component :is="getFileIcon(file.fileName)" />
        </el-icon>
        <div class="grid-name">{{ file.fileName }}</div>
        <div class="grid-size">{{ formatFileSize(file.fileSize) }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { FileInfoResponse } from '@/api/types'
import { formatFileSize, formatDate } from '@/utils/format'

const props = defineProps<{
  files: FileInfoResponse[]
  viewMode: 'list' | 'grid'
  selectedFiles: string[]
}>()

const emit = defineEmits<{
  select: [fileToken: string]
  preview: [file: FileInfoResponse]
  download: [file: FileInfoResponse]
  rename: [file: FileInfoResponse]
  move: [file: FileInfoResponse]
  delete: [file: FileInfoResponse]
  versions: [file: FileInfoResponse]
}>()

const getFileIcon = (filename: string): string => {
  const ext = filename.split('.').pop()?.toLowerCase() || ''
  switch (ext) {
    case 'docx':
    case 'doc':
      return 'Document'
    case 'xlsx':
    case 'xls':
      return 'Grid'
    case 'pptx':
    case 'ppt':
      return 'PictureFilled'
    case 'pdf':
      return 'Document'
    case 'png':
    case 'jpg':
    case 'jpeg':
    case 'gif':
      return 'Picture'
    default:
      return 'Document'
  }
}

const getFileIconClass = (filename: string): string => {
  const ext = filename.split('.').pop()?.toLowerCase() || ''
  return `file-icon ${ext}`
}

const handleSelectionChange = (selection: FileInfoResponse[]) => {
  selection.forEach(file => {
    if (!props.selectedFiles.includes(file.fileToken)) {
      emit('select', file.fileToken)
    }
  })
}

const handleRowClick = (row: FileInfoResponse) => {
  emit('select', row.fileToken)
}

const handleRowDblClick = (row: FileInfoResponse) => {
  emit('preview', row)
}

const handleGridClick = (file: FileInfoResponse) => {
  emit('select', file.fileToken)
}

const handleGridDblClick = (file: FileInfoResponse) => {
  emit('preview', file)
}

const handleGridSelect = (fileToken: string) => {
  emit('select', fileToken)
}

const handleDownload = (file: FileInfoResponse) => {
  emit('download', file)
}

const handleVersions = (file: FileInfoResponse) => {
  emit('versions', file)
}

const handleCommand = (command: string, file: FileInfoResponse) => {
  switch (command) {
    case 'preview':
      emit('preview', file)
      break
    case 'rename':
      emit('rename', file)
      break
    case 'move':
      emit('move', file)
      break
    case 'delete':
      emit('delete', file)
      break
  }
}

const getRowClassName = ({ row }: { row: FileInfoResponse }) => {
  return props.selectedFiles.includes(row.fileToken) ? 'selected-row' : ''
}
</script>

<style scoped lang="scss">
.file-list {
  width: 100%;
}

.file-name-cell {
  display: flex;
  align-items: center;
  gap: 8px;

  .file-name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.file-icon {
  font-size: 24px;

  &.docx, &.doc { color: #2196f3; }
  &.xlsx, &.xls { color: #4caf50; }
  &.pptx, &.ppt { color: #ff9800; }
  &.pdf { color: #f44336; }
  &.png, &.jpg, &.jpeg, &.gif { color: #9c27b0; }
}

.grid-view {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 16px;
  padding: 16px;

  .grid-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 16px;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.2s;
    position: relative;

    &:hover {
      background-color: var(--el-fill-color-light);
    }

    &.selected {
      background-color: var(--el-color-primary-light-9);
      outline: 2px solid var(--el-color-primary);
    }

    .el-checkbox {
      position: absolute;
      top: 8px;
      left: 8px;
    }

    .grid-icon {
      font-size: 48px;
      margin: 8px 0;
    }

    .grid-name {
      font-size: 14px;
      text-align: center;
      word-break: break-all;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
      max-width: 100%;
    }

    .grid-size {
      font-size: 12px;
      color: var(--el-text-color-secondary);
      margin-top: 4px;
    }
  }
}

.selected-row {
  background-color: var(--el-color-primary-light-9) !important;
}
</style>
