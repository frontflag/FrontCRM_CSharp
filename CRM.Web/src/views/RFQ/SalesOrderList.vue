<template>
  <div class="sales-order-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="16" y1="13" x2="8" y2="13" />
              <line x1="16" y1="17" x2="8" y2="17" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('salesOrderList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('salesOrderList.count', { count: pageInfo.total }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="loadData">{{ t('salesOrderList.filters.refresh') }}</button>
      </div>
    </div>

    <!-- 统计卡片（列表模式） -->
    <el-row v-if="viewMode === 'list'" :gutter="20" class="stat-row">
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
          <div class="stat-value">{{ maskSaleSensitiveFields ? '—' : canViewSalesAmount ? `$${statAmount.toLocaleString()}` : '--' }}</div>
          <div class="stat-label">{{ t('salesOrderList.stats.totalAmount') }}</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏：状态 → 订单号 → 客户 → 业务员 → 备注 → 创建日期 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filterForm.status"
          :placeholder="t('salesOrderList.filters.allStatus')"
          multiple
          collapse-tags
          collapse-tags-tooltip
          clearable
          class="status-select filter-select--progress"
          popper-class="progress-multi-select-dropdown"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="opt in statusFilterOptions" :key="opt.value" :label="opt.label" :value="opt.value">
            <ProgressMultiSelectOption
              :label="opt.label"
              :checked="filterForm.status.includes(opt.value)"
            />
          </el-option>
        </el-select>
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
        <template v-if="canViewCustomerInfo && !maskSaleSensitiveFields">
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
        <template v-if="!maskSaleSensitiveFields">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filterForm.salesUserName"
              class="search-input"
              :placeholder="t('salesOrderList.filters.salesUserPlaceholder')"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.comment"
            class="search-input"
            :placeholder="t('salesOrderList.filters.commentPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-date-picker
          v-model="filterForm.createDateRange"
          type="daterange"
          :range-separator="t('salesOrderList.filters.createDateSep')"
          :start-placeholder="t('salesOrderList.filters.createDateFrom')"
          :end-placeholder="t('salesOrderList.filters.createDateTo')"
          value-format="YYYY-MM-DD"
          class="filter-date-range so-list-date-range"
          clearable
          :teleported="false"
        />
        <button class="btn-primary btn-sm" type="button" @click="handleSearch">{{ t('salesOrderList.filters.search') }}</button>
        <button class="btn-ghost btn-sm" type="button" @click="handleReset">{{ t('salesOrderList.filters.reset') }}</button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('salesOrderList.filters.listView') : t('salesOrderList.filters.boardView') }}
        </button>
      </div>
    </div>

    <SalesOrderListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <div v-show="viewMode === 'list'" class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="listTableRef"
        column-layout-key="sales-order-list-main-v2"
        :columns="salesOrderTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="orderList"
        row-key="id"
        highlight-current-row
        @row-dblclick="onSalesOrderRowDblClick"
        @current-change="onTableCurrentRowChange"
        @header-dragend="onSalesOrderTableHeaderDragEnd"
      >
        <template #col-sellOrderCode="{ row }">
          <el-link type="primary" @click="handleView(row)">{{ row.sellOrderCode }}</el-link>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template v-if="canViewCustomerInfo" #col-customer-header>
          <CustomerExtendColumnHeader
            :active-field="customerExtendActiveField"
            @set-active-field="setCustomerExtendActiveField"
          />
        </template>
        <template v-if="canViewCustomerInfo" #col-customer="{ row }">
          <CustomerExtendCell
            :row="row"
            :active-field="customerExtendActiveField"
            :masked="maskSaleSensitiveFields"
            :empty-text="t('quoteList.na')"
          />
        </template>
        <template #col-total="{ row }">
          <template v-if="maskSaleSensitiveFields">
            <span class="dock-tier-empty">—</span>
          </template>
          <template v-else-if="!listTotalAmountHasValue(row.total)">
            <span class="dock-tier-empty">—</span>
          </template>
          <div v-else class="dock-tier-price-line">
            <template v-for="amt in [splitListMoneyParts(Number(row.total))]" :key="'so-total-' + row.id">
              <span class="dock-tier-amt">
                <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
              </span>
            </template>
            <span class="dock-tier-ccy-gap">&nbsp;</span>
            <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
          </div>
        </template>
        <template #col-createTime="{ row }">
          {{ formatDisplayDateTime(row.createTime) }}
        </template>
        <template #col-comment="{ row }">
          {{ row.headerRemarkDisplay || row.comment || '—' }}
        </template>
        <template #col-assistorUserName="{ row }">
          {{ row.assistorUserName || row.AssistorUserName || '—' }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || '—' }}
        </template>
        <template #col-actions-header>
          <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleOpCol"
            >
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>

        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('salesOrderList.actions.detail') }}</button>
              <button v-if="canWriteSaleData" type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">{{ t('salesOrderList.actions.edit') }}</button>
              <button type="button" class="action-btn action-btn--primary" @click.stop="handlePrintReport(row)">
                {{ t('salesOrderList.actions.printReport') }}
              </button>
              <button
                v-if="canWriteSaleData && row.status === 1 && canSubmitSalesOrderAudit"
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
                  <el-dropdown-item v-if="canWriteSaleData" @click.stop="handleEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('salesOrderList.actions.edit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="handlePrintReport(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('salesOrderList.actions.printReport') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="canWriteSaleData && row.status === 1 && canSubmitSalesOrderAudit" @click.stop="submitForAudit(row)">
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
          <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
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
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { salesOrderApi } from '@/api/salesOrder'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  listTotalAmountHasValue,
  splitListMoneyParts
} from '@/utils/moneyFormat'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import CustomerExtendColumnHeader from '@/components/list/CustomerExtendColumnHeader.vue'
import CustomerExtendCell from '@/components/list/CustomerExtendCell.vue'
import { useCustomerExtendColumn, isCustomerExtendTableColumn } from '@/composables/useCustomerExtendColumn'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import SalesOrderListBoard from './SalesOrderListBoard.vue'
import type { SalesOrderListAnalyticsQuery } from '@/api/salesOrderAnalytics'
import ProgressMultiSelectOption from '@/components/common/ProgressMultiSelectOption.vue'
import {
  assignSalesOrderStatusesParam,
  formatSalesOrderStatusesForRoute,
  normalizeSalesOrderStatuses
} from '@/utils/salesOrderStatusQuery'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()

