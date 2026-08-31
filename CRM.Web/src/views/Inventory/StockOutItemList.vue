<template>
  <!-- 业务列表页：结构对齐《业务列表规范》《列表搜索栏规范》；表格见 CrmDataTable + 全局 crm-unified-list.scss -->
  <div class="stockout-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutItemList.title') }}</h1>
          <div v-if="viewMode === 'list'" class="count-badge">{{ t('stockOutItemList.count', { count: listTotal }) }}</div>
        </div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-if="tabModeDimension !== 'status'"
          v-model="filters.status"
          clearable
          :placeholder="t('stockOutItemList.filters.status')"
          class="status-select"
          :teleported="false"
          @change="fetchList"
        >
          <el-option :label="t('stockOutList.status.draft')" :value="0" />
          <el-option :label="t('stockOutList.status.pending')" :value="1" />
          <el-option :label="t('stockOutList.status.done')" :value="2" />
          <el-option :label="t('stockOutList.status.finished')" :value="4" />
          <el-option :label="t('stockOutList.status.cancelled')" :value="3" />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'stockOutType'"
          v-model="filters.stockOutType"
          clearable
          :placeholder="t('stockOutItemList.filters.stockOutType')"
          class="filter-select"
          :teleported="false"
          @change="fetchList"
        >
          <el-option
            v-for="v in STOCK_OUT_TYPE_FILTER_VALUES"
            :key="v"
            :label="listStockOutTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.stockOutCode"
            class="search-input search-input--keyword"
            type="search"
            :placeholder="t('stockOutItemList.filters.stockOutCode')"
            @keyup.enter="fetchList"
          />
        </div>
        <input
          v-model="filters.stockOutItemCode"
          class="search-input search-input--filter search-input--wide"
          type="search"
          :placeholder="t('stockOutItemList.filters.stockOutItemCode')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.stockInCode"
          class="search-input search-input--filter search-input--wide"
          type="search"
          :placeholder="t('stockOutItemList.filters.stockInCode')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.packingCode"
          class="search-input search-input--filter search-input--wide"
          type="search"
          :placeholder="t('stockOutItemList.filters.packingCode')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.freightForwarderOrderNo"
          class="search-input search-input--filter search-input--wide"
          type="search"
          :placeholder="t('common.freightForwarderOrderNoPlaceholder')"
          @keyup.enter="fetchList"
        />
        <div
          class="filter-date-range"
          role="group"
          :aria-label="t('stockOutItemList.filters.stockOutDateRange')"
        >
          <el-date-picker
            v-model="dateFrom"
            type="date"
            value-format="YYYY-MM-DD"
            clearable
            :placeholder="t('stockOutItemList.filters.stockOutDateFrom')"
            class="filter-date-range__picker filter-date-range__picker--start"
            :teleported="false"
            @change="fetchList"
          />
          <span class="filter-date-range__sep">{{ t('stockOutItemList.filters.stockOutDateSep') }}</span>
          <el-date-picker
            v-model="dateTo"
            type="date"
            value-format="YYYY-MM-DD"
            clearable
            :placeholder="t('stockOutItemList.filters.stockOutDateTo')"
            class="filter-date-range__picker filter-date-range__picker--end"
            :teleported="false"
            @change="fetchList"
          />
        </div>
        <input
          v-if="!maskSaleSensitiveFields"
          v-model="filters.customerName"
          class="search-input search-input--filter"
          type="search"
          :placeholder="t('stockOutItemList.filters.customerName')"
          @keyup.enter="fetchList"
        />
        <input
          v-if="!maskSaleSensitiveFields"
          v-model="filters.salesUserName"
          class="search-input search-input--filter"
          type="search"
          :placeholder="t('stockOutItemList.filters.salesUserName')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.purchasePn"
          class="search-input search-input--filter"
          type="search"
          :placeholder="t('stockOutItemList.filters.purchasePn')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.sellOrderItemCode"
          class="search-input search-input--filter search-input--wide"
          type="search"
          :placeholder="t('stockOutItemList.filters.sellOrderItemCode')"
          @keyup.enter="fetchList"
        />
        <button type="button" class="btn-primary btn-sm" @click="fetchList">{{ t('stockOutItemList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('stockOutItemList.filters.reset') }}</button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('stockOutItemList.filters.listView') : t('stockOutItemList.filters.boardView') }}
        </button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="stock-out-item-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('stockOutItemList.settingsMenu.aria')"
              :aria-label="t('stockOutItemList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="stock-out-item-list-settings-menu">
            <button
              type="button"
              class="stock-out-item-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('stockOutItemList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="stock-out-item-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="stock-out-item-list-settings-menu__item stock-out-item-list-settings-menu__item--parent">
                <span>{{ t('stockOutItemList.settingsMenu.tabMode') }}</span>
                <el-icon class="stock-out-item-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="stock-out-item-list-settings-menu__flyout">
                <button
                  v-for="dim in STOCK_OUT_ITEM_LIST_TAB_MODE_OPTIONS"
                  :key="dim"
                  type="button"
                  class="stock-out-item-list-settings-menu__item"
                  :class="{ 'is-active': tabModeDimension === dim }"
                  @click="enableFilterTabMode(dim)"
                >
                  {{ tabModeDimensionLabel(dim) }}
                </button>
              </div>
            </div>
          </div>
        </el-popover>
      </div>
    </div>

    <div class="soi-main-panel" :class="{ 'soi-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="soi-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="soi-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <StockOutItemListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <!-- 主表：列设置齿轮 + 行高密度锚点见《业务列表规范》§1.2、§2.3；双击行见《列表交互规范》 -->
    <CrmDataTable
      v-show="viewMode === 'list'"
      ref="dataTableRef"
      class="stockout-item-list-crm-table"
      column-layout-key="stock-out-item-list-main-v1"
      :columns="stockOutItemTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
      row-key="stockOutItemId"
      :row-class-name="highlightRowClassName"
      @row-dblclick="onRowDblclick"
      @row-click="onRowClick"
    >
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-stockOutCode="{ row }">
        <router-link
          v-if="row.stockOutId?.trim() && row.stockOutCode?.trim()"
          class="link-text mono-cell"
          :to="`/inventory/stock-out/${encodeURIComponent(row.stockOutId.trim())}`"
          @click.stop
        >
          {{ row.stockOutCode.trim() }}
        </router-link>
        <span v-else class="mono-cell">{{ row.stockOutCode || t('quoteList.na') }}</span>
      </template>
      <template #col-stockOutItemCode="{ row }">{{ row.stockOutItemCode || t('quoteList.na') }}</template>
      <template #col-stockInCode="{ row }">{{ row.stockInCode || t('quoteList.na') }}</template>
      <template #col-packingCode="{ row }">
        <router-link
          v-if="row.packingId?.trim() && row.packingCode?.trim()"
          class="link-text mono-cell"
          :to="`/inventory/packing/${row.packingId.trim()}`"
          @click.stop
        >
          {{ row.packingCode.trim() }}
        </router-link>
        <span v-else-if="row.packingCode?.trim()" class="mono-cell">{{ row.packingCode.trim() }}</span>
        <span v-else>{{ t('quoteList.na') }}</span>
      </template>
      <template #col-freightForwarderOrderNo="{ row }">
        <CrmListCopyableTextCell :text="row.freightForwarderOrderNo?.trim() || ''" />
      </template>
      <template #col-stockOutDate="{ row }">
        <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.stockOutDate)]" :key="'sod-' + row.stockOutItemId">
          <span v-if="!p" class="so-item-list-dash">{{ t('quoteList.na') }}</span>
          <span v-else-if="isTimeMidnightOnly(p.time)" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
          </span>
          <span v-else class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
        </template>
      </template>
      <template #col-customerName="{ row }">
        {{ maskSaleSensitiveFields ? '—' : row.customerName || t('quoteList.na') }}
      </template>
      <template #col-salesUserName="{ row }">
        {{ maskSaleSensitiveFields ? '—' : row.salesUserName || t('quoteList.na') }}
      </template>
      <template #col-purchasePn="{ row }">
        <CrmListCopyableTextCell :text="row.purchasePn?.trim() || ''" :empty-text="t('quoteList.na')" />
      </template>
      <template #col-purchaseBrand="{ row }">
        <CrmListCopyableTextCell :text="row.purchaseBrand?.trim() || ''" :empty-text="t('quoteList.na')" />
      </template>
      <template #col-outQuantity="{ row }">
        <span class="so-item-list-qty">{{ formatQtyCell(row.outQuantity) }}</span>
      </template>
      <template #col-stockOutType="{ row }">
        <StockBizTypeTag biz="out" :type="row.stockOutType" />
      </template>
      <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      <template #col-courierTrackingNo="{ row }">{{ row.courierTrackingNo || t('quoteList.na') }}</template>
      <template #col-sellOrderItemCode="{ row }">{{ row.sellOrderItemCode || t('quoteList.na') }}</template>
    </CrmDataTable>
    <div v-show="viewMode === 'list'" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void runStockOutItemFetch(false)"
        @size-change="onStockOutItemPageSizeChange"
      />
    </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, onBeforeUnmount, reactive, ref, watch } from 'vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { ArrowRight, Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import StockOutItemListBoard from '@/views/Inventory/StockOutItemListBoard.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { useListBoardHelpOverride } from '@/composables/useHelpDocOverride'
