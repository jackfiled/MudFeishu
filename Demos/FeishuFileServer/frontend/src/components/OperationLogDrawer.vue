<template>
  <el-drawer
    v-model="visible"
    title="操作日志"
    direction="rtl"
    size="500px"
  >
    <el-table :data="logs" v-loading="loading" style="width: 100%">
      <el-table-column prop="operationType" label="操作类型" width="100">
        <template #default="{ row }">
          <el-tag :type="getOperationTagType(row.operationType)">
            {{ getOperationLabel(row.operationType) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="resourceName" label="资源名称" min-width="150" show-overflow-tooltip />
      <el-table-column prop="operationTime" label="操作时间" width="160">
        <template #default="{ row }">
          {{ formatDate(row.operationTime) }}
        </template>
      </el-table-column>
      <el-table-column prop="isSuccess" label="状态" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isSuccess ? 'success' : 'danger'">
            {{ row.isSuccess ? '成功' : '失败' }}
          </el-tag>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="page"
      :page-size="pageSize"
      :total="totalCount"
      layout="total, prev, pager, next"
      @current-change="loadLogs"
      style="margin-top: 20px; justify-content: flex-end;"
    />
  </el-drawer>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { operationLogApi } from '@/api'
import type { OperationLog } from '@/api/types'

interface Props {
  modelValue: boolean
  resourceToken?: string
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue'])

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const loading = ref(false)
const logs = ref<OperationLog[]>([])
const page = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)

const loadLogs = async () => {
  try {
    loading.value = true
    if (props.resourceToken) {
      const response = await operationLogApi.getByResource(props.resourceToken, page.value, pageSize.value)
      logs.value = response.logs
      totalCount.value = response.totalCount
    } else {
      const response = await operationLogApi.getByUser(page.value, pageSize.value)
      logs.value = response.logs
      totalCount.value = response.totalCount
    }
  } catch (error) {
    ElMessage.error('加载操作日志失败')
  } finally {
    loading.value = false
  }
}

watch(visible, (val) => {
  if (val) {
    page.value = 1
    loadLogs()
  }
})

const getOperationLabel = (type: string): string => {
  const labels: Record<string, string> = {
    Upload: '上传',
    Download: '下载',
    Delete: '删除',
    Restore: '恢复',
    Rename: '重命名',
    Move: '移动',
    Copy: '复制',
    CreateFolder: '创建文件夹',
    DeleteFolder: '删除文件夹',
    RestoreFolder: '恢复文件夹',
    Login: '登录',
    Logout: '退出',
    Register: '注册'
  }
  return labels[type] || type
}

const getOperationTagType = (type: string): string => {
  const types: Record<string, string> = {
    Upload: 'primary',
    Download: 'success',
    Delete: 'danger',
    Restore: 'warning',
    Rename: 'info',
    Move: 'info',
    Copy: 'info',
    CreateFolder: 'primary',
    DeleteFolder: 'danger',
    RestoreFolder: 'warning',
    Login: 'success',
    Logout: 'info',
    Register: 'primary'
  }
  return types[type] || 'info'
}

const formatDate = (dateStr: string): string => {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>
