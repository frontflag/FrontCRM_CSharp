<template>
  <div class="purchase-order-list-page">
    <div class="page-header">
      <h2>采购订单管理</h2>
      <el-button type="primary" @click="handleCreate">
        <el-icon><Plus /></el-icon>新建采购订单
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
          <div class="stat-label">待确认</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ statInProgress }}</div>
          <div class="stat-label">进行中</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-info">
          <div class="stat-value">{{ canViewPurchaseAmount ? `¥${statAmount.toLocaleString()}` : '--' }}</div>
          <div class="stat-label">采购总额</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索筛选 -->
    <el-card class="filter-card">
      <el-form :inline="true" :model="filterForm">
        <el-form-item label="订单号">
          <el-input v-model="filterForm.code" placeholder="请输入订单号" clearable />
        </el-form-item>
        <el-form-item v-if="canViewVendorInfo" label="供应商">
          <el-input v-model="filterForm.vendor" placeholder="请输入供应商" clearable />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filterForm.status" placeholder="全部状态" clearable>
            <el-option label="新建" :value="1" />
            <el-option label="待审核" :value="2" />
            <el-option label="审核通过" :value="10" />
            <el-option label="待确认" :value="20" />
            <el-option label="已确认" :value="30" />
            <el-option label="进行中" :value="50" />
            <el-option label="采购完成" :value="100" />
            <el-option label="审核失败" :value="-1" />
            <el-option label="取消" :value="-2" />
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
      >
        <el-table-column prop="purchaseOrderCode" label="订单号" width="160" min-width="160" show-overflow-tooltip sortable>
          <template #default="{ row }">
            <el-link type="primary" @click="handleView(row)">{{ row.purchaseOrderCode }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="160" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column v-if="canViewVendorInfo" prop="vendorName" label="供应商" min-width="200" show-overflow-tooltip />
        <el-table-column prop="purchaseUserName" label="采购员" width="100" />
        <el-table-column v-if="canViewPurchaseAmount" prop="total" label="总金额" width="160" align="right">
          <template #default="{ row }">
            <span class="amount">{{ formatCurrency(row.total, row.currency) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="itemRows" label="行项目" width="80" align="center" />
        <el-table-column prop="stockStatus" label="入库状态" width="160" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getStockStatusType(row.stockStatus)" size="small">
              {{ getStockStatusText(row.stockStatus) }}
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
            {{ row.createUserName || row.createdBy || row.purchaseUserName || '—' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" min-width="400" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleView(row)">详情</el-button>
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button
              v-if="row.status < 10"
              link
              type="danger"
              @click="cancelOrder(row)"
            >
              取消订单
            </el-button>
            <el-button
              v-if="(row.status >= 1 && row.status < 10) || row.status === -1"
              link
              type="warning"
              @click="submitAudit(row)"
            >
              提交审核
            </el-button>
            <el-button
              v-if="row.status >= 10 && row.status < 30"
              link
              type="success"
              @click="confirmBySupplier(row)"
            >
              供应商确认
            </el-button>
            <el-button
              v-if="row.status === 30"
              link
              type="danger"
              @click="cancelSupplierConfirm(row)"
            >
              取消供应商确认
            </el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
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
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()

const loading = ref(false)
const orderList = ref<any[]>([])
const authStore = useAuthStore()
const canViewVendorInfo = computed(() => authStore.hasPermission('vendor.info.read'))
const canViewPurchaseAmount = computed(() => authStore.hasPermission('purchase.amount.read'))

// 筛选表单
const filterForm = ref({
  code: '',
  vendor: '',
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
    result = result.filter(o => o.purchaseOrderCode.toLowerCase().includes(filterForm.value.code.toLowerCase()))
  }
  if (filterForm.value.vendor) {
    result = result.filter(o => o.vendorName?.toLowerCase().includes(filterForm.value.vendor.toLowerCase()))
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
const statPending = computed(() => orderList.value.filter(o => o.status === 20).length)
const statInProgress = computed(() => orderList.value.filter(o => o.status === 50).length)
const statAmount = computed(() => orderList.value.reduce((sum, o) => sum + (o.total || 0), 0))

// 格式化货币
const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// 状态处理
const getStatusType = (status: number) => {
  const map: Record<number, string> = { 
    1: 'info',
    2: 'warning',
    10: 'success',
    20: 'warning',
    30: 'primary',
    50: 'primary',
    100: 'success',
    '-1': 'danger',
    '-2': 'info'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    1: '新建',
    2: '待审核',
    10: '审核通过',
    20: '待确认',
    30: '已确认',
    50: '进行中',
    100: '采购完成',
    '-1': '审核失败',
    '-2': '取消'
  }
  return map[status] || '未知'
}

const getStockStatusType = (status: number) => {
  const map: Record<number, string> = { 0: 'info', 1: 'warning', 2: 'success' }
  return map[status] || 'info'
}

const getStockStatusText = (status: number) => {
  const map: Record<number, string> = { 0: '未入库', 1: '部分入库', 2: '全部入库' }
  return map[status] || '未知'
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await purchaseOrderApi.getList({ page: 1, pageSize: 2000 })
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
  filterForm.value = { code: '', vendor: '', status: undefined }
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
  router.push({ name: 'PurchaseOrderCreate' })
}

// 编辑
const handleEdit = (row: any) => {
  router.push({ name: 'PurchaseOrderCreate', query: { id: row.id, edit: '1' } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.id } })
}

/** 供应商确认：待确认(20) -> 已确认(30) */
const confirmBySupplier = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${row.purchaseOrderCode} 标记为“已确认”吗？`,
      '供应商确认',
      { type: 'info', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
    // 允许从“审核通过(10)”推进到“待确认(20)”再到“已确认(30)”
    if (row.status === 10) {
      await purchaseOrderApi.updateStatus(row.id, 20)
    }
    await purchaseOrderApi.updateStatus(row.id, 30)
    ElMessage.success('供应商确认成功')
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

/** 取消供应商确认：仅「已确认(30)」时显示；将订单标记为取消 */
const cancelSupplierConfirm = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${row.purchaseOrderCode} 执行“取消供应商确认”吗？`,
      '取消供应商确认',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
    await purchaseOrderApi.updateStatus(row.id, -2)
    ElMessage.success('已取消供应商确认')
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

/** 取消订单：仅在 status < 10 显示 */
const cancelOrder = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${row.purchaseOrderCode} 标记为“取消”吗？`,
      '取消订单',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
    await purchaseOrderApi.updateStatus(row.id, -2)
    ElMessage.success('订单已取消')
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

/** 提交审核 */
const submitAudit = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${row.purchaseOrderCode} 提交审核吗？`,
      '提交审核',
      { type: 'info', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
    // 审核失败(-1)先回到新建(1)，再提交审核(2)
    if (row.status === -1) {
      await purchaseOrderApi.updateStatus(row.id, 1)
    }
    await purchaseOrderApi.updateStatus(row.id, 2)
    ElMessage.success('提交审核成功')
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除采购订单 ${row.purchaseOrderCode} 吗？`, '警告', { type: 'warning' })
    await purchaseOrderApi.delete(row.id)
    loadData()
  } catch {
    // 取消
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
.purchase-order-list-page {
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

    // 操作列按钮禁止折行
    .el-table__cell .el-button {
      white-space: nowrap !important;
    }
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