import CrmListCopyableTextCell from '@/components/CrmListCopyableTextCell.vue'
import { stockOutApi, type StockOutItemListQuery, type StockOutItemListRow } from '@/api/stockOut'
import type { StockOutItemListAnalyticsQuery } from '@/api/stockOutItemAnalytics'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { buildStockOutItemListColumns } from '@/composables/buildStockOutItemListColumns'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import { useCustomerWorkspacePanelStore } from '@/stores/customerWorkspacePanel'
import { useStockOutItemFlowPanelStore } from '@/stores/stockOutItemFlowPanel'
import { STOCK_OUT_TYPE_FILTER_VALUES, resolveStockOutTypeLabelKey } from '@/constants/stockOutType'
import {
  STOCK_OUT_ITEM_LIST_STATUS_TAB_VALUES,
  STOCK_OUT_ITEM_LIST_TAB_MODE_OPTIONS,
  readStockOutItemListTabMode,
  writeStockOutItemListTabMode,
  type StockOutItemListTabModeDimension
} from '@/utils/stockOutItemListTabMode'
import {
  STOCK_OUT_TYPE_TAB_VALUES,
  stockOutStatusFilterToTab,
  stockOutStatusTabToFilter,
  stockOutTypeFilterToTab,
  stockOutTypeTabToFilter,
  type StockOutStatusTabId,
  type StockOutTypeTabId
} from '@/utils/stockOutListTabMode'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const viewMode = ref<'list' | 'board'>('list')
useListBoardHelpOverride('pages/出库明细看板_MENU_STOCK_OUT_ITEMS_BOARD.md', viewMode)

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const customerWorkspacePanelStore = useCustomerWorkspacePanelStore()
customerWorkspacePanelStore.setSource('stockOutItem')
const stockOutItemFlowStore = useStockOutItemFlowPanelStore()
const { onOpsPanelRowClick: onCustomerPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'StockOutItemList',
  hasSelectedRow: () => !!customerWorkspacePanelStore.boundId,
  setRowOnly: row =>
    customerWorkspacePanelStore.setRowOnly({
      id: String(row.stockOutItemId ?? row.id ?? row.Id ?? '').trim()
    }),
  selectRow: row =>
    customerWorkspacePanelStore.selectRow(
      { id: String(row.stockOutItemId ?? row.id ?? row.Id ?? '').trim() },
      t('customerWorkspace.loadFailed')
    ),
  loadSelected: () => {
    void customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  },
  dataTabIds: ['r-customer']
})
const { onOpsPanelRowClick: onFlowPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'StockOutItemList',
  hasSelectedRow: () => !!stockOutItemFlowStore.row,
  setRowOnly: row => stockOutItemFlowStore.setRowOnly(row as unknown as StockOutItemListRow),
  selectRow: row =>
    stockOutItemFlowStore.selectRow(
      row as unknown as StockOutItemListRow,
      t('stockOutItemList.flowPanel.loadFailed')
    ),
  loadSelected: () => {
    void stockOutItemFlowStore.loadSelected(t('stockOutItemList.flowPanel.loadFailed'))
  },
  dataTabIds: ['r-flow']
})

