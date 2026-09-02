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
      <div v-if="canWritePurchaseData" class="header-right">
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
    <el-row v-if="viewMode === 'list'" :gutter="20" class="stat-row">
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
          <div class="stat-value">{{ statAmountDisplay }}</div>
          <div class="stat-label">{{ t('purchaseOrderList.stats.totalAmount') }}</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏：状态 → 类型 → 采购订单号 → 供应商 → 采购员 → 货代单号 → 备注 → 创建日期 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filterForm.status"
          :placeholder="t('purchaseOrderList.filters.allStatus')"
          multiple
          collapse-tags
          collapse-tags-tooltip
          clearable
          class="status-select status-select--po filter-select--progress"
          popper-class="progress-multi-select-dropdown"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option
            v-for="opt in statusFilterOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          >
            <ProgressMultiSelectOption
              :label="opt.label"
              :checked="filterForm.status.includes(opt.value)"
            />
          </el-option>
        </el-select>
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
        <template v-if="canViewPurchaseUser">
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
              v-model="filterForm.purchaseUserName"
              class="search-input"
              :placeholder="t('purchaseOrderList.filters.purchaserPlaceholder')"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
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
            v-model="filterForm.freightForwarderOrderNo"
            class="search-input"
            :placeholder="t('purchaseOrderList.filters.freightForwarderOrderNoPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
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
            v-model="filterForm.comment"
            class="search-input"
            :placeholder="t('purchaseOrderList.filters.commentPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-date-picker
          v-model="filterForm.createDateRange"
          type="daterange"
          :range-separator="t('purchaseOrderList.filters.createDateSep')"
          :start-placeholder="t('purchaseOrderList.filters.createDateFrom')"
          :end-placeholder="t('purchaseOrderList.filters.createDateTo')"
          value-format="YYYY-MM-DD"
          class="filter-date-range po-list-date-range"
          clearable
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('purchaseOrderList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('purchaseOrderList.filters.reset') }}</button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('purchaseOrderList.filters.listView') : t('purchaseOrderList.filters.boardView') }}
        </button>
      </div>
    </div>

    <PurchaseOrderListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <div v-show="viewMode === 'list'" class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="purchase-order-list-main"
        :columns="purchaseOrderTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="orderList"
        row-key="id"
        highlight-current-row
        :row-class-name="purchaseOrderListRowClassName"
        @row-dblclick="onPurchaseOrderRowDblClick"
        @header-dragend="onPurchaseOrderTableHeaderDragEnd"
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
        <template #col-type="{ row }">
          <el-tag effect="dark" :type="purchaseOrderTypeTagType(row)" size="small">
            {{ purchaseOrderTypeLabel(row) }}
          </el-tag>
        </template>
        <template v-if="canViewVendorInfo" #col-vendor-header>
          <VendorExtendColumnHeader
            :active-field="vendorExtendActiveField"
            @set-active-field="setVendorExtendActiveField"
          />
        </template>
        <template v-if="canViewVendorInfo" #col-vendor="{ row }">
          <VendorExtendCell
            :row="row"
            :active-field="vendorExtendActiveField"
            :masked="maskPurchaseSensitiveFields"
            :empty-text="t('quoteList.na')"
          />
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
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('purchaseOrderList.actions.detail') }}</button>
              <button v-if="canWritePurchaseData" type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">{{ t('purchaseOrderList.actions.edit') }}</button>
              <button
                v-if="canWritePurchaseData && ((poListMainStatus(row) >= 1 && poListMainStatus(row) < 10) || poListMainStatus(row) === -1)"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="submitAudit(row)"
              >
                {{ t('purchaseOrderList.actions.submitAudit') }}
              </button>
              <button
                v-if="canWritePurchaseData && poListMainStatus(row) >= 10 && poListMainStatus(row) < 30"
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
                v-if="canWritePurchaseData && poListMainStatus(row) === 30"
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
                  <el-dropdown-item v-if="canWritePurchaseData" @click.stop="handleEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('purchaseOrderList.actions.edit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="canWritePurchaseData && ((poListMainStatus(row) >= 1 && poListMainStatus(row) < 10) || poListMainStatus(row) === -1)"
                    @click.stop="submitAudit(row)"
                  >
                    <span class="op-more-item op-more-item--warning">{{ t('purchaseOrderList.actions.submitAudit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="canWritePurchaseData && poListMainStatus(row) >= 10 && poListMainStatus(row) < 30"
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
                  <el-dropdown-item v-if="canWritePurchaseData && poListMainStatus(row) === 30" @click.stop="cancelSupplierConfirm(row)">
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
import type { PurchaseOrderListAnalyticsQuery } from '@/api/purchaseOrderAnalytics'
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
import VendorExtendColumnHeader from '@/components/list/VendorExtendColumnHeader.vue'
import VendorExtendCell from '@/components/list/VendorExtendCell.vue'
import { useListBoardHelpOverride } from '@/composables/useHelpDocOverride'
import PurchaseOrderListBoard from './PurchaseOrderListBoard.vue'
import ProgressMultiSelectOption from '@/components/Common/ProgressMultiSelectOption.vue'
import {
  assignPurchaseOrderStatusesParam,
  normalizePurchaseOrderStatuses
} from '@/utils/purchaseOrderStatusQuery'
import { useVendorExtendColumn, isVendorExtendTableColumn } from '@/composables/useVendorExtendColumn'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import { isCancelledOrderHeaderStatus, LIST_ROW_CANCELLED_CLASS } from '@/utils/listCancelledRow'

const router = useRouter()
const { t, locale } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const {
  expanded: vendorExtendExpanded,
  activeField: vendorExtendActiveField,
  colWidth: vendorExtendColWidth,
  colMinWidth: vendorExtendColMinWidth,
  setActiveField: setVendorExtendActiveField,
  applyOuterWidthFromTable: applyVendorExtendOuterWidth
} = useVendorExtendColumn()

function onPurchaseOrderTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isVendorExtendTableColumn(column)) return
  applyVendorExtendOuterWidth(newWidth)
}

const loading = ref(false)
const viewMode = ref<'list' | 'board'>('list')
useListBoardHelpOverride('pages/采购订单看板_MENU_PURCHASE_ORDERS_BOARD.md', viewMode)
const orderList = ref<any[]>([])
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const authStore = useAuthStore()
const { canWritePurchaseData } = useDepartmentDataReadOnly()
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
const canViewPurchaseUser = computed(
  () => authStore.hasPermission('purchase.user.read') || authStore.hasPermission('purchase-order.read')
)

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

const poListMainStatus = normalizePurchaseOrderMainStatus

function purchaseOrderListRowClassName({ row }: { row: Record<string, unknown> }) {
  return isCancelledOrderHeaderStatus(poListMainStatus(row)) ? LIST_ROW_CANCELLED_CLASS : ''
}

function purchaseOrderHeaderType(row: Record<string, unknown>): number {
  const n = Number(row.type ?? row.Type)
  return n >= 1 && n <= 3 ? n : 1
}

function isPurchaseOrderStocking(row: Record<string, unknown>) {
  return purchaseOrderHeaderType(row) === 2
}

function purchaseOrderTypeLabel(row: Record<string, unknown>) {
  const n = purchaseOrderHeaderType(row)
  if (n === 2) return t('purchaseOrderList.filters.orderTypeStocking')
  if (n === 3) return t('purchaseOrderList.filters.orderTypeSample')
  return t('purchaseOrderList.filters.orderTypeCustomer')
}

function purchaseOrderTypeTagType(row: Record<string, unknown>): '' | 'success' | 'warning' | 'danger' | 'info' {
  const n = purchaseOrderHeaderType(row)
  if (n === 2) return 'warning'
  if (n === 3) return 'info'
  return ''
}

/** 采购订单列表主表可配置列（localStorage：crm-table-columns:v1:purchase-order-list-main） */
const purchaseOrderTableColumns = computed((): CrmTableColumnDef[] => {
  void vendorExtendExpanded.value
  void vendorExtendColWidth.value
  return [
  { key: 'status', label: t('purchaseOrderList.columns.status'), prop: 'status', width: 160, align: 'center' as const },
  {
    key: 'type',
    label: t('purchaseOrderList.columns.orderType'),
    prop: 'type',
    width: 110,
    minWidth: 100,
    align: 'center' as const,
    showOverflowTooltip: true
  },
  ...(canViewVendorInfo.value
    ? [{
        key: 'vendor',
        label: t('common.vendorExtendCol.columnTitle'),
        prop: 'vendor',
        minWidth: vendorExtendColMinWidth.value,
        width: vendorExtendColWidth.value,
        showOverflowTooltip: true,
        className: 'vendor-extend-col',
        labelClassName: 'vendor-extend-col'
      }]
    : []),
  { key: 'purchaseUserName', label: t('purchaseOrderList.columns.purchaser'), prop: 'purchaseUserName', width: 100 },
  ...(canViewPurchaseAmount.value
    ? [{ key: 'total', label: t('purchaseOrderList.columns.totalAmount'), prop: 'total', width: 160, align: 'right' as const }]
    : []),
  {
    key: 'itemRows',
    label: t('purchaseOrderList.columns.itemRows'),
    prop: 'itemRows',
    width: 120,
    minWidth: 120,
    align: 'center' as const
  },
  { key: 'deliveryDate', label: t('purchaseOrderList.columns.deliveryDate'), prop: 'deliveryDate', width: 160 },
  {
    key: 'freightForwarderOrderNo',
    label: t('purchaseOrderList.columns.freightForwarderOrderNo'),
    prop: 'freightForwarderOrderNo',
    width: 160,
    minWidth: 140,
    showOverflowTooltip: true
  },
  {
    key: 'comment',
    label: t('purchaseOrderList.columns.comment'),
    prop: 'comment',
    minWidth: 160,
    showOverflowTooltip: true
  },
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
    labelClassName: 'op-col',
    resizable: false
  }
  ]
})

