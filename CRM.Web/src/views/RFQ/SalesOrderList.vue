<template>
  <div class="sales-order-list-page">
    <div class="page-header">
      <h2>销售订单</h2>
      <el-button type="primary" @click="handleCreate">新建销售订单</el-button>
    </div>
    <el-card class="table-card">
      <el-table :data="orderList" v-loading="loading">
        <el-table-column prop="sellOrderCode" label="订单号" />
        <el-table-column prop="customerName" label="客户" />
        <el-table-column prop="total" label="总金额" />
        <el-table-column prop="status" label="状态">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">{{ getStatusText(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" />
        <el-table-column label="操作" width="200">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleView(row)">查看</el-button>
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'

const router = useRouter()
const loading = ref(false)
const orderList = ref([])

const getStatusType = (status: number) => {
  const map: Record<number, string> = { 0: 'info', 1: 'success', 2: 'warning', 3: 'danger' }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = { 0: '草稿', 1: '已提交', 2: '审批中', 3: '已驳回' }
  return map[status] || '未知'
}

const handleCreate = () => {
  ElMessage.info('功能开发中')
}

const handleView = (row: any) => {
  ElMessage.info('功能开发中')
}

const handleEdit = (row: any) => {
  ElMessage.info('功能开发中')
}

onMounted(() => {
  // TODO: 加载销售订单列表
})
</script>

<style scoped>
.sales-order-list-page {
  padding: 20px;
}
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.page-header h2 {
  margin: 0;
  color: #E8F4FF;
}
.table-card {
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
}
</style>
