<template>
  <div class="sales-order-list-page customer-list-theme">
    <div class="page-header">
      <h2>销售订单</h2>
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

    <!-- 搜索栏：对齐客户列表 CustomerList search-bar -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">订单号</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.code"
            class="search-input"
            placeholder="请输入订单号"
            @keyup.enter="handleSearch"
          />
        </div>
        <template v-if="canViewCustomerInfo">
          <span class="filter-field-label">客户</span>
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filterForm.customer"
              class="search-input"
              placeholder="客户名称"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
        <el-select
          v-model="filterForm.status"
          placeholder="全部状态"
          clearable
          class="status-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="opt in statusFilterOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <button class="btn-primary btn-sm" type="button" @click="handleSearch">搜索</button>
        <button class="btn-ghost btn-sm" type="button" @click="handleReset">重置</button>
      </div>
    </div>

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
        <!-- 操作列：列表操作列规范（收起/展开 + 列头 >/< + 行内 ... 菜单） -->
        <el-table-column
          label="操作"
          :width="opColWidth"
          :min-width="opColMinWidth"
          fixed="right"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="op-col-header">
              <span class="op-col-header-text">操作</span>
              <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
                {{ opColExpanded ? '>' : '<' }}
              </button>
            </div>
          </template>

          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="opColExpanded" class="action-btns">
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
              </div>

              <el-dropdown v-else trigger="click" placement="bottom-end">
                <button type="button" class="op-more-trigger">...</button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop="handleView(row)">
                      <span class="op-more-item op-more-item--primary">详情</span>
                    </el-dropdown-item>
                    <el-dropdown-item @click.stop="handleEdit(row)">
                      <span class="op-more-item op-more-item--primary">编辑</span>
                    </el-dropdown-item>
                    <el-dropdown-item v-if="row.status === 1 && canSubmitSalesOrderAudit" @click.stop="submitForAudit(row)">
                      <span class="op-more-item op-more-item--warning">提交审核</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
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
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { salesOrderApi } from '@/api/salesOrder'
import { salesOrderStatusText, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const route = useRoute()

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

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 260
const OP_COL_EXPANDED_MIN_WIDTH = 240
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

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

function syncFiltersFromRoute() {
  if (route.name !== 'SalesOrderList') return
  const q = route.query
  filterForm.value.code = typeof q.code === 'string' ? q.code : ''
  filterForm.value.customer = typeof q.customer === 'string' ? q.customer : ''
  const st = q.status
  if (st === undefined || st === null || st === '') {
    filterForm.value.status = undefined
  } else {
    const n = Number(st)
    filterForm.value.status = Number.isNaN(n) ? undefined : n
  }
}

watch(
  () => [route.name, route.query] as const,
  () => syncFiltersFromRoute(),
  { deep: true, immediate: true }
)

// 搜索和重置（与左侧检索面板共用 query）
const handleSearch = () => {
  const query: Record<string, string> = {}
  const code = filterForm.value.code.trim()
  if (code) query.code = code
  const customer = filterForm.value.customer.trim()
  if (customer) query.customer = customer
  if (filterForm.value.status !== undefined && filterForm.value.status !== null) {
    query.status = String(filterForm.value.status)
  }
  router.replace({ name: 'SalesOrderList', query })
  pageInfo.value.page = 1
}

const handleReset = () => {
  filterForm.value = { code: '', customer: '', status: undefined }
  router.replace({ name: 'SalesOrderList', query: {} })
  pageInfo.value.page = 1
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
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

onMounted(loadData)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.sales-order-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  margin-bottom: 20px;
  h2 {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
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

// ---- 搜索栏（与 CustomerList 一致）----
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
}

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: $text-muted;
  pointer-events: none;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 32px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.status-select {
  width: 160px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }
  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
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