const loading = ref(false)
const viewMode = ref<'list' | 'board'>('list')
const orderList = ref<any[]>([])
const listTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const authStore = useAuthStore()
const { canWriteSaleData } = useDepartmentDataReadOnly()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const {
  expanded: customerExtendExpanded,
  activeField: customerExtendActiveField,
  colWidth: customerExtendColWidth,
  colMinWidth: customerExtendColMinWidth,
  setActiveField: setCustomerExtendActiveField,
  applyOuterWidthFromTable: applyCustomerExtendOuterWidth
} = useCustomerExtendColumn()

function onSalesOrderTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isCustomerExtendTableColumn(column)) return
  applyCustomerExtendOuterWidth(newWidth)
}

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
  salesUserName: '',
  comment: '',
  createDateRange: null as [string, string] | null,
  status: [] as number[]
})

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

/** 销售订单列表主表可配置列（localStorage：crm-table-columns:v1:sales-order-list-main） */
const salesOrderTableColumns = computed((): CrmTableColumnDef[] => {
  void locale.value
  void customerExtendExpanded.value
  void customerExtendColWidth.value
  return [
  { key: 'status', label: t('salesOrderList.columns.status'), prop: 'status', width: 160, align: 'center' as const },
  ...(canViewCustomerInfo.value
    ? [{
        key: 'customer',
        label: t('common.customerExtendCol.columnTitle'),
        prop: 'customer',
        minWidth: customerExtendColMinWidth.value,
        width: customerExtendColWidth.value,
        showOverflowTooltip: true,
        className: 'customer-extend-col',
        labelClassName: 'customer-extend-col'
      }]
    : []),
  { key: 'salesUserName', label: t('salesOrderList.columns.salesUser'), prop: 'salesUserName', width: 120, minWidth: 120, showOverflowTooltip: true },
  { key: 'assistorUserName', label: t('salesOrderList.columns.assistor'), prop: 'assistorUserName', width: 120, minWidth: 120, showOverflowTooltip: true },
  ...(canViewSalesAmount.value ? [{ key: 'total', label: t('salesOrderList.columns.totalAmount'), prop: 'total', width: 160, align: 'right' as const }] : []),
  {
    key: 'itemRows',
    label: t('salesOrderList.columns.itemRows'),
    prop: 'itemRows',
    width: 120,
    minWidth: 120,
    align: 'center' as const
  },
  {
    key: 'comment',
    label: t('salesOrderList.columns.comment'),
    prop: 'comment',
    minWidth: 160,
    showOverflowTooltip: true
  },
  {
    key: 'sellOrderCode',
    label: t('salesOrderList.columns.orderCode'),
    prop: 'sellOrderCode',
    width: 160,
    minWidth: 160,
    showOverflowTooltip: true,
    sortable: true
  },
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
    labelClassName: 'op-col',
  resizable: false
  }
  ]
})

