<template>
  <div class="file-list">
    <template v-if="viewMode === 'list'">
      <div class="list-header">
        <div class="col-checkbox"></div>
        <div class="col-name">文件名</div>
        <div class="col-size">大小</div>
        <div class="col-date">修改时间</div>
        <div class="col-actions">操作</div>
      </div>
      <transition-group name="list" tag="div" class="list-body">
        <div
          v-for="file in files"
          :key="file.fileToken"
          class="list-item"
          :class="{ selected: selectedFiles.includes(file.fileToken) }"
          @click="handleRowClick(file)"
          @dblclick="handleRowDblClick(file)"
        >
          <div class="col-checkbox" @click.stop>
            <el-checkbox
              :model-value="selectedFiles.includes(file.fileToken)"
              @change="handleGridSelect(file.fileToken)"
            />
          </div>
          <div class="col-name">
            <div class="file-icon-wrapper">
              <el-icon :class="getFileIconClass(file.fileName)" :size="32">
                <component :is="getFileIcon(file.fileName)" />
              </el-icon>
            </div>
            <div class="file-info">
              <span class="file-name">{{ file.fileName }}</span>
              <span class="file-token">{{ file.fileToken.substring(0, 8) }}...</span>
            </div>
          </div>
          <div class="col-size">
            <span class="size-badge">{{ formatFileSize(file.fileSize) }}</span>
          </div>
          <div class="col-date">
            <span class="date-text">{{ formatDate(file.uploadTime) }}</span>
          </div>
          <div class="col-actions">
            <div class="action-buttons">
              <button class="action-btn icon-btn" @click.stop="handleDownload(file)" title="下载">
                <el-icon><Download /></el-icon>
              </button>
              <button class="action-btn icon-btn" @click.stop="handleVersions(file)" title="版本历史">
                <el-icon><Clock /></el-icon>
              </button>
              <el-dropdown @command="(cmd: string) => handleCommand(cmd, file)" trigger="click">
                <button class="action-btn icon-btn" @click.stop title="更多">
                  <el-icon><MoreFilled /></el-icon>
                </button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="preview">
                      <el-icon><View /></el-icon>
                      <span>预览</span>
                    </el-dropdown-item>
                    <el-dropdown-item command="rename">
                      <el-icon><Edit /></el-icon>
                      <span>重命名</span>
                    </el-dropdown-item>
                    <el-dropdown-item command="move">
                      <el-icon><FolderRemove /></el-icon>
                      <span>移动到</span>
                    </el-dropdown-item>
                    <el-dropdown-item command="delete" divided>
                      <el-icon><Delete /></el-icon>
                      <span>删除</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </div>
        </div>
      </transition-group>
    </template>

    <transition-group v-else name="grid" tag="div" class="grid-view">
      <div
        v-for="file in files"
        :key="file.fileToken"
        class="grid-item modern-card"
        :class="{ selected: selectedFiles.includes(file.fileToken) }"
        @click="handleGridClick(file)"
        @dblclick="handleGridDblClick(file)"
      >
        <div class="grid-item-header">
          <el-checkbox
            :model-value="selectedFiles.includes(file.fileToken)"
            @click.stop
            @change="handleGridSelect(file.fileToken)"
          />
          <el-dropdown @command="(cmd: string) => handleCommand(cmd, file)" trigger="click">
            <button class="more-btn" @click.stop>
              <el-icon><MoreFilled /></el-icon>
            </button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="download">
                  <el-icon><Download /></el-icon>
                  <span>下载</span>
                </el-dropdown-item>
                <el-dropdown-item command="rename">
                  <el-icon><Edit /></el-icon>
                  <span>重命名</span>
                </el-dropdown-item>
                <el-dropdown-item command="delete" divided>
                  <el-icon><Delete /></el-icon>
                  <span>删除</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
        
        <div class="grid-icon-wrapper">
          <el-icon :class="['grid-icon', getFileIconClass(file.fileName)]">
            <component :is="getFileIcon(file.fileName)" />
          </el-icon>
        </div>
        
        <div class="grid-info">
          <div class="grid-name" :title="file.fileName">{{ file.fileName }}</div>
          <div class="grid-meta">
            <span class="grid-size">{{ formatFileSize(file.fileSize) }}</span>
          </div>
        </div>
      </div>
    </transition-group>
  </div>
</template>

<script setup lang="ts">
import type { FileInfoResponse } from '@/api/types'
import { formatFileSize, formatDate } from '@/utils/format'

defineProps<{
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
    case 'tiff':
      return 'Picture'
    default:
      return 'Document'
  }
}

const getFileIconClass = (filename: string): string => {
  const ext = filename.split('.').pop()?.toLowerCase() || ''
  return `file-icon ${ext}`
}

const handleRowClick = (file: FileInfoResponse) => {
  emit('select', file.fileToken)
}