const highlightCode = computed(() => {
  const raw = route.query.highlight
  const v = Array.isArray(raw) ? raw[0] : raw
  return String(v ?? '').trim()
})
const { ensureLoaded: ensureLogisticsDict, arrivalOptions } = useLogisticsFormDict()

/** LogisticsArrivalMethod ItemCode → 字典显示名（与出库单头、出库详情一致） */
const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of arrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

function shipmentMethodDisplay(code?: string | number | null): string {
  if (code === null || code === undefined || code === '') return t('quoteList.na')
  const c = String(code).trim()
  if (!c) return t('quoteList.na')
  const label = arrivalLabelByCode.value.get(c.toLowerCase())
  return label ?? c
}

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const stockOutItemTableColumns = computed<CrmTableColumnDef[]>(() => buildStockOutItemListColumns({ t }))

const tabModeDimension = ref<StockOutItemListTabModeDimension>(readStockOutItemListTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)

const statusFilterOptions = computed(() => [
  { value: 0, label: t('stockOutList.status.draft') },
  { value: 1, label: t('stockOutList.status.pending') },
  { value: 2, label: t('stockOutList.status.done') },
  { value: 4, label: t('stockOutList.status.finished') },
  { value: 3, label: t('stockOutList.status.cancelled') }
])

const TAB_MODE_FILTER_I18N: Record<Exclude<StockOutItemListTabModeDimension, 'off'>, string> = {
  status: 'stockOutItemList.filters.status',
  stockOutType: 'stockOutItemList.filters.stockOutType'
}