// 对话框控制
/** 与当前筛选条件一致的全量汇总（来自后端 aggregates，非仅当前页） */
const listAggregates = ref({
  totalCount: 0,
  pendingCount: 0,
  approvedPlusCount: 0,
  totalAmountSum: 0
})

const statTotal = computed(() => listAggregates.value.totalCount)
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

const statPending = computed(() => listAggregates.value.pendingCount)
const statApproved = computed(() => listAggregates.value.approvedPlusCount)
const statAmount = computed(() => listAggregates.value.totalAmountSum)

const boardFilters = computed((): SalesOrderListAnalyticsQuery => {
  const q: SalesOrderListAnalyticsQuery = {}
  const code = filterForm.value.code.trim()
  if (code) q.code = code
  if (canViewCustomerInfo.value) {
    const customer = filterForm.value.customer.trim()
    if (customer) q.customer = customer
  }
  if (!maskSaleSensitiveFields.value) {
    const salesUser = filterForm.value.salesUserName.trim()
    if (salesUser) q.salesUserName = salesUser
  }
  const cm = filterForm.value.comment.trim()
  if (cm) q.comment = cm
  const statuses = normalizeSalesOrderStatuses(filterForm.value.status)
  if (statuses.length) q.status = statuses
  if (filterForm.value.createDateRange?.[0]) q.startDate = filterForm.value.createDateRange[0]
  if (filterForm.value.createDateRange?.[1]) q.endDate = filterForm.value.createDateRange[1]
  return q
})

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
}

// 状态处理
const getStatusType = (status: number) => salesOrderStatusTagType(status)
const getStatusText = (status: number) => translateSalesOrderStatus(status, t)

/** 分页/筛选后同步表格当前行：仍在本页则保持，否则落到本页首行 */
function syncTableCurrentRowFromPage() {
  const rows = orderList.value
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
    const params: Record<string, unknown> = {
      page: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize
    }
    const code = filterForm.value.code.trim()
    if (code) params.code = code
    if (canViewCustomerInfo.value) {
      const customer = filterForm.value.customer.trim()
      if (customer) params.customer = customer
    }
    assignSalesOrderStatusesParam(params, 'status', filterForm.value.status)
    if (!maskSaleSensitiveFields.value) {
      const salesUser = filterForm.value.salesUserName.trim()
      if (salesUser) params.salesUserName = salesUser
    }
    const cm = filterForm.value.comment.trim()
    if (cm) params.comment = cm
    if (filterForm.value.createDateRange?.[0]) params.startDate = filterForm.value.createDateRange[0]
    if (filterForm.value.createDateRange?.[1]) params.endDate = filterForm.value.createDateRange[1]

    const res = (await salesOrderApi.getList(params)) as {
      items?: any[]
      total?: number
      page?: number
      pageSize?: number
      aggregates?: {
        totalCount?: number
        pendingCount?: number
        approvedPlusCount?: number
        totalAmountSum?: number | null
      }
    }
    const items = res.items ?? []
    const nTotal = res.total ?? 0
    if (pageInfo.value.page > 1 && items.length === 0 && nTotal > 0) {
      pageInfo.value.page = 1
      await loadData()
      return
    }
    orderList.value = items
    pageInfo.value.total = nTotal
    if (typeof res.page === 'number' && res.page >= 1) pageInfo.value.page = res.page
    if (typeof res.pageSize === 'number' && res.pageSize >= 1) pageInfo.value.pageSize = res.pageSize
    const agg = res.aggregates
    if (agg) {
      listAggregates.value = {
        totalCount: agg.totalCount ?? 0,
        pendingCount: agg.pendingCount ?? 0,
        approvedPlusCount: agg.approvedPlusCount ?? 0,
        totalAmountSum: agg.totalAmountSum != null && Number.isFinite(Number(agg.totalAmountSum)) ? Number(agg.totalAmountSum) : 0
      }
    } else {
      listAggregates.value = { totalCount: nTotal, pendingCount: 0, approvedPlusCount: 0, totalAmountSum: 0 }
    }
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
  filterForm.value.salesUserName = typeof q.salesUserName === 'string' ? q.salesUserName : ''
  filterForm.value.comment = typeof q.comment === 'string' ? q.comment : ''
  const from = typeof q.startDate === 'string' ? q.startDate : ''
  const to = typeof q.endDate === 'string' ? q.endDate : ''
  filterForm.value.createDateRange = from && to ? [from, to] : null
  filterForm.value.status = normalizeSalesOrderStatuses(q.status)
}