const handleRowDblClick = (file: FileInfoResponse) => {
  emit('preview', file)
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
    case 'download':
      emit('download', file)
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
</script>

<style scoped lang="scss">
.file-list {
  width: 100%;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.list-header {
  display: grid;
  grid-template-columns: 40px 1fr 100px 150px 140px;
  align-items: center;
  padding: var(--spacing-sm) var(--spacing-md);
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  font-size: 12px;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: var(--spacing-sm);
  flex-shrink: 0;
}

.list-body {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  flex: 1;
}

.list-item {
  display: grid;
  grid-template-columns: 40px 1fr 100px 150px 140px;
  align-items: center;
  padding: var(--spacing-sm) var(--spacing-md);
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all var(--transition-fast);
  border: 2px solid transparent;
  
  &:hover {
    background: var(--bg-tertiary);
    transform: translateX(4px);
    
    .action-buttons {
      opacity: 1;
    }
  }
  
  &.selected {
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.15) 0%, rgba(99, 102, 241, 0.08) 100%);
    border-color: var(--primary-color);
    box-shadow: 0 0 0 1px var(--primary-color), 0 2px 8px rgba(99, 102, 241, 0.2);
    
    .file-name {
      color: var(--primary-color);
      font-weight: 600;
    }
    
    .file-icon-wrapper {
      background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
      
      .file-icon {
        color: white;
      }
    }
    
    .size-badge {
      background: var(--primary-color);
      color: white;
    }
    
    .date-text {
      color: var(--primary-color);
    }
  }
}

.col-name {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  min-width: 0;
}

.file-icon-wrapper {
  width: 44px;
  height: 44px;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all var(--transition-fast);
  
  .list-item:hover & {
    background: var(--primary-bg);
  }
}

.file-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.file-name {
  font-weight: 500;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-token {
  font-size: 11px;
  color: var(--text-tertiary);
  font-family: var(--font-mono);
}

.size-badge {
  display: inline-flex;
  padding: 2px var(--spacing-sm);
  background: var(--bg-tertiary);
  border-radius: var(--radius-full);
  font-size: 12px;
  color: var(--text-secondary);
}

.date-text {
  font-size: 13px;
  color: var(--text-secondary);
}

.action-buttons {
  display: flex;
  gap: var(--spacing-xs);
  opacity: 0;
  transition: opacity var(--transition-fast);
}

.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xs);
  border: none;
  background: transparent;
  color: var(--text-secondary);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
  
  &:hover {
    background: var(--primary-bg);
    color: var(--primary-color);
  }
}

.icon-btn {
  width: 32px;
  height: 32px;
}

.file-icon {
  font-size: 24px;
  
  &.docx, &.doc { color: #3b82f6; }
  &.xlsx, &.xls { color: #22c55e; }
  &.pptx, &.ppt { color: #f97316; }
  &.pdf { color: #ef4444; }
  &.png, &.jpg, &.jpeg, &.gif { color: #a855f7; }
  &.tiff { color: #ec4899; }
}

.grid-view {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: var(--spacing-md);
}

.grid-item {
  display: flex;
  flex-direction: column;
  padding: var(--spacing-md);
  cursor: pointer;
  transition: all var(--transition-normal);
  position: relative;
  border: 2px solid transparent;
  
  &:hover {
    transform: translateY(-4px);
    box-shadow: var(--shadow-lg);
    
    .more-btn {
      opacity: 1;
    }
  }
  
  &.selected {
    border-color: var(--primary-color);
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.12) 0%, rgba(99, 102, 241, 0.05) 100%);
    box-shadow: 0 0 0 1px var(--primary-color), 0 4px 16px rgba(99, 102, 241, 0.25);
    
    .grid-name {
      color: var(--primary-color);
      font-weight: 600;
    }
    
    .grid-icon-wrapper {
      background: linear-gradient(135deg, rgba(99, 102, 241, 0.15) 0%, rgba(99, 102, 241, 0.08) 100%);
    }
    
    .grid-icon {
      color: var(--primary-color);
    }
    
    .grid-size {
      color: var(--primary-color);
    }
  }
}

.grid-item-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-sm);
}

.more-btn {
  width: 28px;
  height: 28px;
  border: none;
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: var(--radius-sm);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: all var(--transition-fast);
  
  &:hover {
    background: var(--primary-bg);
    color: var(--primary-color);
  }
}

.grid-icon-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-lg);
  margin-bottom: var(--spacing-sm);
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
}

.grid-icon {
  font-size: 56px;
  transition: transform var(--transition-spring);
  
  .grid-item:hover & {
    transform: scale(1.1);
  }
}

.grid-info {
  text-align: center;
}

.grid-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
  word-break: break-all;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  margin-bottom: var(--spacing-xs);
}

.grid-meta {
  display: flex;
  justify-content: center;
}

.grid-size {
  font-size: 12px;
  color: var(--text-secondary);
}

.grid-enter-active,
.grid-leave-active {
  transition: all var(--transition-normal);
}

.grid-enter-from {
  opacity: 0;
  transform: scale(0.9);
}

.grid-leave-to {
  opacity: 0;
  transform: scale(0.9);
}

.grid-move {
  transition: transform var(--transition-normal);
}
</style>
