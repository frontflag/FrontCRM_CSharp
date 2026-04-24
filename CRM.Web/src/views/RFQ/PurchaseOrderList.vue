<template>
  <div class="purchase-order-list-page">
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
          <h1 class="page-title">{{ t('purchaseOrderList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('purchaseOrderList.count', { count: pageInfo.total }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="loadData">{{ t('purchaseOrderList.filters.refresh') }}</button>
        <button type="button" class="btn-success" @click="handleCreate">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          {{ t('purchaseOrderList.create') }}
        </button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ statTotal }}</div>
          <div class="stat-label">{{ t('purchaseOrderList.stats.total') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ statPending }}</div>
          <div class="stat-label">{{ t('purchaseOrderList.stats.pendingConfirm') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ statInProgress }}</div>
          <div class="stat-label">{{ t('purchaseOrderList.stats.inProgress') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-info">
          <div class="stat-value">{{ canViewPurchaseAmount ? `¥${statAmount.toLocaleString()}` : '--' }}</div>
          <div class="stat-label">{{ t('purchaseOrderList.stats.totalAmount') }}</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏（与客户列表页 search-bar 一致） -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('purchaseOrderList.filters.orderType') }}</span>
        <el-select
          v-model="filterForm.orderType"
          :placeholder="t('purchaseOrderList.filters.allOrderTypes')"
          clearable
          class="status-select status-select--po-type"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option :label="t('purchaseOrderList.filters.orderTypeCustomer')" :value="1" />
          <el-option :label="t('purchaseOrderList.filters.orderTypeStocking')" :value="2" />
          <el-option :label="t('purchaseOrderList.filters.orderTypeSample')" :value="3" />
        </el-select>
        <span class="filter-field-label">{{ t('purchaseOrderList.filters.orderCode') }}</span>
        <div class="search-input-wrap">
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            class="search-icon"
            aria-hidden="true"
          >
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.code"
            class="search-input"
            :placeholder="t('purchaseOrderList.filters.orderCodePlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <template v-if="canViewVendorInfo">
          <span class="filter-field-label">{{ t('purchaseOrderList.filters.vendor') }}</span>
          <div class="search-input-wrap">
            <svg
              width="14"
              height="14"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              class="search-icon"
              aria-hidden="true"
            >
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filterForm.vendor"
              class="search-input"
              :placeholder="t('purchaseOrderList.filters.vendorPlaceholder')"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
        <span class="filter-field-label">{{ t('purchaseOrderList.filters.status') }}</span>
        <el-select
          v-model="filterForm.status"
          :placeholder="t('purchaseOrderList.filters.allStatus')"
          clearable
          class="status-select status-select--po"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option :label="t('purchaseOrderList.status.draft')" :value="0" />
          <el-option :label="t('purchaseOrderList.status.new')" :value="1" />
          <el-option :label="t('purchaseOrderList.status.pendingReview')" :value="2" />
          <el-option :label="t('purchaseOrderList.status.approved')" :value="10" />
          <el-option :label="t('purchaseOrderList.status.pendingConfirm')" :value="20" />
          <el-option :label="t('purchaseOrderList.status.confirmed')" :value="30" />
          <el-option :label="t('purchaseOrderList.status.inProgress')" :value="50" />
          <el-option :label="t('purchaseOrderList.status.completed')" :value="100" />
          <el-option :label="t('purchaseOrderList.status.reviewFailed')" :value="-1" />
          <el-option :label="t('purchaseOrderList.status.cancelled')" :value="-2" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('purchaseOrderList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('purchaseOrderList.filters.reset') }}</button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="purchase-order-list-main"
        :columns="purchaseOrderTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="filteredList"
        row-key="id"
        highlight-current-row
        @row-dblclick="handleView"
      >
        <template #col-purchaseOrderCode="{ row }">
          <span class="po-code-with-badge">
            <el-link type="primary" @click="handleView(row)">{{ row.purchaseOrderCode }}</el-link>
            <el-tooltip
              v-if="isPurchaseOrderStocking(row)"
              :content="t('purchaseOrderList.filters.orderTypeStocking')"
              placement="top"
            >
              <el-tag type="warning" effect="plain" size="small" class="po-stocking-tag" round>
                {{ t('purchaseOrderList.filters.stockingTag') }}
              </el-tag>
            </el-tooltip>
          </span>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(poListMainStatus(row))" size="small">
            {{ getStatusText(poListMainStatus(row)) }}
          </el-tag>
        </template>
        <template #col-total="{ row }">
          <template v-if="!listTotalAmountHasValue(row.total)">
            <span class="dock-tier-empty">—</span>
          </template>
          <div v-else class="dock-tier-price-line">
            <template v-for="amt in [splitListMoneyParts(Number(row.total))]" :key="'po-total-' + row.id">
              <span class="dock-tier-amt">
                <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
              </span>
            </template>
            <span class="dock-tier-ccy-gap">&nbsp;</span>
            <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
          </div>
        </template>
        <template #col-deliveryDate="{ row }">
          {{ formatDisplayDate(row.deliveryDate) }}
        </template>
        <template #col-createTime="{ row }">
          {{ formatDisplayDateTime(row.createTime) }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || row.purchaseUserName || '—' }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('purchaseOrderList.columns.actions') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('purchaseOrderList.actions.detail') }}</button>
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">{{ t('purchaseOrderList.actions.edit') }}</button>
              <button
                v-if="(poListMainStatus(row) >= 1 && poListMainStatus(row) < 10) || poListMainStatus(row) === -1"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="submitAudit(row)"
              >
                {{ t('purchaseOrderList.actions.submitAudit') }}
              </button>
              <button
                v-if="poListMainStatus(row) >= 10 && poListMainStatus(row) < 30"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="confirmBySupplier(row)"
              >
                {{ t('purchaseOrderList.actions.confirmBySupplier') }}
              </button>
              <button
                v-if="purchaseOrderReportAllowed(poListMainStatus(row))"
                type="button"
                class="action-btn action-btn--primary"
                @click.stop="handlePrintOrder(row)"
              >
                {{ t('purchaseOrderList.actions.report') }}
              </button>
              <button
                v-if="poListMainStatus(row) === 30"
                type="button"
                class="action-btn action-btn--danger"
                @click.stop="cancelSupplierConfirm(row)"
              >
                {{ t('purchaseOrderList.actions.cancelConfirm') }}
              </button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('purchaseOrderList.actions.detail') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="handleEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('purchaseOrderList.actions.edit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="(poListMainStatus(row) >= 1 && poListMainStatus(row) < 10) || poListMainStatus(row) === -1"
                    @click.stop="submitAudit(row)"
                  >
                    <span class="op-more-item op-more-item--warning">{{ t('purchaseOrderList.actions.submitAudit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="poListMainStatus(row) >= 10 && poListMainStatus(row) < 30"
                    @click.stop="confirmBySupplier(row)"
                  >
                    <span class="op-more-item op-more-item--warning">{{ t('purchaseOrderList.actions.confirmBySupplier') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="purchaseOrderReportAllowed(poListMainStatus(row))"
                    @click.stop="handlePrintOrder(row)"
                  >
                    <span class="op-more-item op-more-item--primary">{{ t('purchaseOrderList.actions.report') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="poListMainStatus(row) === 30" @click.stop="cancelSupplierConfirm(row)">
                    <span class="op-more-item op-more-item--danger">{{ t('purchaseOrderList.actions.cancelConfirm') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <!-- 底栏：列设置（图标+Tip+Spacer） + 分页（顶对齐） -->
      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
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
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  listTotalAmountHasValue,
  splitListMoneyParts
} from '@/utils/moneyFormat'
import {
  purchaseOrderReportAllowed,
  normalizePurchaseOrderMainStatus
} from '@/constants/purchaseOrderStatus'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'

const router = useRouter()
const { t } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()

const loading = ref(false)
const orderList = ref<any[]>([])
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const authStore = useAuthStore()
/** 与后端 PurchaseOrdersController.MaskPurchaseOrder 中 canViewVendorInfo 一致（非仅 vendor.info.read） */
const canViewVendorInfo = computed(
  () =>
    !maskPurchaseSensitiveFields.value &&
    (authStore.hasPermission('vendor.info.read') ||
      authStore.hasPermission('vendor.read') ||
      authStore.hasPermission('purchase-order.read') ||
      authStore.hasPermission('purchase-order.write'))
)
const canViewPurchaseAmount = computed(
  () => !maskPurchaseSensitiveFields.value && authStore.hasPermission('purchase.amount.read')
)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 300
const OP_COL_EXPANDED_MIN_WIDTH = 292
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const poListMainStatus = normalizePurchaseOrderMainStatus

function purchaseOrderHeaderType(row: Record<string, unknown>): number {
  const n = Number(row.type ?? row.Type)
  return n >= 1 && n <= 3 ? n : 1
}

function isPurchaseOrderStocking(row: Record<string, unknown>) {
  return purchaseOrderHeaderType(row) === 2
}

/** 采购订单列表主表可配置列（localStorage：crm-table-columns:v1:purchase-order-list-main） */
const purchaseOrderTableColumns = computed((): CrmTableColumnDef[] => [
  { key: 'status', label: t('purchaseOrderList.columns.status'), prop: 'status', width: 160, align: 'center' as const },
  ...(canViewVendorInfo.value
    ? [{ key: 'vendorName', label: t('purchaseOrderList.columns.vendor'), prop: 'vendorName', minWidth: 200, showOverflowTooltip: true }]
    : []),
  { key: 'purchaseUserName', label: t('purchaseOrderList.columns.purchaser'), prop: 'purchaseUserName', width: 100 },
  ...(canViewPurchaseAmount.value
    ? [{ key: 'total', label: t('purchaseOrderList.columns.totalAmount'), prop: 'total', width: 160, align: 'right' as const }]
    : []),
  { key: 'itemRows', label: t('purchaseOrderList.columns.itemRows'), prop: 'itemRows', width: 80, align: 'center' as const },
  { key: 'deliveryDate', label: t('purchaseOrderList.columns.deliveryDate'), prop: 'deliveryDate', width: 160 },
  {
    key: 'purchaseOrderCode',
    label: t('purchaseOrderList.columns.orderCode'),
    prop: 'purchaseOrderCode',
    width: 160,
    minWidth: 160,
    showOverflowTooltip: true,
    sortable: true
  },
  { key: 'createTime', label: t('purchaseOrderList.columns.createTime'), prop: 'createTime', width: 160 },
  { key: 'createUser', label: t('purchaseOrderList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('purchaseOrderList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
])

// 筛选表单
const filterForm = ref({
  code: '',
  vendor: '',
  status: undefined as number | undefined,
  orderType: undefined as number | undefined
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
    result = result.filter(o => poListMainStatus(o) === filterForm.value.status)
  }
  if (filterForm.value.orderType !== undefined) {
    const ot = filterForm.value.orderType
    result = result.filter(o => purchaseOrderHeaderType(o as Record<string, unknown>) === ot)
  }
  pageInfo.value.total = result.length
  const start = (pageInfo.value.page - 1) * pageInfo.value.pageSize
  return result.slice(start, start + pageInfo.value.pageSize)
})

// 统计
const statTotal = computed(() => orderList.value.length)
const statPending = computed(() => orderList.value.filter(o => poListMainStatus(o) === 20).length)
const statInProgress = computed(() => orderList.value.filter(o => poListMainStatus(o) === 50).length)
const statAmount = computed(() => orderList.value.reduce((sum, o) => sum + (o.total || 0), 0))

// 状态处理
const getStatusType = (status: number) => {
  if (!Number.isFinite(status)) return 'info'
  const map: Record<number, string> = {
    0: 'info',
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
  if (!Number.isFinite(status)) return t('quoteList.na')
  const map: Record<number, string> = {
    0: t('purchaseOrderList.status.draft'),
    1: t('purchaseOrderList.status.new'),
    2: t('purchaseOrderList.status.pendingReview'),
    10: t('purchaseOrderList.status.approved'),
    20: t('purchaseOrderList.status.pendingConfirm'),
    30: t('purchaseOrderList.status.confirmed'),
    50: t('purchaseOrderList.status.inProgress'),
    100: t('purchaseOrderList.status.completed'),
    '-1': t('purchaseOrderList.status.reviewFailed'),
    '-2': t('purchaseOrderList.status.cancelled')
  }
  return map[status] || t('rfqDetail.unknown')
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await purchaseOrderApi.getList({ page: 1, pageSize: 2000 })
    orderList.value = (res as { items?: unknown[] }).items || []
    pageInfo.value.total = orderList.value.length
  } catch (error) {
    ElMessage.error(t('purchaseOrderList.loadFailed'))
  } finally {
    loading.value = false
  }
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
}

const handleReset = () => {
  filterForm.value = { code: '', vendor: '', status: undefined, orderType: undefined }
  pageInfo.value.page = 1
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
}

// 新建：直接进入创建页，默认备货采购（Type=2），无销售/申请链路
const handleCreate = () => {
  router.push({ name: 'PurchaseOrderCreate', query: { type: '2' } })
}

// 编辑
const handleEdit = (row: any) => {
  router.push({ name: 'PurchaseOrderCreate', query: { id: row.id, edit: '1' } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.id } })
}

const handlePrintOrder = (row: any) => {
  if (!purchaseOrderReportAllowed(poListMainStatus(row))) {
    ElMessage.warning(t('purchaseOrderList.reportNotAllowed'))
    return
  }
  router.push({ name: 'PurchaseOrderReport', params: { id: row.id } })
}

/** 供应商确认：待确认(20) -> 已确认(30) */
const confirmBySupplier = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      t('purchaseOrderList.confirmBySupplierConfirm', { code: row.purchaseOrderCode }),
      t('purchaseOrderList.actions.confirmBySupplier'),
      { type: 'info', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
    // 允许从“审核通过(10)”推进到“待确认(20)”再到“已确认(30)”
    if (poListMainStatus(row) === 10) {
      await purchaseOrderApi.updateStatus(row.id, 20)
    }
    await purchaseOrderApi.updateStatus(row.id, 30)
    ElMessage.success(t('purchaseOrderList.confirmBySupplierSuccess'))
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

/** 取消确认：仅「已确认(30)」时显示 */
const cancelSupplierConfirm = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      t('purchaseOrderList.cancelConfirmMessage', { code: row.purchaseOrderCode }),
      t('purchaseOrderList.actions.cancelConfirm'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
    await purchaseOrderApi.updateStatus(row.id, -2)
    ElMessage.success(t('purchaseOrderList.cancelConfirmSuccess'))
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

/** 提交审核 */
const submitAudit = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      t('purchaseOrderList.submitAuditConfirm', { code: row.purchaseOrderCode }),
      t('purchaseOrderList.actions.submitAudit'),
      { type: 'info', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
    // 审核失败(-1)先回到新建(1)，再提交审核(2)
    if (poListMainStatus(row) === -1) {
      await purchaseOrderApi.updateStatus(row.id, 1)
    }
    await purchaseOrderApi.updateStatus(row.id, 2)
    ElMessage.success(t('purchaseOrderList.submitAuditSuccess'))
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.purchase-order-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
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
    flex-wrap: wrap;
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
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
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

// ---- 搜索栏（与客户列表一致）----
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
  outline: none;
  transition: border-color 0.2s;
  box-sizing: border-box;

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.search-input--plain {
  padding: 7px 12px;
  width: 200px;
}

.status-select {
  width: 120px;
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

.status-select--po {
  width: 150px;
}

.status-select--po-type {
  width: 140px;
}

.po-code-with-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.po-stocking-tag {
  flex-shrink: 0;
  cursor: default;
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

/* 列表页「新建/新增」：UI 规范 success 绿（见 列表操作按钮颜色规范PRD） */
.btn-success {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.85), rgba(70, 191, 145, 0.75));
  border: 1px solid rgba(70, 191, 145, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
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
  cursor: pointer;
  transition: all 0.2s;

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}

.table-wrapper :deep(.el-table) {
  .el-table__cell.op-col .cell {
    display: inline-block;
    width: max-content;
    max-width: 100%;
  }
  .el-table__cell .cell {
    white-space: nowrap;
  }
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

// 列表操作列规范（收起/展开）
.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--warning {
  color: $color-amber;
}

.op-more-item--danger {
  color: $color-red-brown;
}

.op-more-item--success {
  color: $color-mint-green;
}

.op-more-item--info {
  color: rgba(200, 216, 232, 0.85);
}
</style>