watch(
  () => [route.name, route.query] as const,
  async () => {
    syncFiltersFromRoute()
    if (route.name === 'SalesOrderList') await loadData()
  },
  { deep: true, immediate: true }
)

// 搜索和重置（与左侧检索面板共用 query）
const handleSearch = () => {
  const query: Record<string, string> = {}
  const code = filterForm.value.code.trim()
  if (code) query.code = code
  const customer = filterForm.value.customer.trim()
  if (customer) query.customer = customer
  const salesUser = filterForm.value.salesUserName.trim()
  if (salesUser) query.salesUserName = salesUser
  const cm = filterForm.value.comment.trim()
  if (cm) query.comment = cm
  if (filterForm.value.createDateRange?.[0]) query.startDate = filterForm.value.createDateRange[0]
  if (filterForm.value.createDateRange?.[1]) query.endDate = filterForm.value.createDateRange[1]
  const statusQ = formatSalesOrderStatusesForRoute(filterForm.value.status)
  if (statusQ) query.status = statusQ
  pageInfo.value.page = 1
  router.replace({ name: 'SalesOrderList', query })
}

const handleReset = () => {
  filterForm.value = {
    code: '',
    customer: '',
    salesUserName: '',
    comment: '',
    createDateRange: null,
    status: []
  }
  pageInfo.value.page = 1
  router.replace({ name: 'SalesOrderList', query: {} })
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
  pageInfo.value.page = 1
  void loadData()
}

const handlePageChange = () => {
  void loadData()
}

/** 分页后同步表格当前行 */
watch(
  () => [pageInfo.value.page, pageInfo.value.pageSize, orderList.value.length] as const,
  () => {
    if (!orderList.value.length) return
    syncTableCurrentRowFromPage()
  }
)

// 编辑（独立路由 /sales-orders/:id/edit，与采购订单 /purchase-orders/:id/edit 一致）
const handleEdit = (row: { id?: string }) => {
  if (!row?.id) return
  router.push({ name: 'SalesOrderEdit', params: { id: String(row.id) } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'SalesOrderDetail', params: { id: row.id } })
}

function onSalesOrderRowDblClick(row: { id?: string }, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: canWriteSaleData.value,
    onEdit: handleEdit,
    onDefault: handleView,
  })
}

const handlePrintReport = (row: { id?: string }) => {
  if (!row?.id) return
  router.push({ name: 'SalesOrderReport', params: { id: String(row.id) } })
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
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  gap: 12px;
  flex-wrap: wrap;
  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }
  .header-right {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-shrink: 0;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 36px;
    height: 36px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }
  .page-title {
    font-size: 20px;
    font-weight: 600;
    color: $text-primary;
    margin: 0;
  }
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

.table-wrapper {
  min-height: 120px;
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

.filter-date-range.so-list-date-range {
  width: 260px;
  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
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
  width: 168px;
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

.btn-board-active {
  border-color: rgba(0, 212, 255, 0.45);
  color: #00d4ff;
  background: rgba(0, 212, 255, 0.08);
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

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
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
