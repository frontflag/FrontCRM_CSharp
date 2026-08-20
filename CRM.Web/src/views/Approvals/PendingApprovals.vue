<template>
  <div class="pending-approvals-page">
    <div class="page-header">
      <h2 class="page-title">{{ t('pendingApprovals.title') }}</h2>
      <button type="button" class="btn-approval-desktop" @click="openApprovalDesktop">
        <span>{{ t('pendingApprovals.openApprovalDesktop') }}</span>
        <el-icon class="btn-approval-desktop__arrow"><ArrowRight /></el-icon>
      </button>
    </div>

    <div class="stats-row">
      <div class="stat-card stat-card--pending">
        <div class="stat-label">{{ t('pendingApprovals.stats.pending') }}</div>
        <div class="stat-value">{{ pendingCount }}</div>
      </div>
      <div class="stat-card stat-card--approved">
        <div class="stat-label">{{ t('pendingApprovals.stats.approved') }}</div>
        <div class="stat-value">{{ approvedCount }}</div>
      </div>
      <div class="stat-card stat-card--rejected">
        <div class="stat-label">{{ t('pendingApprovals.stats.rejected') }}</div>
        <div class="stat-value">{{ rejectedCount }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <span class="search-label">{{ t('pendingApprovals.filters.bizType') }}</span>
        <el-select
          v-model="searchForm.bizType"
          :placeholder="t('pendingApprovals.filters.all')"
          clearable
          style="width: 160px"
        >
          <el-option :label="t('pendingApprovals.bizType.CUSTOMER')" value="CUSTOMER" />
          <el-option :label="t('pendingApprovals.bizType.VENDOR')" value="VENDOR" />
          <el-option :label="t('pendingApprovals.bizType.SALES_ORDER')" value="SALES_ORDER" />
          <el-option :label="t('pendingApprovals.bizType.PURCHASE_ORDER')" value="PURCHASE_ORDER" />
          <el-option :label="t('pendingApprovals.bizType.FINANCE_PAYMENT')" value="FINANCE_PAYMENT" />
        </el-select>
        <span class="search-label">{{ t('pendingApprovals.filters.submittedRange') }}</span>
        <el-date-picker
          v-model="submittedDateRange"
          type="daterange"
          :range-separator="t('pendingApprovals.filters.to')"
          :start-placeholder="t('pendingApprovals.filters.startDate')"
          :end-placeholder="t('pendingApprovals.filters.endDate')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <span class="search-label">{{ t('pendingApprovals.filters.documentCode') }}</span>
        <el-input
          v-model="searchForm.documentCode"
          :placeholder="t('pendingApprovals.filters.documentCodePlaceholder')"
          clearable
          style="width: 160px"
          @keyup.enter="onSearchClick"
        />
        <span class="search-label">{{ t('pendingApprovals.filters.submitter') }}</span>
        <el-input
          v-model="searchForm.submitter"
          :placeholder="t('pendingApprovals.filters.submitterPlaceholder')"
          clearable
          style="width: 140px"
          @keyup.enter="onSearchClick"
        />
        <span class="search-label">{{ t('pendingApprovals.filters.approver') }}</span>
        <el-input
          v-model="searchForm.approver"
          :placeholder="t('pendingApprovals.filters.approverPlaceholder')"
          clearable
          style="width: 140px"
          @keyup.enter="onSearchClick"
        />
        <button type="button" class="btn-primary btn-sm" @click="onSearchClick">
          {{ t('pendingApprovals.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="handleResetFilters">
          {{ t('pendingApprovals.filters.reset') }}
        </button>
      </div>
    </div>

    <div class="segment-row">
      <button class="segment-item" :class="{ 'is-active': activeState === 'pending' }" @click="switchState('pending')">{{ t('pendingApprovals.segment.pending', { count: pendingCount }) }}</button>
      <button class="segment-item" :class="{ 'is-active': activeState === 'approved' }" @click="switchState('approved')">{{ t('pendingApprovals.segment.approved', { count: approvedCount }) }}</button>
      <button class="segment-item" :class="{ 'is-active': activeState === 'rejected' }" @click="switchState('rejected')">{{ t('pendingApprovals.segment.rejected', { count: rejectedCount }) }}</button>
    </div>

    <CrmDataTable
      row-density-storage-key="pending-approvals-list-main"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="approvalList"
      v-loading="loading"
      highlight-current-row
      :default-sort="{ prop: 'createdAt', order: 'descending' }"
      @sort-change="onSortChange"
      @row-dblclick="handleRowDblClick"
    >
      <el-table-column :label="t('pendingApprovals.columns.bizType')" width="140" min-width="140" align="center">
        <template #default="{ row }">
          <el-tag effect="dark" :type="getBizTypeTagType(row.bizType)" size="small">
            {{ row.bizTypeName || getBizTypeText(row.bizType) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column :label="t('pendingApprovals.columns.auditStatus')" width="120" min-width="110" align="center">
        <template #default="{ row }">
          <el-tag
            v-if="row.status != null && row.status !== ''"
            effect="dark"
            :type="getApprovalStatusTagType(row.status)"
            size="small"
          >
            {{ statusText(Number(row.status)) }}
          </el-tag>
          <span v-else class="text-muted">—</span>
        </template>
      </el-table-column>
      <el-table-column prop="documentCode" :label="t('pendingApprovals.columns.documentCode')" width="160" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="code-link" @click="handleView(row)">{{ row.documentCode }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="counterpartyName" :label="t('pendingApprovals.columns.counterparty')" width="200" min-width="200" show-overflow-tooltip>
        <template #default="{ row }">
          <span>{{ displayCounterpartyName(row) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('pendingApprovals.columns.description')" show-overflow-tooltip>
        <template #default="{ row }">
          <span>{{ buildItemDescription(row) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="amount" :label="t('pendingApprovals.columns.amount')" width="160" align="right">
        <template #default="{ row }">
          <template v-if="!listTotalAmountHasValue(row.amount)">
            <span class="dock-tier-empty">—</span>
          </template>
          <div v-else class="dock-tier-price-line">
            <template v-for="amt in [splitListMoneyParts(Number(row.amount))]" :key="'pa-amt-' + row.businessId">
              <span class="dock-tier-amt">
                <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
              </span>
            </template>
            <span class="dock-tier-ccy-gap">&nbsp;</span>
            <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
          </div>
        </template>
      </el-table-column>
      <el-table-column
        prop="createdAt"
        :label="t('pendingApprovals.columns.submittedAt')"
        width="160"
        sortable="custom"
      >
        <template #default="{ row }">
          {{ formatDate(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column prop="submitter" :label="t('pendingApprovals.columns.submitter')" width="100" show-overflow-tooltip>
        <template #default="{ row }">
          {{ row.submitter || '—' }}
        </template>
      </el-table-column>
      <el-table-column
        prop="approvedAt"
        :label="t('pendingApprovals.columns.approvedAt')"
        width="160"
        sortable="custom"
      >
        <template #default="{ row }">
          {{ row.approvedAt ? formatDate(row.approvedAt) : '—' }}
        </template>
      </el-table-column>
      <el-table-column prop="approver" :label="t('pendingApprovals.columns.approver')" width="100" show-overflow-tooltip>
        <template #default="{ row }">
          {{ row.approver || '—' }}
        </template>
      </el-table-column>
      <el-table-column
        :label="t('pendingApprovals.columns.actions')"
        :width="opColWidth"
        :min-width="opColMinWidth"
        fixed="right"
        class-name="op-col"
        label-class-name="op-col"
        :resizable="false"
      >
        <template #header>
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
        <template #default="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <template v-if="activeState === 'pending'">
                <el-button
                  v-if="rowCanDecide(row)"
                  link
                  type="primary"
                  size="small"
                  @click.stop="openAuditDialog(row)"
                >
                  {{ t('pendingApprovals.actions.audit') }}
                </el-button>
                <el-button v-else link type="primary" size="small" @click.stop="openAuditDialog(row)">
                  {{ t('pendingApprovals.actions.viewOnly') }}
                </el-button>
              </template>
              <el-button v-else link type="primary" size="small" @click.stop="handleView(row)">{{ t('pendingApprovals.actions.detail') }}</el-button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <template v-if="activeState === 'pending'">
                    <el-dropdown-item v-if="rowCanDecide(row)" @click.stop="openAuditDialog(row)">
                      <span class="op-more-item op-more-item--primary">{{ t('pendingApprovals.actions.audit') }}</span>
                    </el-dropdown-item>
                    <el-dropdown-item v-else @click.stop="openAuditDialog(row)">
                      <span class="op-more-item op-more-item--primary">{{ t('pendingApprovals.actions.viewOnly') }}</span>
                    </el-dropdown-item>
                  </template>
                  <el-dropdown-item v-else @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('pendingApprovals.actions.detail') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </el-table-column>
    </CrmDataTable>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
      </div>
      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[20, 50, 100]"
        :total="pagination.total"
        layout="total, sizes, prev, pager, next"
        @size-change="handleSearch"
        @current-change="handleSearch"
      />
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { ArrowRight } from '@element-plus/icons-vue'
import { approvalsApi, type BizType, type PendingApprovalItem } from '@/api/approvals'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import {
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  listTotalAmountHasValue,
  splitListMoneyParts
} from '@/utils/moneyFormat'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const router = useRouter()
const { t, te } = useI18n()

const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const loading = ref(false)

const rowCanDecide = (row: PendingApprovalItem) => row.canDecide !== false

const searchForm = ref({
  bizType: '' as '' | BizType,
  documentCode: '',
  submitter: '',
  approver: ''
})
const submittedDateRange = ref<[string, string] | null>(null)

const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

const approvalList = ref<PendingApprovalItem[]>([])
const activeState = ref<'pending' | 'approved' | 'rejected'>('pending')
const pendingCount = ref(0)
const approvedCount = ref(0)
const rejectedCount = ref(0)

/** 表头排序：默认提交时间降序（勿把 default-sort 做成响应式，否则会二次触发 sort-change 打乱结果） */
const sortBy = ref<'submittedAt' | 'approvedAt'>('submittedAt')
const sortDir = ref<'asc' | 'desc'>('desc')

function buildApprovalQueryParams() {
  const range = submittedDateRange.value
  return {
    bizType: searchForm.value.bizType || undefined,
    state: activeState.value,
    submittedFrom: range?.[0] || undefined,
    submittedTo: range?.[1] || undefined,
    documentCode: searchForm.value.documentCode?.trim() || undefined,
    submitter: searchForm.value.submitter?.trim() || undefined,
    approver: searchForm.value.approver?.trim() || undefined,
    sortBy: sortBy.value,
    sortDir: sortDir.value,
    sortAsc: sortDir.value === 'asc',
    page: pagination.value.page,
    pageSize: pagination.value.pageSize
  }
}

function onSortChange(payload: { prop: string; order: string | null }) {
  const prop = String(payload.prop || '')
  const nextSortBy: 'submittedAt' | 'approvedAt' = prop === 'approvedAt' ? 'approvedAt' : 'submittedAt'
  let nextDir: 'asc' | 'desc' = 'desc'
  if (payload.order === 'ascending') nextDir = 'asc'
  else if (payload.order === 'descending') nextDir = 'desc'
  else {
    // 取消排序 → 恢复默认：提交时间降序
    sortBy.value = 'submittedAt'
    sortDir.value = 'desc'
    pagination.value.page = 1
    handleSearch()
    return
  }

  if (sortBy.value === nextSortBy && sortDir.value === nextDir) return
  sortBy.value = nextSortBy
  sortDir.value = nextDir
  pagination.value.page = 1
  handleSearch()
}

/** 兼容后端 PascalCase；无 canDecide 时视为可审批（旧接口） */
function normalizePendingItem(raw: PendingApprovalItem): PendingApprovalItem {
  const legacy = raw as unknown as Record<string, unknown>
  const canDecideRaw = raw.canDecide ?? legacy.CanDecide
  const canDecide = typeof canDecideRaw === 'boolean' ? canDecideRaw : true
  const approver = (raw.approver ?? legacy.Approver) as string | null | undefined
  const approvedAt = (raw.approvedAt ?? legacy.ApprovedAt) as string | null | undefined
  return { ...raw, canDecide, approver: approver ?? null, approvedAt: approvedAt ?? null }
}

function displayCounterpartyName(row: PendingApprovalItem): string {
  const bt = String(row.bizType || '')
  if (maskPurchaseSensitiveFields.value && (bt === 'VENDOR' || bt === 'PURCHASE_ORDER' || bt === 'FINANCE_PAYMENT'))
    return '—'
  if (maskSaleSensitiveFields.value && (bt === 'CUSTOMER' || bt === 'SALES_ORDER' || bt === 'FINANCE_RECEIPT')) return '—'
  return row.counterpartyName || '—'
}

const getBizTypeText = (type: string) => {
  const key = `pendingApprovals.bizType.${type}` as const
  return te(key) ? t(key) : type
}

const getBizTypeTagType = (type: string) => {
  const map: Record<string, string> = {
    VENDOR: 'warning',
    QUOTE: 'primary',
    SALES_ORDER: 'primary',
    PURCHASE_ORDER: 'warning',
    FINANCE_RECEIPT: 'success',
    FINANCE_PAYMENT: 'danger'
  }
  return map[type] || ''
}

/** 与 statusText 规则一致，用于列表「审核状态」标签色 */
const getApprovalStatusTagType = (status: unknown) => {
  const n = Number(status)
  if (!Number.isFinite(n)) return 'info'
  if (n === 2 || n === 1) return 'warning'
  if (n === 10 || n === 20 || n === 3) return 'success'
  if (n < 0 || n === 4 || n === 5) return 'danger'
  return 'info'
}

const formatDate = (dateStr: string) => formatDisplayDateTime(dateStr)

const buildItemDescription = (row: PendingApprovalItem) => {
  const titlePart = (row.title || '').trim()
  const bt = String(row.bizType || '')
  const cpRaw = (row.counterpartyName || '').trim()
  const cpRedactedPurchase =
    maskPurchaseSensitiveFields.value && (bt === 'VENDOR' || bt === 'PURCHASE_ORDER' || bt === 'FINANCE_PAYMENT')
  const cpRedactedSale =
    maskSaleSensitiveFields.value && (bt === 'CUSTOMER' || bt === 'SALES_ORDER' || bt === 'FINANCE_RECEIPT')
  const cp = cpRedactedPurchase || cpRedactedSale ? '' : cpRaw
  const join = t('pendingApprovals.descJoin')
  if (titlePart && cp && titlePart !== cp) return `${titlePart}${join}${cp}`
  if (titlePart) return titlePart
  if (cp) return cp
  return row.documentCode || '—'
}

const is404Error = (e: unknown) => {
  const msg = e instanceof Error ? e.message : String(e ?? '')
  return /404/.test(msg) || /not\s*found/i.test(msg)
}

const loadApprovalItemsCompat = async () => {
  const params = buildApprovalQueryParams()
  try {
    return await approvalsApi.getApprovalItems(params)
  } catch (e) {
    // 兼容旧后端：仅提供 /pending 接口
    if (!is404Error(e)) throw e
    if (activeState.value !== 'pending') {
      return {
        items: [] as PendingApprovalItem[],
        total: 0,
        page: pagination.value.page,
        pageSize: pagination.value.pageSize
      }
    }
    const { state: _state, ...pendingParams } = params
    return await approvalsApi.getPendingApprovals(pendingParams)
  }
}

const onSearchClick = () => {
  pagination.value.page = 1
  handleSearch()
}

const handleResetFilters = () => {
  searchForm.value = {
    bizType: '',
    documentCode: '',
    submitter: '',
    approver: ''
  }
  submittedDateRange.value = null
  pagination.value.page = 1
  handleSearch()
}

const loadApprovalSummaryCompat = async () => {
  try {
    return await approvalsApi.getApprovalSummary({
      bizType: searchForm.value.bizType || undefined
    })
  } catch (e) {
    if (!is404Error(e)) throw e
    // 旧后端没有 summary：降级展示
    return {
      pendingCount: activeState.value === 'pending' ? Number(pagination.value.total || 0) : 0,
      approvedCount: 0,
      rejectedCount: 0
    }
  }
}

const handleSearch = async () => {
  loading.value = true
  try {
    const [res, summary] = await Promise.all([
      loadApprovalItemsCompat(),
      loadApprovalSummaryCompat()
    ])
    const rawList = (res as { items?: PendingApprovalItem[]; Items?: PendingApprovalItem[] }).items
      ?? (res as { Items?: PendingApprovalItem[] }).Items
      ?? []
    approvalList.value = rawList.map(normalizePendingItem)
    const rawTotal = (res as { total?: number; Total?: number }).total
      ?? (res as { Total?: number }).Total
    pagination.value.total = Number(rawTotal ?? 0)
    // 兼容模式下，pendingCount 可用当前分页总数兜底
    const fallbackPending = activeState.value === 'pending' ? Number(pagination.value.total || 0) : 0
    pendingCount.value = Number(summary.pendingCount ?? 0)
    approvedCount.value = Number(summary.approvedCount ?? 0)
    rejectedCount.value = Number(summary.rejectedCount ?? 0)
    if (!pendingCount.value && fallbackPending > 0) pendingCount.value = fallbackPending
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('pendingApprovals.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

const switchState = (state: 'pending' | 'approved' | 'rejected') => {
  if (activeState.value === state) return
  activeState.value = state
  pagination.value.page = 1
  handleSearch()
}

const getDetailRoute = (row: PendingApprovalItem) => {
  const id = row.businessId
  switch (row.bizType) {
    case 'SALES_ORDER':
      return { name: 'SalesOrderDetail', params: { id } }
    case 'VENDOR':
      return { name: 'VendorDetail', params: { id } }
    case 'CUSTOMER':
      return { name: 'CustomerDetail', params: { id } }
    case 'FINANCE_RECEIPT':
      return { name: 'FinanceReceiptDetail', params: { id } }
    case 'FINANCE_PAYMENT':
      return { name: 'FinancePaymentDetail', params: { id } }
    case 'PURCHASE_ORDER':
      return { name: 'PurchaseOrderDetail', params: { id } }
    default:
      return null
  }
}

const handleView = (row: PendingApprovalItem) => {
  const route = getDetailRoute(row)
  if (!route) {
    ElMessage.warning(t('pendingApprovals.messages.jumpNotSupported'))
    return
  }
  router.push(route)
}

/** 待审「审核 / 仅查看」：进入审批桌面并定位该条 */
const openAuditDialog = (row: PendingApprovalItem) => {
  router.push({
    name: 'ApprovalDesktop',
    query: {
      bizType: row.bizType,
      businessId: row.businessId
    }
  })
}

/** 待处理：双击进入审批桌面并开始审该条；已通过/已拒绝：打开业务详情 */
const handleRowDblClick = (row: PendingApprovalItem) => {
  if (activeState.value === 'pending') {
    openAuditDialog(row)
    return
  }
  handleView(row)
}

const statusText = (status: number) => {
  if (status === 2 || status === 1) return t('pendingApprovals.rowStatus.pending')
  if (status === 10 || status === 20 || status === 3) return t('pendingApprovals.rowStatus.passed')
  if (status < 0 || status === 4 || status === 5) return t('pendingApprovals.rowStatus.rejected')
  return String(status)
}

const openApprovalDesktop = () => {
  router.push({ name: 'ApprovalDesktop' })
}

onMounted(() => {
  handleSearch()
})
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.pending-approvals-page {
  padding: 20px 24px;
  min-height: 100%;
}

.btn-approval-desktop {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px 8px 18px;
  border: none;
  border-radius: 10px;
  background: #eaf5ff;
  color: #1a2332;
  font-size: 13px;
  font-weight: 500;
  font-family: 'Noto Sans SC', sans-serif;
  line-height: 1.2;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;

  &:hover {
    background: #ddefff;
    color: #0f172a;
  }

  &:active {
    background: #d0e8ff;
  }

  &__arrow {
    font-size: 14px;
  }
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 14px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  letter-spacing: 0.3px;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
  margin-bottom: 14px;
}

.stat-card {
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: 10px;
  padding: 16px 18px;
}

.stat-label {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 30px;
  line-height: 1;
  font-weight: 700;
  font-family: 'Noto Sans SC', sans-serif;
}

.stat-card--pending .stat-value {
  color: $warning-color;
}
.stat-card--approved .stat-value {
  color: $success-color;
}
.stat-card--rejected .stat-value {
  color: $danger-color;
}

.search-bar {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding: 14px 18px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
}

.search-left {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px 12px;
  min-width: 0;
  flex: 1;
}

.filter-date-range {
  width: 260px;
}

.segment-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
}

.segment-item {
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.08);
  color: $text-secondary;
  border-radius: 999px;
  padding: 3px 10px;
  font-size: 12px;
  cursor: pointer;
}

.segment-item.is-active {
  color: $cyan-primary;
  border-color: rgba(0, 212, 255, 0.45);
}

.segment-item:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.75), rgba(0, 212, 255, 0.65));
  border: 1px solid rgba(0, 212, 255, 0.35);
  border-radius: 8px;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;

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
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &.btn-sm {
    padding: 6px 12px;
  }
}

.search-label {
  font-size: 13px;
  color: $text-secondary;
  white-space: nowrap;
  margin-right: 8px;
}

.code-link {
  color: $cyan-primary;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  font-family: 'Noto Sans SC', sans-serif;
  transition: color 0.15s;

  &:hover {
    color: lighten(#00d4ff, 10%);
    text-decoration: underline;
  }
}

.text-muted {
  color: $text-muted;
}

.action-btns {
  display: flex;
  gap: 4px;
  white-space: nowrap;
}

.list-footer-left {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.pagination-wrapper {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 18px;
  border-top: 1px solid rgba(255, 255, 255, 0.04);

  :deep(.el-pagination) {
    --el-pagination-bg-color: transparent;
    --el-pagination-button-bg-color: transparent;
    --el-pagination-hover-color: #{$cyan-primary};
    color: $text-secondary;

    .el-pagination__total,
    .el-pagination__sizes {
      color: $text-muted;
    }

    .el-pager li {
      background: transparent;
      color: $text-secondary;

      &.is-active {
        color: $cyan-primary;
        font-weight: 600;
      }

      &:hover {
        color: $cyan-primary;
      }
    }

    button {
      background: transparent;
      color: $text-secondary;

      &:hover {
        color: $cyan-primary;
      }

      &:disabled {
        color: $text-muted;
      }
    }
  }
}

</style>