// 筛选表单
const filterForm = ref({
  code: '',
  freightForwarderOrderNo: '',
  vendor: '',
  purchaseUserName: '',
  comment: '',
  createDateRange: null as [string, string] | null,
  status: [] as number[],
  orderType: undefined as number | undefined
})

const statusFilterOptions = computed(() => {
  void locale.value
  return [
    { label: t('purchaseOrderList.status.draft'), value: 0 },
    { label: t('purchaseOrderList.status.new'), value: 1 },
    { label: t('purchaseOrderList.status.pendingReview'), value: 2 },
    { label: t('purchaseOrderList.status.approved'), value: 10 },
    { label: t('purchaseOrderList.status.pendingConfirm'), value: 20 },
    { label: t('purchaseOrderList.status.confirmed'), value: 30 },
    { label: t('purchaseOrderList.status.inProgress'), value: 50 },
    { label: t('purchaseOrderList.status.completed'), value: 100 },
    { label: t('purchaseOrderList.status.reviewFailed'), value: -1 },
    { label: t('purchaseOrderList.status.cancelled'), value: -2 }
  ]
})

const boardFilters = computed((): PurchaseOrderListAnalyticsQuery => {
  const q: PurchaseOrderListAnalyticsQuery = {}
  const c = filterForm.value.code.trim()
  if (c) q.code = c
  if (canViewVendorInfo.value) {
    const vendor = filterForm.value.vendor.trim()
    if (vendor) q.vendor = vendor
  }
  const ff = filterForm.value.freightForwarderOrderNo.trim()
  if (ff) q.freightForwarderOrderNo = ff
  if (canViewPurchaseUser.value) {
    const pu = filterForm.value.purchaseUserName.trim()
    if (pu) q.purchaseUserName = pu
  }
  const cm = filterForm.value.comment.trim()
  if (cm) q.comment = cm
  const statuses = normalizePurchaseOrderStatuses(filterForm.value.status)
  if (statuses.length) q.status = statuses
  if (filterForm.value.orderType !== undefined && filterForm.value.orderType !== null) {
    q.orderType = filterForm.value.orderType
  }
  if (filterForm.value.createDateRange?.[0]) q.startDate = filterForm.value.createDateRange[0]
  if (filterForm.value.createDateRange?.[1]) q.endDate = filterForm.value.createDateRange[1]
  return q
})

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
}

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