function tabModeDimensionLabel(dim: Exclude<StockOutItemListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeStockOutItemListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<StockOutItemListTabModeDimension, 'off'>) {
  tabModeDimension.value = dim
  writeStockOutItemListTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const filterTabStripVisible = computed(() => tabModeDimension.value !== 'off')

const filterTabStripAriaLabel = computed(() => {
  if (tabModeDimension.value === 'off') return ''
  return tabModeDimensionLabel(tabModeDimension.value)
})

type StockOutItemFilterTabId = StockOutStatusTabId | StockOutTypeTabId

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return [] as Array<{ id: StockOutItemFilterTabId; label: string }>
  if (dim === 'status') {
    const labelByValue = new Map(statusFilterOptions.value.map((o) => [o.value, o.label]))
    return [
      { id: 'all' as const, label: t('stockOutItemList.filterTabs.all') },
      ...STOCK_OUT_ITEM_LIST_STATUS_TAB_VALUES.map((value) => ({
        id: String(value) as StockOutStatusTabId,
        label: labelByValue.get(value) ?? String(value)
      }))
    ]
  }
  return [
    { id: 'all' as const, label: t('stockOutItemList.filterTabs.all') },
    ...STOCK_OUT_TYPE_TAB_VALUES.map((value) => ({
      id: String(value) as StockOutTypeTabId,
      label: listStockOutTypeLabel(value)
    }))
  ]
})

const activeFilterTabId = computed((): StockOutItemFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'status') return stockOutStatusFilterToTab(filters.status)
  if (dim === 'stockOutType') return stockOutTypeFilterToTab(filters.stockOutType)
  return 'all'
})

