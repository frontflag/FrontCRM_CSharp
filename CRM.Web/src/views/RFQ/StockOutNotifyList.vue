<template>
  <div class="stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 5h18M3 12h18M3 19h18" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutNotifyList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('stockOutNotifyList.count', { count: listTotal }) }}</div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filterForm.status"
          :placeholder="t('stockOutNotifyList.filters.statusPlaceholder')"
          clearable
          class="status-select"
          :teleported="false"
        >
          <el-option
            v-for="opt in statusFilterOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select
          v-model="filterForm.regionType"
          :placeholder="t('stockOutNotifyList.filters.regionPlaceholder')"
          clearable
          class="status-select status-select--region"
          :teleported="false"
        >
          <el-option :label="t('inventoryList.warehouse.regionDomestic')" :value="REGION_TYPE_DOMESTIC" />
          <el-option :label="t('inventoryList.warehouse.regionOverseas')" :value="REGION_TYPE_OVERSEAS" />
        </el-select>
        <el-select
          v-model="filterForm.stockOutType"
          :placeholder="t('stockOutNotifyList.filters.stockOutTypePlaceholder')"
          clearable
          class="status-select status-select--stock-out-type"
          :teleported="false"
        >
          <el-option
            v-for="v in STOCK_OUT_TYPE_FILTER_VALUES"
            :key="v"
            :label="notifyStockOutTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <div v-if="!maskSaleSensitiveFields" class="search-input-wrap">
          <input
            v-model="filterForm.customerName"
            class="search-input search-input--customer"
            type="search"
            :placeholder="t('stockOutNotifyList.filters.customerPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <div v-if="!maskSaleSensitiveFields" class="search-input-wrap">
          <input
            v-model="filterForm.salesUserName"
            class="search-input search-input--sales"
            type="search"
            :placeholder="t('stockOutNotifyList.filters.salesUserPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="search-input-wrap">
          <input
            v-model="filterForm.materialModel"
            class="search-input search-input--material"
            type="search"
            :placeholder="t('stockOutNotifyList.filters.materialModelPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-date-picker
          v-model="filterForm.requestDateRange"
          type="daterange"
          :range-separator="t('stockOutNotifyList.filters.dateTo')"
          :start-placeholder="t('stockOutNotifyList.filters.dateStart')"
          :end-placeholder="t('stockOutNotifyList.filters.dateEnd')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('stockOutNotifyList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('stockOutNotifyList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-out-notify-list-main-v7"
      :columns="stockOutNotifyColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      row-key="id"
      v-loading="loading"
      @selection-change="onSelectionChange"
      @row-dblclick="goDetail"
    >
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-customsStatus="{ row }">{{ customsStatusLabel(row.customsStatus) }}</template>
      <template #col-stockOutType="{ row }">
        <StockBizTypeTag biz="out" :type="row.stockOutType" />
      </template>
      <template #col-requestCode="{ row }">
        <span class="notify-code-cell">
          <span class="notify-code-text">{{ row.requestCode || '—' }}</span>
          <el-tooltip
            v-if="isCustomsNotify(row) && salesNotifyTooltip(row)"
            :content="salesNotifyTooltip(row)"
            placement="top"
            :hide-after="0"
          >
            <span class="customs-notify-tag">{{ t('stockOutNotifyList.customsNotifyTag') }}</span>
          </el-tooltip>
        </span>
      </template>
      <template #col-outQuantity="{ row }">{{ row.outQuantity }}</template>
      <template #col-regionType="{ row }">{{ regionTypeLabel(row) }}</template>
      <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      <template #col-expressCompany="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
      <template #col-packingCode="{ row }">
        <router-link
          v-if="row.packingId?.trim() && row.packingCode?.trim()"
          :to="{ name: 'PackingDetail', params: { id: row.packingId.trim() } }"
          class="cell-link"
          @click.stop
        >
          {{ row.packingCode.trim() }}
        </router-link>
        <span v-else-if="row.packingCode?.trim()">{{ row.packingCode.trim() }}</span>
        <span v-else>—</span>
      </template>
      <template #col-requestDate="{ row }">{{ formatRequestDateTime(row.requestDate) }}</template>
      <template #col-createTime="{ row }">{{ formatRequestDateTime(row.createTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.requestUserName || '--' }}</template>
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
          <template v-if="opColExpanded">
            <div
              v-if="Number(row.status) !== STOCK_OUT_REQUEST_STATUS.StockedOut && Number(row.status) !== STOCK_OUT_REQUEST_STATUS.Cancelled"
              class="action-btns"
            >
              <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteRow(row)">删除</button>
              <button v-if="isSysAdmin" type="button" class="action-btn action-btn--danger" @click.stop="handleForceDeleteRow(row)">
                强制删除
              </button>
            </div>
            <span v-else class="op-done">{{ t('stockOutNotifyList.actions.alreadyShipped') }}</span>
          </template>
          <template v-else>
            <el-tooltip
              v-if="Number(row.status) === STOCK_OUT_REQUEST_STATUS.StockedOut"
            :content="t('stockOutNotifyList.actions.alreadyShipped')"
            placement="left"
            :hide-after="0"
          >
            <span class="op-done op-done--collapsed" :aria-label="t('stockOutNotifyList.actions.alreadyShipped')">—</span>
            </el-tooltip>
            <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="handleDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="isSysAdmin" @click.stop="handleForceDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">强制删除</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
            </el-dropdown>
          </template>
        </div>
      </template>
    </CrmDataTable>
    <div v-if="listTotal > 0" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('stockOutNotifyList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('stockOutNotifyList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
        <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
          {{ t('stockOutNotifyList.basket.open') }}<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </el-button>
        <el-button
          v-if="basketCount"
          class="basket-clear-btn"
          link
          type="warning"
          @click="handleClearBasket"
        >
          {{ t('stockOutNotifyList.basket.clear') }}
        </el-button>
        <button
          type="button"
          class="btn-primary btn-sm basket-batch-purchase-btn"
          :disabled="!basketCount"
          @click="handleCreatePacking"
        >
          {{ t('stockOutNotifyList.actions.createPacking') }}
        </button>
      </div>
      <el-pagination
        class="list-main-pagination quantum-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void runNotifyFetch(false)"
        @size-change="onNotifyPageSizeChange"
      />
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      :title="t('stockOutNotifyList.basket.drawerTitle')"
      direction="rtl"
      size="min(560px, 94vw)"
      class="stock-out-notify-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">{{ t('stockOutNotifyList.basket.emptyHint') }}</p>
      <template v-else>
        <p class="basket-drawer-summary">
          {{ t('stockOutNotifyList.basket.summaryBeforeBtn', { count: basketCount }) }}
          <el-button
            class="basket-clear-btn basket-clear-btn--drawer-inline"
            link
            type="warning"
            @click="handleClearBasket"
          >
            {{ t('stockOutNotifyList.basket.clear') }}
          </el-button>
          {{ t('stockOutNotifyList.basket.summaryAfterBtn') }}
        </p>
        <div class="crm-items-table crm-data-table">
          <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
            <el-table-column
              :label="t('stockOutNotifyList.columns.requestCode')"
              prop="requestCode"
              min-width="140"
              show-overflow-tooltip
            />
            <el-table-column
              :label="t('stockOutNotifyList.columns.materialModel')"
              prop="materialModel"
              min-width="130"
              show-overflow-tooltip
            />
            <el-table-column
              :label="t('stockOutNotifyList.columns.customer')"
              prop="customerName"
              min-width="120"
              show-overflow-tooltip
            />
            <el-table-column :label="t('stockOutNotifyList.columns.status')" width="100" align="center">
              <template #default="{ row }">
                <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
              </template>
            </el-table-column>
            <el-table-column :label="t('stockOutNotifyList.columns.actions')" width="72" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="warning" size="small" @click="removeOneFromBasket(row.id)">
                  {{ t('stockOutNotifyList.basket.remove') }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { storeToRefs } from 'pinia'
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import { normalizeRegionType, REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useAuthStore } from '@/stores/auth'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { useStockOutNotifyListBasketStore } from '@/stores/stockOutNotifyListBasket'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { STOCK_OUT_NOTIFY_CUSTOMS_STATUS } from '@/constants/stockOutNotifyCustomsStatus'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { StockOutTypeCode, STOCK_OUT_TYPE_FILTER_VALUES } from '@/constants/stockOutType'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const router = useRouter()
const { t, locale } = useI18n()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const authStore = useAuthStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const loading = ref(false)
const filterForm = reactive({
  status: undefined as number | undefined,
  regionType: undefined as number | undefined,
  stockOutType: undefined as number | undefined,
  customerName: '',
  salesUserName: '',
  materialModel: '',
  requestDateRange: null as [string, string] | null
})
const list = ref<StockOutRequestDto[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
const dataTableRef = ref<{
  openColumnSettings?: () => void
  clearSelection?: () => void
  toggleRowSelection?: (row: unknown, selected?: boolean) => void
} | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const basketStore = useStockOutNotifyListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)
const basketDrawerVisible = ref(false)
const suppressBasketMerge = ref(false)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const statusFilterOptions = computed(() => [
  { value: STOCK_OUT_REQUEST_STATUS.PendingCustoms, label: t('stockOutNotifyList.status.pendingCustoms') },
  { value: STOCK_OUT_REQUEST_STATUS.PendingPacking, label: t('stockOutNotifyList.status.pendingPacking') },
  { value: STOCK_OUT_REQUEST_STATUS.Packed, label: t('stockOutNotifyList.status.packed') },
  { value: STOCK_OUT_REQUEST_STATUS.StockedOut, label: t('stockOutNotifyList.status.stockedOut') },
  { value: STOCK_OUT_REQUEST_STATUS.Cancelled, label: t('stockOutNotifyList.status.cancelled') }
])

function buildListQueryParams() {
  const params: Parameters<typeof stockOutApi.getRequestListPaged>[0] = {
    page: listPage.value,
    pageSize: listPageSize.value
  }
  if (filterForm.status != null) params.status = filterForm.status
  if (filterForm.regionType === REGION_TYPE_DOMESTIC || filterForm.regionType === REGION_TYPE_OVERSEAS) {
    params.regionType = filterForm.regionType
  }
  if (filterForm.stockOutType != null) params.stockOutType = filterForm.stockOutType
  const customer = filterForm.customerName.trim()
  if (customer) params.customerName = customer
  const salesUser = filterForm.salesUserName.trim()
  if (salesUser) params.salesUserName = salesUser
  const material = filterForm.materialModel.trim()
  if (material) params.materialModel = material
  if (filterForm.requestDateRange?.[0]) params.requestDateFrom = filterForm.requestDateRange[0]
  if (filterForm.requestDateRange?.[1]) params.requestDateTo = filterForm.requestDateRange[1]
  return params
}

const stockOutNotifyColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
  {
    key: 'selection',
    type: 'selection',
    width: 44,
    fixed: 'left',
    hideable: false,
    reorderable: false,
    reserveSelection: true
  },
  { key: 'status', label: t('stockOutNotifyList.columns.status'), prop: 'status', width: 110, align: 'center' },
  {
    key: 'customsStatus',
    label: t('stockOutNotifyList.columns.customsStatus'),
    width: 120,
    minWidth: 110,
    align: 'center'
  },
  {
    key: 'stockOutType',
    label: t('stockOutNotifyList.columns.stockOutType'),
    width: 110,
    minWidth: 100,
    align: 'center'
  },
  { key: 'materialModel', label: t('stockOutNotifyList.columns.materialModel'), prop: 'materialModel', width: 180, showOverflowTooltip: true },
  { key: 'brand', label: t('stockOutNotifyList.columns.brand'), prop: 'brand', width: 140, showOverflowTooltip: true },
  { key: 'outQuantity', label: t('stockOutNotifyList.columns.outQuantity'), prop: 'outQuantity', width: 110, align: 'right' },
  {
    key: 'regionType',
    label: t('stockOutNotifyList.columns.regionType'),
    width: 100,
    minWidth: 100,
    align: 'center'
  },
  {
    key: 'shipmentMethod',
    label: t('stockOutNotifyList.columns.shipmentMethod'),
    width: 120,
    minWidth: 100,
    showOverflowTooltip: true
  },
  {
    key: 'expressCompany',
    label: t('stockOutNotifyList.columns.expressCompany'),
    width: 120,
    minWidth: 100,
    showOverflowTooltip: true
  },
  {
    key: 'packingCode',
    label: t('stockOutNotifyList.columns.packingCode'),
    prop: 'packingCode',
    width: 150,
    minWidth: 130,
    showOverflowTooltip: true
  },
  { key: 'requestDate', label: t('stockOutNotifyList.columns.requestDate'), prop: 'requestDate', width: 170 },
  { key: 'salesUserName', label: t('stockOutNotifyList.columns.salesUserName'), prop: 'salesUserName', width: 130, showOverflowTooltip: true },
  { key: 'customerName', label: t('stockOutNotifyList.columns.customer'), prop: 'customerName', minWidth: 180, showOverflowTooltip: true },
  { key: 'remark', label: t('stockOutNotifyList.columns.remark'), prop: 'remark', minWidth: 180, showOverflowTooltip: true },
  { key: 'requestCode', label: t('stockOutNotifyList.columns.requestCode'), prop: 'requestCode', width: 190, minWidth: 170 },
  { key: 'salesOrderCode', label: t('stockOutNotifyList.columns.salesOrderCode'), prop: 'salesOrderCode', width: 160, minWidth: 160 },
  { key: 'createTime', label: t('stockOutNotifyList.columns.createTime'), prop: 'createTime', width: 170 },
  { key: 'createUser', label: t('stockOutNotifyList.columns.createUser'), width: 140, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('stockOutNotifyList.columns.actions'),
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

const statusLabel = (s: number) => {
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function customsStatusLabel(code?: number | null): string {
  const n = Number(code ?? 0)
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.NotRequired) return t('stockOutNotifyList.customsStatus.notRequired')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) return t('stockOutNotifyList.customsStatus.pendingCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms) return t('stockOutNotifyList.customsStatus.inCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) return t('stockOutNotifyList.customsStatus.completed')
  return '—'
}

function notifyStockOutTypeLabel(type?: number | null): string {
  const n = Number(type ?? StockOutTypeCode.Sales)
  if (n === StockOutTypeCode.Customs) return t('stockOutNotifyList.stockOutTypeLabels.customs')
  if (n === StockOutTypeCode.Return) return t('stockOutNotifyList.stockOutTypeLabels.return')
  if (n === StockOutTypeCode.Scrap) return t('stockOutNotifyList.stockOutTypeLabels.scrap')
  return t('stockOutNotifyList.stockOutTypeLabels.sales')
}

function isCustomsNotify(row: StockOutRequestDto): boolean {
  return resolveNotifyStockOutType(row.stockOutType) === StockOutTypeCode.Customs
}

function salesNotifyTooltip(row: StockOutRequestDto): string {
  const code = String(row.salesStockOutNotifyCode ?? '').trim()
  if (!code) return ''
  return t('stockOutNotifyList.salesNotifyCodeTooltip', { code })
}

const regionTypeLabel = (row: StockOutRequestDto) => {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function shipmentMethodDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = shipmentArrivalOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = expressOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

/** 按本地时区显示年月日 + 时分 */
const formatRequestDateTime = (v?: string | null) => {
  if (v == null || v === '') return '--'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

async function runNotifyFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const reqPage = await stockOutApi.getRequestListPaged(buildListQueryParams())
    list.value = reqPage.items
    listTotal.value = reqPage.total
    await restoreTableSelectionFromBasket()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutNotifyList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  void runNotifyFetch(true)
}

function onNotifyPageSizeChange() {
  listPage.value = 1
  void runNotifyFetch(false)
}

function handleReset() {
  filterForm.status = undefined
  filterForm.regionType = undefined
  filterForm.stockOutType = undefined
  filterForm.customerName = ''
  filterForm.salesUserName = ''
  filterForm.materialModel = ''
  filterForm.requestDateRange = null
  void runNotifyFetch(true)
}

function resolveNotifyId(row: StockOutRequestDto): string {
  return String(row?.id || '').trim()
}

function isPendingNotify(row: StockOutRequestDto): boolean {
  return Number(row.status) === STOCK_OUT_REQUEST_STATUS.PendingPacking
}

async function onSelectionChange(rows: StockOutRequestDto[]) {
  if (suppressBasketMerge.value) return
  const eligible = rows.filter(isPendingNotify)
  const ineligible = rows.filter((r) => !isPendingNotify(r))
  if (ineligible.length > 0) {
    suppressBasketMerge.value = true
    for (const row of ineligible) {
      dataTableRef.value?.toggleRowSelection?.(row, false)
    }
    await nextTick()
    suppressBasketMerge.value = false
    ElMessage.warning(t('stockOutNotifyList.packing.onlyPendingSelectable'))
  }
  basketStore.mergePageSelection(list.value, eligible)
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  table.clearSelection?.()
  await nextTick()
  for (const row of list.value) {
    const id = resolveNotifyId(row)
    if (id && basketStore.has(id)) {
      table.toggleRowSelection?.(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeOneFromBasket(id: string) {
  const rid = String(id || '').trim()
  if (!rid) return
  basketStore.remove(rid)
  suppressBasketMerge.value = true
  const row = list.value.find((r) => resolveNotifyId(r) === rid)
  if (row) {
    dataTableRef.value?.toggleRowSelection?.(row, false)
  }
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
}

async function handleClearBasket() {
  if (!basketStore.count) return
  try {
    await ElMessageBox.confirm(
      t('stockOutNotifyList.messages.clearBasketConfirm'),
      t('stockOutNotifyList.messages.clearBasketTitle'),
      {
        type: 'warning',
        confirmButtonText: t('stockOutNotifyList.messages.clearBasketOk'),
        cancelButtonText: t('stockOutNotifyList.messages.clearBasketCancel')
      }
    )
  } catch {
    return
  }
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection?.()
  await nextTick()
  suppressBasketMerge.value = false
  ElMessage.success(t('stockOutNotifyList.messages.basketCleared'))
}

type PackingSelectionValidation = { ok: true } | { ok: false; reasons: string[] }

function resolveNotifyStockOutType(v: unknown): number {
  const n = Number(v)
  if (
    n === StockOutTypeCode.Sales ||
    n === StockOutTypeCode.Customs ||
    n === StockOutTypeCode.Return ||
    n === StockOutTypeCode.Scrap
  ) {
    return n
  }
  return StockOutTypeCode.Sales
}

function validatePackingSelection(rows: StockOutRequestDto[]): PackingSelectionValidation {
  const reasons: string[] = []
  if (rows.length === 0) {
    reasons.push(t('stockOutNotifyList.packing.noSelection'))
    return { ok: false, reasons }
  }

  const customerIds = rows.map((r) => String(r.customerId || '').trim())
  const customerNames = rows.map((r) => String(r.customerName || '').trim())
  const uniqueCustomerIds = new Set(customerIds.filter(Boolean))
  const uniqueCustomerNames = new Set(customerNames.filter(Boolean))
  if (uniqueCustomerIds.size === 0 && uniqueCustomerNames.size === 0) {
    reasons.push(t('stockOutNotifyList.packing.ruleCustomerRequired'))
  } else if (uniqueCustomerIds.size > 1 || (uniqueCustomerIds.size === 0 && uniqueCustomerNames.size > 1)) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameCustomer'))
  }

  const currencyKeys = rows.map((r) => {
    const raw = r.currency
    if (raw == null) return null
    const n = Number(raw)
    return Number.isFinite(n) ? n : null
  })
  const uniqueCurrencies = new Set(
    currencyKeys.filter((c): c is number => c !== null).map((c) => String(c))
  )
  const currencyMismatch =
    currencyKeys.some((c) => c === null) || uniqueCurrencies.size !== 1
  if (currencyMismatch) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameCurrency'))
  }

  const stockOutTypes = rows.map((r) => resolveNotifyStockOutType(r.stockOutType))
  if (new Set(stockOutTypes).size !== 1) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameStockOutType'))
  }

  const hasMissingShipment = rows.some((r) => !String(r.shipmentMethod ?? '').trim())
  if (hasMissingShipment) {
    reasons.push(t('stockOutNotifyList.packing.ruleShipmentMethodRequired'))
  } else {
    const uniqueShipmentMethods = new Set(rows.map((r) => String(r.shipmentMethod ?? '').trim()))
    if (uniqueShipmentMethods.size > 1) {
      reasons.push(t('stockOutNotifyList.packing.ruleSameShipmentMethod'))
    }
  }

  const expressKeys = rows.map((r) => String(r.expressCompany ?? '').trim())
  if (new Set(expressKeys).size > 1) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameExpressCompany'))
  }

  if (reasons.length > 0) return { ok: false, reasons }
  return { ok: true }
}

async function showPackingValidationAlert(reasons: string[]) {
  const body =
    reasons.length === 1 && reasons[0] === t('stockOutNotifyList.packing.noSelection')
      ? reasons[0]
      : `${t('stockOutNotifyList.packing.cannotCreateIntro')}\n\n${reasons.map((r) => `• ${r}`).join('\n')}`
  await ElMessageBox.alert(body, t('stockOutNotifyList.packing.cannotCreateTitle'), {
    confirmButtonText: t('stockOutNotifyList.packing.cannotCreateOk'),
    type: 'warning'
  })
}

async function handleCreatePacking() {
  const rows = basketStore.items.filter(isPendingNotify)
  const validation = validatePackingSelection(rows)
  if (!validation.ok) {
    await showPackingValidationAlert(validation.reasons)
    return
  }
  void router.push({
    name: 'PackingCreate',
    query: { ids: rows.map((r) => r.id).join(',') }
  })
}

function goDetail(row: StockOutRequestDto) {
  const id = String(row?.id || '').trim()
  if (!id) {
    ElMessage.warning(t('stockOutNotifyList.messages.missingId'))
    return
  }
  router.push({ name: 'StockOutNotifyDetail', params: { id } })
}

const handleDeleteRow = async (row: StockOutRequestDto) => {
  const ok = window.confirm(`确认删除出库通知 ${row.requestCode} 吗？`)
  if (!ok) return
  try {
    await stockOutApi.deleteStockOutRequest(row.id)
    ElMessage.success('删除成功')
    await runNotifyFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '删除失败')
  }
}

const handleForceDeleteRow = async (row: StockOutRequestDto) => {
  const entered = window.prompt('请输入出库通知单号以确认强制删除', row.requestCode || '')?.trim() ?? ''
  if (!entered) return
  if (entered !== String(row.requestCode || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await stockOutApi.forceDeleteStockOutRequest(row.id, entered)
    ElMessage.success('强制删除成功')
    await runNotifyFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '强制删除失败')
  }
}

onMounted(() => {
  void ensureLogisticsDict()
  void runNotifyFetch(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-notify-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
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
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 搜索栏（与 CustomerList.vue 一致）----
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

  &--wide {
    width: 280px;
  }

  &--customer {
    width: 160px;
  }

  &--sales {
    width: 120px;
  }

  &--material {
    width: 160px;
  }
}

.status-select--region {
  width: 110px;
}

.status-select--stock-out-type {
  width: 120px;
}

.notify-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

.notify-code-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.customs-notify-tag {
  flex: 0 0 auto;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 1.4;
  color: #ffb84d;
  background: rgba(255, 184, 77, 0.14);
  border: 1px solid rgba(255, 184, 77, 0.45);
  cursor: default;
  user-select: none;
}

.filter-date-range {
  width: 260px !important;

  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
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

.status-select--workflow {
  width: 168px;
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
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-5 { background: rgba(156, 89, 182, 0.18); color: #9c59b6; }
  &.status-10 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-20 { background: rgba(0,212,255,0.15); color: $cyan-primary; }
  &.status-100 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status--1 { background: rgba(201,87,69,0.18); color: #C95745; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  &:hover { text-decoration: underline; }
}
.op-done {
  font-size: 12px;
  color: $text-muted;
}

.op-done--collapsed {
  display: inline-block;
  width: 100%;
  text-align: center;
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.list-main-pagination {
  margin-left: auto;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
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

.basket-batch-purchase-btn {
  margin-left: 10px;
  letter-spacing: normal;

  &:hover:not(:disabled) {
    transform: none;
    box-shadow: none;
  }
}

.basket-open-btn {
  padding: 4px 6px 4px 8px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-clear-btn {
  padding: 4px 8px 4px 2px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-count-label {
  color: $cyan-primary;
  font-weight: 600;
  margin-left: 2px;
}

.pagination-wrapper .list-main-pagination {
  margin-left: auto;
  align-self: flex-start;
}

.cell-link {
  color: var(--el-color-primary);
  text-decoration: none;
}
.cell-link:hover {
  text-decoration: underline;
}
</style>

<style lang="scss">
@import '@/assets/styles/variables.scss';

.stock-out-notify-basket-drawer {
  .basket-drawer-hint {
    font-size: 13px;
    color: rgba(255, 255, 255, 0.55);
    line-height: 1.6;
    margin: 0 0 12px;
  }

  .basket-drawer-summary {
    font-size: 13px;
    color: rgba(232, 244, 255, 0.75);
    margin: 0 0 12px;
    line-height: 1.6;
  }

  .basket-clear-btn--drawer-inline {
    vertical-align: baseline;
    height: auto !important;
    min-height: 0 !important;
    padding: 0 2px !important;
    margin: 0 1px;
    font-size: 13px !important;
    font-weight: 500;
  }
}
</style>