/** 与当前筛选一致的全量汇总（后端 aggregates） */
const listAggregates = ref({
  totalCount: 0,
  pendingConfirmCount: 0,
  inProgressCount: 0,
  totalAmountSum: null as number | null
})

// 统计（全筛选范围，非仅当前页）
const statTotal = computed(() => listAggregates.value.totalCount)
const statPending = computed(() => listAggregates.value.pendingConfirmCount)
const statInProgress = computed(() => listAggregates.value.inProgressCount)
const statAmountDisplay = computed(() => {
  if (!canViewPurchaseAmount.value) return '--'
  if (listAggregates.value.totalAmountSum == null) return '--'
  const n = Number(listAggregates.value.totalAmountSum)
  if (!Number.isFinite(n)) return '--'
  return `$${n.toLocaleString()}`
})

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
    const params: Record<string, unknown> = {
      page: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize
    }
    const c = filterForm.value.code?.trim()
    const ff = filterForm.value.freightForwarderOrderNo?.trim()
    const v = filterForm.value.vendor?.trim()
    const pu = filterForm.value.purchaseUserName?.trim()
    const cm = filterForm.value.comment?.trim()
    if (c) params.code = c
    if (ff) params.freightForwarderOrderNo = ff
    if (v) params.vendor = v
    if (canViewPurchaseUser.value && pu) params.purchaseUserName = pu
    if (cm) params.comment = cm
    if (filterForm.value.createDateRange?.[0]) params.startDate = filterForm.value.createDateRange[0]
    if (filterForm.value.createDateRange?.[1]) params.endDate = filterForm.value.createDateRange[1]
    assignPurchaseOrderStatusesParam(params, 'status', filterForm.value.status)
    if (filterForm.value.orderType !== undefined) params.orderType = filterForm.value.orderType

    const res = (await purchaseOrderApi.getList(params)) as {
      items?: unknown[]
      total?: number
      aggregates?: {
        totalCount?: number
        pendingConfirmCount?: number
        inProgressCount?: number
        totalAmountSum?: number | null
      }
    }
    const items = (res.items || []) as any[]
    const total = res.total ?? 0
    if (pageInfo.value.page > 1 && items.length === 0 && total > 0) {
      pageInfo.value.page = 1
      await loadData()
      return
    }
    orderList.value = items
    pageInfo.value.total = total
    const agg = res.aggregates
    if (agg) {
      listAggregates.value = {
        totalCount: agg.totalCount ?? total,
        pendingConfirmCount: agg.pendingConfirmCount ?? 0,
        inProgressCount: agg.inProgressCount ?? 0,
        totalAmountSum: agg.totalAmountSum ?? null
      }
    } else {
      listAggregates.value = {
        totalCount: total,
        pendingConfirmCount: 0,
        inProgressCount: 0,
        totalAmountSum: null
      }
    }
  } catch (error) {
    ElMessage.error(t('purchaseOrderList.loadFailed'))
  } finally {
    loading.value = false
  }
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
  void loadData()
}