function onFilterTabClick(tab: StockOutItemFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'status') {
    const next = stockOutStatusTabToFilter(tab as StockOutStatusTabId)
    if (filters.status === next) return
    filters.status = next
    fetchList()
    return
  }
  if (dim === 'stockOutType') {
    const next = stockOutTypeTabToFilter(tab as StockOutTypeTabId)
    if (filters.stockOutType === next) return
    filters.stockOutType = next
    fetchList()
  }
}

const loading = ref(false)
const list = ref<StockOutItemListRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotal = ref(0)
watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})
const dateFrom = ref<string | null>(null)
const dateTo = ref<string | null>(null)

const filters = reactive({
  status: undefined as number | undefined,
  stockOutType: undefined as number | undefined,
  stockOutCode: '',
  stockOutItemCode: '',
  stockInCode: '',
  packingCode: '',
  freightForwarderOrderNo: '',
  customerName: '',
  salesUserName: '',
  purchasePn: '',
  sellOrderItemCode: ''
})

const boardFilters = computed<StockOutItemListAnalyticsQuery>(() => buildQuery())

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
  if (viewMode.value === 'list') void runStockOutItemFetch(false)
}

function buildQuery(): StockOutItemListQuery {
  const q: StockOutItemListQuery = {
    status: filters.status,
    stockOutType: filters.stockOutType,
    stockOutCode: filters.stockOutCode.trim() || undefined,
    stockOutItemCode: filters.stockOutItemCode.trim() || undefined,
    stockInCode: filters.stockInCode.trim() || undefined,
    packingCode: filters.packingCode.trim() || undefined,
    freightForwarderOrderNo: filters.freightForwarderOrderNo.trim() || undefined,
    stockOutDateFrom: dateFrom.value?.trim() || undefined,
    stockOutDateTo: dateTo.value?.trim() || undefined,
    purchasePn: filters.purchasePn.trim() || undefined,
    sellOrderItemCode: filters.sellOrderItemCode.trim() || undefined
  }
  if (!maskSaleSensitiveFields.value) {
    q.customerName = filters.customerName.trim() || undefined
    q.salesUserName = filters.salesUserName.trim() || undefined
  }
  return q
}

async function runStockOutItemFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  if (viewMode.value === 'board') return
  loading.value = true
  try {
    const res = await stockOutApi.searchItemsPaged({
      ...buildQuery(),
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = res.items
    listTotal.value = res.total
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('stockOutItemList.messages.loadFailed')))
    list.value = []
    listTotal.value = 0
  } finally {
    loading.value = false
  }
  if (resetPage) {
    resetListRightPanelOnReload(customerWorkspacePanelStore)
    resetListRightPanelOnReload(stockOutItemFlowStore)
  }
}

function onStockOutItemPageSizeChange() {
  listPage.value = 1
  void runStockOutItemFetch(false)
}

const fetchList = () => void runStockOutItemFetch(true)

function applyHighlightFilter() {
  const code = highlightCode.value
  if (code) filters.stockOutItemCode = code
}

function firstQueryString(value: unknown): string {
  if (typeof value === 'string') return value.trim()
  if (Array.isArray(value) && typeof value[0] === 'string') return value[0].trim()
  return ''
}

function syncFiltersFromRoute() {
  const q = route.query
  const statusRaw = firstQueryString(q.status)
  const statusNum = Number(statusRaw)
  filters.status = statusRaw !== '' && Number.isFinite(statusNum) ? statusNum : filters.status
  const typeRaw = firstQueryString(q.stockOutType)
  const typeNum = Number(typeRaw)
  filters.stockOutType = typeRaw !== '' && Number.isFinite(typeNum) ? typeNum : undefined
  const from = firstQueryString(q.stockOutDateFrom)
  const to = firstQueryString(q.stockOutDateTo)
  if (from) dateFrom.value = from
  if (to) dateTo.value = to
}

function highlightRowClassName({ row }: { row: StockOutItemListRow }) {
  const code = highlightCode.value
  const codeHit =
    !!code && String(row.stockOutItemCode ?? '').trim().toLowerCase() === code.toLowerCase()
  const flowActive =
    !!stockOutItemFlowStore.row &&
    stockOutItemFlowStore.rowKey(row) === stockOutItemFlowStore.rowKey(stockOutItemFlowStore.row)
  return [codeHit || flowActive ? 'so-item-row--active' : '', 'table-row-pointer'].filter(Boolean).join(' ')
}

