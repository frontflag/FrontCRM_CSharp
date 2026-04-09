<template>
  <div class="sales-order-list-page customer-list-theme">
    <div class="page-header">
      <h2>{{ t('salesOrderList.title') }}</h2>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ statTotal }}</div>
          <div class="stat-label">{{ t('salesOrderList.stats.total') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ statPending }}</div>
          <div class="stat-label">{{ t('salesOrderList.stats.pending') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ statApproved }}</div>
          <div class="stat-label">{{ t('salesOrderList.stats.approvedPlus') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-info">
          <div class="stat-value">{{ canViewSalesAmount ? `¥${statAmount.toLocaleString()}` : '--' }}</div>
          <div class="stat-label">{{ t('salesOrderList.stats.totalAmount') }}</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏：对齐客户列表 CustomerList search-bar -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('salesOrderList.filters.orderCode') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.code"
            class="search-input"
            :placeholder="t('salesOrderList.filters.orderCodePlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <template v-if="canViewCustomerInfo">
          <span class="filter-field-label">{{ t('salesOrderList.filters.customer') }}</span>
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filterForm.customer"
              class="search-input"
              :placeholder="t('salesOrderList.filters.customerPlaceholder')"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
        <el-select
          v-model="filterForm.status"
          :placeholder="t('salesOrderList.filters.allStatus')"
          clearable
          class="status-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="opt in statusFilterOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <button class="btn-primary btn-sm" type="button" @click="handleSearch">{{ t('salesOrderList.filters.search') }}</button>
        <button class="btn-ghost btn-sm" type="button" @click="handleReset">{{ t('salesOrderList.filters.reset') }}</button>
      </div>
    </div>

    <!-- 数据表格 -->
    <el-card class="table-card">
      <CrmDataTable
        ref="listTableRef"
        column-layout-key="sales-order-list-main-v2"
        :columns="salesOrderTableColumns"
        :show-column-settings="false"
        :data="filteredList"
        v-loading="loading"
        highlight-current-row
        @row-dblclick="handleView"
        @current-change="onTableCurrentRowChange"
      >
        <template #col-sellOrderCode="{ row }">
          <el-link type="primary" @click="handleView(row)">{{ row.sellOrderCode }}</el-link>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template #col-total="{ row }">
          <span class="amount">{{ formatCurrency(row.total, row.currency) }}</span>
        </template>
        <template #col-createTime="{ row }">
          {{ formatDisplayDateTime(row.createTime) }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || row.salesUserName || '—' }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('salesOrderList.columns.actions') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>

        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('salesOrderList.actions.detail') }}</button>
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">{{ t('salesOrderList.actions.edit') }}</button>
              <button
                v-if="row.status === 1 && canSubmitSalesOrderAudit"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="submitForAudit(row)"
              >
                {{ t('salesOrderList.actions.submitAudit') }}
              </button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('salesOrderList.actions.detail') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="handleEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('salesOrderList.actions.edit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="row.status === 1 && canSubmitSalesOrderAudit" @click.stop="submitForAudit(row)">
                    <span class="op-more-item op-more-item--warning">{{ t('salesOrderList.actions.submitAudit') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="listTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
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
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { salesOrderApi } from '@/api/salesOrder'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()

const loading = ref(false)
const orderList = ref<any[]>([])
const listTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const authStore = useAuthStore()
/** 订单上的客户名属销售业务上下文：业务员有 sales-order.read 即可见列与筛选，不必具备客户主数据权限 customer.info.read */
const canViewCustomerInfo = computed(
  () =>
    authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewSalesAmount = computed(() => authStore.hasPermission('sales.amount.read'))
/** 提交审核（新建→待审核） */
const canSubmitSalesOrderAudit = computed(() => authStore.hasPermission('sales-order.write'))

/** 当前高亮行 id（分页/筛选时尽量保持同一订单行） */
const listFocusedOrderId = ref('')

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

/** 销售订单列表主表可配置列（localStorage：crm-table-columns:v1:sales-order-list-main） */
const salesOrderTableColumns = computed((): CrmTableColumnDef[] => {
  void locale.value
  return [
  {
    key: 'sellOrderCode',
    label: t('salesOrderList.columns.orderCode'),
    prop: 'sellOrderCode',
    width: 160,
    minWidth: 160,
    showOverflowTooltip: true,
    sortable: true
  },
  { key: 'status', label: t('salesOrderList.columns.status'), prop: 'status', width: 160, align: 'center' as const },
  ...(canViewCustomerInfo.value
    ? [{ key: 'customerName', label: t('salesOrderList.columns.customer'), prop: 'customerName', minWidth: 200, showOverflowTooltip: true }]
    : []),
  { key: 'salesUserName', label: t('salesOrderList.columns.salesUser'), prop: 'salesUserName', width: 120, minWidth: 120, showOverflowTooltip: true },
  ...(canViewSalesAmount.value ? [{ key: 'total', label: t('salesOrderList.columns.totalAmount'), prop: 'total', width: 160, align: 'right' as const }] : []),
  { key: 'itemRows', label: t('salesOrderList.columns.itemRows'), prop: 'itemRows', width: 80, align: 'center' as const },
  { key: 'createTime', label: t('salesOrderList.columns.createTime'), prop: 'createTime', width: 160 },
  { key: 'createUser', label: t('salesOrderList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('salesOrderList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
  ]
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
const statusFilterOptions = computed(() => {
  void locale.value
  return [
    { label: t('salesOrderList.status.new'), value: 1 },
    { label: t('salesOrderList.status.pendingReview'), value: 2 },
    { label: t('salesOrderList.status.approved'), value: 10 },
    { label: t('salesOrderList.status.inProgress'), value: 20 },
    { label: t('salesOrderList.status.completed'), value: 100 },
    { label: t('salesOrderList.status.reviewFailed'), value: -1 },
    { label: t('salesOrderList.status.cancelled'), value: -2 }
  ]
})

const terminalOkStatuses = new Set([10, 20, 100])

const statPending = computed(() => orderList.value.filter(o => o.status === 1 || o.status === 2).length)
const statApproved = computed(() => orderList.value.filter(o => terminalOkStatuses.has(o.status)).length)
const statAmount = computed(() =>
  orderList.value.reduce((sum, o) => {
    const n = Number((o as { total?: unknown }).total)
    return sum + (Number.isFinite(n) ? n : 0)
  }, 0)
)

// 格式化货币
const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// 状态处理
const getStatusType = (status: number) => salesOrderStatusTagType(status)
const getStatusText = (status: number) => translateSalesOrderStatus(status, t)

/** 分页/筛选后同步表格当前行：仍在本页则保持，否则落到本页首行 */
function syncTableCurrentRowFromPage() {
  const rows = filteredList.value
  if (!rows.length) {
    listFocusedOrderId.value = ''
    listTableRef.value?.setCurrentRow(undefined)
    return
  }
  const cur = listFocusedOrderId.value
  if (cur && rows.some((r) => String(r.id) === cur)) {
    const hit = rows.find((r) => String(r.id) === cur)
    if (hit) listTableRef.value?.setCurrentRow(hit)
    return
  }
  const r = rows[0] as { id?: string }
  listFocusedOrderId.value = String(r.id ?? '')
  listTableRef.value?.setCurrentRow(rows[0])
}

function onTableCurrentRowChange(row: Record<string, unknown> | null) {
  if (!row?.id) {
    listFocusedOrderId.value = ''
    return
  }
  listFocusedOrderId.value = String(row.id)
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await salesOrderApi.getList({ page: 1, pageSize: 2000 })
    orderList.value = (res as { items?: unknown[] }).items || []
    pageInfo.value.total = orderList.value.length
    syncTableCurrentRowFromPage()
  } catch (error) {
    ElMessage.error(t('salesOrderList.loadFailed'))
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

/** 分页 / 筛选变化时同步表格当前行 */
watch(
  () =>
    [pageInfo.value.page, pageInfo.value.pageSize, filterForm.value.code, filterForm.value.customer, filterForm.value.status, orderList.value.length] as const,
  () => {
    if (!orderList.value.length) return
    syncTableCurrentRowFromPage()
  }
)

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
      t('salesOrderList.submitAuditConfirm', { code: row.sellOrderCode }),
      t('salesOrderList.actions.submitAudit'),
      { type: 'info', confirmButtonText: t('salesOrderList.submit'), cancelButtonText: t('common.cancel') }
    )
    await salesOrderApi.updateStatus(row.id, 2)
    ElMessage.success(t('salesOrderList.submitAuditSuccess'))
    await loadData()
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e instanceof Error ? e.message : t('salesOrderList.submitAuditFailed'))
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
  background: $layer-3;
  border: 1px solid $border-card;
  :deep(.el-card__body) {
    padding: 15px;
  }
  .stat-value {
    font-size: 24px;
    font-weight: bold;
    color: $cyan-primary;
    margin-bottom: 5px;
  }
  .stat-label {
    font-size: 14px;
    color: $text-muted;
  }
  &.stat-warning .stat-value {
    color: $warning-color;
  }
  &.stat-success .stat-value {
    color: $success-color;
  }
  &.stat-info .stat-value {
    color: $info-color;
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
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
  flex-shrink: 0;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
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
