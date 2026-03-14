<template>
  <div class="share-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>我的分享</span>
        </div>
      </template>

      <el-table :data="shares" v-loading="loading" style="width: 100%">
        <el-table-column prop="resourceName" label="名称" min-width="200" />
        <el-table-column prop="resourceType" label="类型" width="100">
          <template #default="{ row }">
            <el-tag :type="row.resourceType === 'File' ? 'primary' : 'success'">
              {{ row.resourceType === 'File' ? '文件' : '文件夹' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="requirePassword" label="密码保护" width="100">
          <template #default="{ row }">
            <el-tag :type="row.requirePassword ? 'warning' : 'info'">
              {{ row.requirePassword ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="allowDownload" label="允许下载" width="100">
          <template #default="{ row }">
            <el-tag :type="row.allowDownload ? 'success' : 'danger'">
              {{ row.allowDownload ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="expireTime" label="过期时间" width="180">
          <template #default="{ row }">
            {{ row.expireTime ? formatDate(row.expireTime) : '永不过期' }}
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" size="small" @click="handleCopyLink(row)">
              复制链接
            </el-button>
            <el-button type="danger" size="small" @click="handleDelete(row)">
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="page"
        :page-size="pageSize"
        :total="totalCount"
        layout="total, prev, pager, next"
        @current-change="loadShares"
        style="margin-top: 20px; justify-content: flex-end;"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { shareApi } from '@/api'
import type { ShareResponse } from '@/api/types'

const loading = ref(false)
const shares = ref<ShareResponse[]>([])
const page = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)

const loadShares = async () => {
  try {
    loading.value = true
    const response = await shareApi.getMyShares(page.value, pageSize.value)
    shares.value = response.shares
    totalCount.value = response.totalCount
  } catch (error) {
    ElMessage.error('加载分享列表失败')
  } finally {
    loading.value = false
  }
}

const handleCopyLink = async (share: ShareResponse) => {
  const link = `${window.location.origin}/share/${share.shareCode}`
  try {
    await navigator.clipboard.writeText(link)
    ElMessage.success('链接已复制到剪贴板')
  } catch (error) {
    ElMessage.error('复制失败')
  }
}

const handleDelete = async (share: ShareResponse) => {
  try {
    await ElMessageBox.confirm('确定要删除此分享吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await shareApi.delete(share.id)
    ElMessage.success('分享已删除')
    loadShares()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除分享失败')
    }
  }
}

const formatDate = (dateStr: string): string => {
  return new Date(dateStr).toLocaleString('zh-CN')
}

onMounted(() => {
  loadShares()
})
</script>

<style scoped>
.share-list {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