function listStockOutTypeLabel(type: number | undefined | null): string {
  return t(`stockOutList.stockOutTypeLabels.${resolveStockOutTypeLabelKey(type)}`)
}

const resetFilters = () => {
  filters.status = undefined
  filters.stockOutType = undefined
  filters.stockOutCode = ''
  filters.stockOutItemCode = ''
  filters.stockInCode = ''
  filters.packingCode = ''
  filters.freightForwarderOrderNo = ''
  filters.customerName = ''
  filters.salesUserName = ''
  filters.purchasePn = ''
  filters.sellOrderItemCode = ''
  dateFrom.value = null
  dateTo.value = null
  if (highlightCode.value) {
    const nextQuery = { ...route.query }
    delete nextQuery.highlight
    void router.replace({ query: nextQuery })
    return
  }
  void fetchList()
}

function isTimeMidnightOnly(time: string) {
  const t0 = (time || '').trim()
  return t0 === '00:00' || t0.startsWith('00:00:')
}

function formatQtyCell(v: unknown) {
  if (v == null || v === '') return t('quoteList.na')
  const n = Number(v)
  if (!Number.isFinite(n)) return t('quoteList.na')
  return n.toLocaleString('zh-CN')
}

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return t('rfqDetail.unknown')
  }
}

async function onRowClick(row: StockOutItemListRow) {
  await onFlowPanelRowClick(row as unknown as Record<string, unknown>)
  await onCustomerPanelRowClick(row as unknown as Record<string, unknown>)
}

const onRowDblclick = (row: StockOutItemListRow) => {
  const id = (row.stockOutId || '').trim()
  if (!id) {
    ElMessage.warning(t('stockOutItemList.messages.missingStockOutId'))
    return
  }
  void router.push(`/inventory/stock-out/${encodeURIComponent(id)}`)
}

watch(highlightCode, (code, prev) => {
  if (code === prev) return
  applyHighlightFilter()
  void fetchList()
})

watch(
  () =>
    [route.query.status, route.query.stockOutType, route.query.stockOutDateFrom, route.query.stockOutDateTo] as const,
  (next, prev) => {
    if (!prev) return
    if (next[0] === prev[0] && next[1] === prev[1] && next[2] === prev[2] && next[3] === prev[3]) return
    syncFiltersFromRoute()
    void fetchList()
  }
)

onMounted(async () => {
  try {
    await ensureLogisticsDict()
  } catch {
    /* 字典失败时 shipmentMethodDisplay 仍回退为原始码 */
  }
  syncFiltersFromRoute()
  applyHighlightFilter()
  void fetchList()
})