const handleReset = () => {
  filterForm.value = {
    code: '',
    freightForwarderOrderNo: '',
    vendor: '',
    purchaseUserName: '',
    comment: '',
    createDateRange: null,
    status: [],
    orderType: undefined
  }
  pageInfo.value.page = 1
  void loadData()
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
  pageInfo.value.page = 1
  void loadData()
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
  void loadData()
}

// 新建：直接进入创建页，默认备货采购（Type=2），无销售/申请链路
const handleCreate = () => {
  router.push({ name: 'PurchaseOrderCreate', query: { type: '2' } })
}

// 编辑（须走 PurchaseOrderEdit，组件内以 route.name + params.id 识别编辑模式）
const handleEdit = (row: any) => {
  router.push({ name: 'PurchaseOrderEdit', params: { id: row.id } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.id } })
}

function onPurchaseOrderRowDblClick(row: { id?: string }, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: canWritePurchaseData.value,
    onEdit: handleEdit,
    onDefault: handleView,
  })
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

/** 取消确认：仅「已确认(30)」时显示；退回待确认(20)，不是整单取消 */
const cancelSupplierConfirm = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      t('purchaseOrderList.cancelConfirmMessage', { code: row.purchaseOrderCode }),
      t('purchaseOrderList.actions.cancelConfirm'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
    await purchaseOrderApi.updateStatus(row.id, 20)
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
  width: 168px;
}

.status-select--po-type {
  width: 140px;
}

.filter-date-range.po-list-date-range {
  width: 260px;
  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
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

.btn-board-active {
  border-color: rgba(0, 212, 255, 0.45);
  color: #00d4ff;
  background: rgba(0, 212, 255, 0.08);
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
