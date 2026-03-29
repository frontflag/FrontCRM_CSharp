<template>
  <div class="sales-order-list-page customer-list-theme">
    <div class="page-header">
      <h2>销售订单管理</h2>
      <el-button type="primary" @click="handleCreate">
        <el-icon><Plus /></el-icon>新建销售订单
      </el-button>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ statTotal }}</div>
          <div class="stat-label">订单总数</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ statPending }}</div>
          <div class="stat-label">待处理</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ statApproved }}</div>
          <div class="stat-label">审核通过及以上</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-info">
          <div class="stat-value">{{ canViewSalesAmount ? `¥${statAmount.toLocaleString()}` : '--' }}</div>
          <div class="stat-label">总金额</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索筛选 -->
    <el-card class="filter-card">
      <el-form :inline="true" :model="filterForm">
        <el-form-item label="订单号">
          <el-input v-model="filterForm.code" placeholder="请输入订单号" clearable />
        </el-form-item>
        <el-form-item v-if="canViewCustomerInfo" label="客户">
          <el-input v-model="filterForm.customer" placeholder="请输入客户名称" clearable />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filterForm.status" placeholder="全部状态" clearable>
            <el-option v-for="opt in statusFilterOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <el-card class="table-card">
      <CrmDataTable
        :data="filteredList"
        v-loading="loading"
        highlight-current-row
        @row-dblclick="handleView"
      >
        <el-table-column prop="sellOrderCode" label="订单号" width="160" min-width="160" show-overflow-tooltip sortable>
          <template #default="{ row }">
            <el-link type="primary" @click="handleView(row)">{{ row.sellOrderCode }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="160" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column v-if="canViewCustomerInfo" prop="customerName" label="客户" min-width="200" show-overflow-tooltip />
        <el-table-column prop="salesUserName" label="业务员" width="100" />
        <el-table-column v-if="canViewSalesAmount" prop="total" label="总金额" width="160" align="right">
          <template #default="{ row }">
            <span class="amount">{{ formatCurrency(row.total, row.currency) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="itemRows" label="行项目" width="80" align="center" />
        <el-table-column prop="purchaseOrderStatus" label="采购状态" width="160" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getPurchaseStatusType(row.purchaseOrderStatus)" size="small">
              {{ getPurchaseStatusText(row.purchaseOrderStatus) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="deliveryDate" label="交货日期" width="160">
          <template #default="{ row }">
            {{ formatDisplayDate(row.deliveryDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" width="160">
          <template #default="{ row }">
            {{ formatDisplayDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.createUserName || row.createdBy || row.salesUserName || '—' }}
          </template>
        </el-table-column>
        <!-- 操作列须为最后一列 + fixed="right"，结构对齐客户列表 CustomerList 末列 td -->
        <el-table-column label="操作" width="480" min-width="480" fixed="right" class-name="op-col" label-class-name="op-col">
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div class="action-btns">
                <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">详情</button>
                <button type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">编辑</button>
                <button
                  v-if="row.status === 1 && canSubmitSalesOrderAudit"
                  type="button"
                  class="action-btn action-btn--warning"
                  @click.stop="submitForAudit(row)"
                >
                  提交审核
                </button>
                <button type="button" class="action-btn action-btn--warning" @click.stop="handleGeneratePO(row)">
                  生成采购单
                </button>
                <button type="button" class="action-btn action-btn--danger" @click.stop="handleDelete(row)">删除</button>
              </div>
            </div>
          </template>
        </el-table-column>
      </CrmDataTable>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, Search } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { salesOrderApi } from '@/api/salesOrder'
import { salesOrderStatusText, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()

const loading = ref(false)
const orderList = ref<any[]>([])
const authStore = useAuthStore()
const canViewCustomerInfo = computed(() => authStore.hasPermission('customer.info.read'))
const canViewSalesAmount = computed(() => authStore.hasPermission('sales.amount.read'))
/** 提交审核（新建→待审核） */
const canSubmitSalesOrderAudit = computed(() => authStore.hasPermission('sales-order.write'))

// 筛选表单
const filterForm = ref({
  code: '',
  customer: '',
  status: undefined as number | undefined
})

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 10,
  total: 0
})

// 对话框控制
// 计算属性：筛选后的列表
const filteredList = computed(() => {
  let result = orderList.value
  if (filterForm.value.code) {
    result = result.filter(o => o.sellOrderCode.toLowerCase().includes(filterForm.value.code.toLowerCase()))
  }
  if (filterForm.value.customer) {
    result = result.filter(o => o.customerName?.toLowerCase().includes(filterForm.value.customer.toLowerCase()))
  }
  if (filterForm.value.status !== undefined) {
    result = result.filter(o => o.status === filterForm.value.status)
  }
  pageInfo.value.total = result.length
  const start = (pageInfo.value.page - 1) * pageInfo.value.pageSize
  return result.slice(start, start + pageInfo.value.pageSize)
})

// 统计
const statTotal = computed(() => orderList.value.length)
const statusFilterOptions = [
  { label: '新建', value: 1 },
  { label: '待审核', value: 2 },
  { label: '审核通过', value: 10 },
  { label: '进行中', value: 20 },
  { label: '完成', value: 100 },
  { label: '审核失败', value: -1 },
  { label: '取消', value: -2 }
] as const

const terminalOkStatuses = new Set([10, 20, 100])

const statPending = computed(() => orderList.value.filter(o => o.status === 1 || o.status === 2).length)
const statApproved = computed(() => orderList.value.filter(o => terminalOkStatuses.has(o.status)).length)
const statAmount = computed(() => orderList.value.reduce((sum, o) => sum + (o.total || 0), 0))

// 格式化货币
const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// 状态处理
const getStatusType = (status: number) => salesOrderStatusTagType(status)
const getStatusText = (status: number) => salesOrderStatusText(status)

const getPurchaseStatusType = (status: number) => {
  const map: Record<number, string> = { 0: 'info', 1: 'warning', 2: 'success' }
  return map[status] || 'info'
}

const getPurchaseStatusText = (status: number) => {
  const map: Record<number, string> = { 0: '未采购', 1: '部分采购', 2: '全部采购' }
  return map[status] || '未知'
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await salesOrderApi.getList({ page: 1, pageSize: 2000 })
    orderList.value = (res as { items?: unknown[] }).items || []
    pageInfo.value.total = orderList.value.length
  } catch (error) {
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
}

const handleReset = () => {
  filterForm.value = { code: '', customer: '', status: undefined }
  pageInfo.value.page = 1
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
}

// 新建
const handleCreate = () => {
  router.push({ name: 'SalesOrderCreate' })
}

// 编辑
const handleEdit = (row: any) => {
  router.push({ name: 'SalesOrderCreate', query: { id: row.id, edit: '1' } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'SalesOrderDetail', params: { id: row.id } })
}

/** 新建(1) → 待审核(2) */
const submitForAudit = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定将销售订单 ${row.sellOrderCode} 提交审核吗？提交后上级可在「待审批」中处理。`,
      '提交审核',
      { type: 'info', confirmButtonText: '提交', cancelButtonText: '取消' }
    )
    await salesOrderApi.updateStatus(row.id, 2)
    ElMessage.success('已提交审核')
    await loadData()
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e instanceof Error ? e.message : '提交失败')
    }
  }
}

// 生成采购单
const handleGeneratePO = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要根据销售订单 ${row.sellOrderCode} 生成采购订单吗？`, '提示', { type: 'info' })
    await purchaseOrderApi.autoGenerate(row.id)
  } catch {
    // 取消
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除销售订单 ${row.sellOrderCode} 吗？`, '警告', { type: 'warning' })
    await salesOrderApi.delete(row.id)
    loadData()
  } catch {
    // 取消
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
.sales-order-list-page {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  h2 {
    margin: 0;
    color: #E8F4FF;
    font-size: 20px;
  }
}

.stat-row {
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-card__body) {
    padding: 15px;
  }
  .stat-value {
    font-size: 24px;
    font-weight: bold;
    color: #00D4FF;
    margin-bottom: 5px;
  }
  .stat-label {
    font-size: 14px;
    color: rgba(200, 216, 232, 0.6);
  }
}

.filter-card {
  margin-bottom: 20px;
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
}

.table-card {
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: rgba(0, 212, 255, 0.1);
    color: #E8F4FF;

    .el-table__cell .cell {
      white-space: nowrap;
    }
  }
}

.amount {
  color: #00D4FF;
  font-weight: 500;
}

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.items-section {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid rgba(0, 212, 255, 0.1);
  .items-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
    h4 {
      margin: 0;
      color: #E8F4FF;
    }
  }
  .total-amount {
    margin-top: 10px;
    text-align: right;
    font-size: 16px;
    color: #E8F4FF;
    .amount {
      font-size: 20px;
      font-weight: bold;
      color: #00D4FF;
    }
  }
}

.tags-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.doc-tab-content {
  padding: 8px 0;
}
.detail-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 16px;
  }
  :deep(.el-tabs__content) {
    min-height: 200px;
  }
}
</style>