onBeforeUnmount(() => {
  customerWorkspacePanelStore.clear()
  stockOutItemFlowStore.clear()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-item-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 16px;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  display: flex;
  color: $cyan-primary;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.count-badge {
  font-size: 13px;
  color: $text-muted;
}

.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
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

.search-input--keyword {
  width: 200px;
}

.search-input--filter {
  width: 140px;
  padding: 6px 10px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-sm;
  background: $layer-2;
  color: $text-primary;
  font-size: 13px;
}

.search-input--wide {
  width: 160px;
}

.status-select,
.filter-select {
  width: 130px;
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

.filter-date-range {
  display: inline-flex;
  align-items: stretch;
  border: 1px solid $border-panel;
  border-radius: $border-radius-sm;
  background: $layer-2;
  overflow: hidden;
  vertical-align: middle;
}

.filter-date-range__sep {
  display: inline-flex;
  align-items: center;
  padding: 0 6px;
  font-size: 12px;
  color: $text-muted;
  flex-shrink: 0;
  user-select: none;
  border-left: 1px solid $border-panel;
  border-right: 1px solid $border-panel;
}

.filter-date-range__picker {
  width: 132px;
}

.filter-date-range__picker :deep(.el-input__wrapper) {
  box-shadow: none !important;
  background: transparent;
}

.link-text {
  color: inherit;
  text-decoration: none;
  cursor: default;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
    cursor: pointer;
  }
}

.mono-cell {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
}

.so-item-list-qty {
  display: inline-block;
  max-width: 100%;
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .so-item-list-qty {
  color: $text-primary;
}

.so-item-list-dash {
  color: $text-muted;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 {
    background: rgba(255, 255, 255, 0.05);
    color: $text-muted;
  }
  &.status-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.status-2 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }
  &.status-3 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }
  &.status-4 {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
  }
}

.btn-primary,
.btn-ghost {
  padding: 6px 12px;
  border-radius: $border-radius-sm;
  font-size: 12px;
  cursor: pointer;
  border: 1px solid transparent;
}

.btn-primary {
  background: $cyan-primary;
  color: #fff;
}

.btn-ghost {
  border: 1px solid $border-panel;
  background: transparent;
  color: $text-secondary;
}

.btn-board-active {
  border-color: rgba(0, 212, 255, 0.45);
  color: #00d4ff;
  background: rgba(0, 212, 255, 0.08);
}

.btn-icon-only {
  width: 32px;
  padding-left: 0;
  padding-right: 0;
  justify-content: center;
  display: inline-flex;
  align-items: center;
}

.soi-main-panel {
  width: 100%;
}

.soi-main-panel--with-filter-tabs {
  :deep(.crm-data-table-root) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }

  :deep(.el-table),
  :deep(.el-table__inner-wrapper),
  :deep(.el-table__header-wrapper) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }
}

.soi-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.soi-filter-tabs__item {
  flex: 1 1 0;
  min-width: 0;
  padding: 9px 8px;
  border: 1px solid var(--crm-border-panel, #e2e8f0);
  border-bottom: none;
  border-radius: 8px 8px 0 0;
  background: #e8edf5;
  color: var(--crm-text-primary);
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
  text-align: center;
  cursor: pointer;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition: background 0.12s, border-color 0.12s, color 0.12s, box-shadow 0.12s;

  &:hover {
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 45%, var(--crm-border-panel));
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }

  &.is-active {
    background: color-mix(in srgb, var(--crm-cyan-primary) 16%, var(--crm-layer-2, #fff));
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 55%, var(--crm-border-panel));
    box-shadow: inset 0 2px 0 0 var(--crm-cyan-primary);
    font-weight: 600;
    z-index: 1;
  }
}

html[data-theme='dark'] .soi-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
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

.stockout-item-list-crm-table.table-wrapper {
  :deep(.el-table th.so-item-qty-col .cell),
  :deep(.el-table td.so-item-qty-col .cell) {
    overflow: visible;
    text-overflow: clip;
    white-space: nowrap;
  }
}

:deep(.el-table__body tr.el-table__row.so-item-row--active > td.el-table__cell) {
  background: rgba(0, 160, 220, 0.1) !important;
}
</style>

<style lang="scss">
.stock-out-item-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.stock-out-item-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.stock-out-item-list-settings-menu__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 8px 10px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--crm-text-secondary, rgba(224, 244, 255, 0.7));
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  text-align: left;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: var(--crm-accent-008, rgba(0, 212, 255, 0.08));
    color: var(--crm-text-primary, #e8f4ff);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  &.is-active {
    color: var(--crm-cyan-primary, #00d4ff);
  }

  &--parent {
    cursor: default;
  }
}

.stock-out-item-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.stock-out-item-list-settings-menu__submenu {
  position: relative;
}

.stock-out-item-list-settings-menu__flyout {
  position: absolute;
  top: 0;
  left: calc(100% + 4px);
  min-width: 148px;
  padding: 6px;
  border-radius: 8px;
  border: 1px solid var(--crm-border-panel, rgba(0, 212, 255, 0.15));
  background: var(--crm-layer-2, #0d1e35);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.28);
  z-index: 10;
}
</style>
