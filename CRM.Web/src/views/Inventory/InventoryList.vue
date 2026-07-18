<template>
  <div class="inventory-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
              <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
              <line x1="12" y1="22.08" x2="12" y2="12"/>
            </svg>
          </div>
          <h1 class="page-title">{{ t('inventoryList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('inventoryList.count', { count: listTotal }) }}</div>
      </div>
      <div class="header-right">
        <button v-if="canWriteLogisticsData" class="btn-primary" type="button" @click="goWarehouseManage">
          {{ t('inventoryList.actions.warehouseManagement') }}
        </button>
      </div>
    </div>

    <div class="stat-row" v-if="finance">
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.capitalOccupied') }}</div>
        <div class="value">{{ formatMoney(finance.inventoryCapital) }} RMB（折算）</div>
        <div v-if="financeByCurrencyRows.length" class="stat-subrows">
          <div v-for="row in financeByCurrencyRows" :key="`cap-${row.currency}`" class="stat-subrow">
            <span>{{ currencyCodeText(row.currency) }}</span>
            <span>{{ formatMoney(row.inventoryCapital) }}</span>
          </div>
        </div>
      </div>
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.monthlyOutCost') }}</div>
        <div class="value">{{ formatMoney(finance.monthlyOutCost) }} RMB（折算）</div>
        <div v-if="financeByCurrencyRows.length" class="stat-subrows">
          <div v-for="row in financeByCurrencyRows" :key="`out-${row.currency}`" class="stat-subrow">
            <span>{{ currencyCodeText(row.currency) }}</span>
            <span>{{ formatMoney(row.monthlyOutCost) }}</span>
          </div>
        </div>
      </div>
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.turnoverDays') }}</div>
        <div class="value">{{ finance.turnoverDays?.toFixed(2) || '0.00' }}</div>
      </div>
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.stagnantCount') }}</div>
        <div class="value">{{ finance.stagnantMaterialCount }}</div>
      </div>
    </div>

    <!-- 搜索栏：与《业务列表规范》及 StockInList / CustomerList 一致 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-if="tabModeDimension !== 'stockType'"
          v-model="stockTypeFilter"
          :placeholder="t('inventoryList.filters.allOrderTypes')"
          clearable
          :filterable="false"
          class="status-select status-select--inv-order"
          :teleported="false"
          @change="fetchList"
        >
          <el-option :label="t('inventoryList.stockTypes.customer')" :value="1" />
          <el-option :label="t('inventoryList.stockTypes.stocking')" :value="2" />
          <el-option :label="t('inventoryList.stockTypes.sample')" :value="3" />
        </el-select>
        <el-select
          v-if="!warehouseTabStripActive"
          v-model="warehouseFilter"
          :placeholder="t('inventoryList.filters.allInventoryCodes')"
          clearable
          :filterable="false"
          class="status-select status-select--inv-warehouse"
          :teleported="false"
          @change="fetchList"
        >
          <el-option
            v-for="opt in warehouseSelectOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-input
          v-model.trim="materialModelFilter"
          :placeholder="t('inventoryList.filters.materialModelPlaceholder')"
          clearable
          class="search-input search-input--material-model"
          @keyup.enter="fetchList"
        />
        <el-input
          v-model.trim="stockCodeFilter"
          :placeholder="t('inventoryList.filters.stockCodePlaceholder')"
          clearable
          class="search-input search-input--stock-code"
          @keyup.enter="fetchList"
        />
        <button type="button" class="btn-primary btn-sm" @click="fetchList">
          {{ t('inventoryList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="resetInventorySearch">
          {{ t('inventoryList.filters.reset') }}
        </button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="inv-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('inventoryList.settingsMenu.aria')"
              :aria-label="t('inventoryList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="inv-list-settings-menu">
            <button
              type="button"
              class="inv-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('inventoryList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="inv-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="inv-list-settings-menu__item inv-list-settings-menu__item--parent">
                <span>{{ t('inventoryList.settingsMenu.tabMode') }}</span>
                <el-icon class="inv-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="inv-list-settings-menu__flyout">
                <button
                  v-for="dim in INVENTORY_LIST_TAB_MODE_OPTIONS"
                  :key="dim"
                  type="button"
                  class="inv-list-settings-menu__item"
                  :class="{
                    'is-active': tabModeDimension === dim,
                    'is-disabled': dim === 'warehouse' && !warehouseTabModeAllowed
                  }"
                  :disabled="dim === 'warehouse' && !warehouseTabModeAllowed"
                  :title="
                    dim === 'warehouse' && !warehouseTabModeAllowed
                      ? t('inventoryList.settingsMenu.warehouseTabDisabled', {
                          max: INVENTORY_WAREHOUSE_TAB_MAX
                        })
                      : undefined
                  "
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

    <div class="inv-main-panel" :class="{ 'inv-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="inv-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="inv-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        :title="tab.label"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      class="inventory-list-crm-table"
      column-layout-key="inventory-list-main-v5"
      :columns="inventoryTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
      @row-click="onRowClick"
    >
      <template #col-stockCode="{ row }">{{ stockCodeDisplay(row) }}</template>
      <template #col-materialModel="{ row }">
        <CrmListCopyableTextCell :text="materialModelCopyValue(row)" />
      </template>
      <template #col-materialBrand="{ row }">
        <CrmListCopyableTextCell :text="materialBrandCopyValue(row)" />
      </template>
      <template #col-warehouseName="{ row }">{{ warehouseNameOf(row.warehouseId) }}</template>
      <template #col-region="{ row }">
        <span class="region-type-chip" :class="`region-type-chip--${regionTypeKind(row)}`">
          <span>{{ regionLabel(row) }}</span>
        </span>
      </template>
      <template #col-stockType="{ row }">
        <span
          class="inv-stock-type-cell"
          :class="{ 'inv-stock-type-cell--stocking': rowStockTypeNum(row) === 2 }"
        >
          <span>{{ stockTypeLabel(row) }}</span>
          <el-icon v-if="rowStockTypeNum(row) === 2" class="inv-stock-type-icon" aria-hidden="true">
            <Box />
          </el-icon>
        </span>
      </template>
      <template #col-onHandQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.onHandQty) }}</span>
      </template>
      <template #col-availableQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.availableQty) }}</span>
      </template>
      <template #col-lockedQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.lockedQty) }}</span>
      </template>
      <template #col-inventoryAmount="{ row }">
        <template v-if="!inventoryAmountHasValue(row.inventoryAmount)">
          <span class="inv-list-dash">—</span>
        </template>
        <div v-else class="inv-list-amount-cell dock-tier-price-line">
          <template v-for="amt in [splitInventoryMoneyParts(Number(row.inventoryAmount))]" :key="'amt-' + row.stockId">
            <span class="inv-list-amt">
              <span class="inv-list-amt-int">{{ amt.intPart }}</span><span class="inv-list-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', inventoryCurrencyClass(row)]">{{ inventoryCurrencyIso(row) }}</span>
        </div>
      </template>
      <template #col-lastMoveTime="{ row }">
        <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.lastMoveTime)]" :key="'lm-' + row.stockId">
          <span v-if="p" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
          <span v-else class="inv-list-dash">—</span>
        </template>
      </template>
      <template #col-createTime="{ row }">
        <template v-for="p in [formatDisplayDateTime2DigitYearParts((row as any).createTime || (row as any).createdAt)]" :key="'ct-' + row.stockId">
          <span v-if="p" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
          <span v-else class="inv-list-dash">—</span>
        </template>
      </template>
      <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '—' }}</template>
    </CrmDataTable>
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
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onInventoryPageSizeChange"
        @current-change="onInventoryPageChange"
      />
    </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { ArrowRight, Box, Setting } from '@element-plus/icons-vue'
import { inventoryCenterApi, type FinanceSummary, type InventoryOverview, type WarehouseInfo } from '@/api/inventoryCenter'
import {
  INVENTORY_LIST_TAB_MODE_OPTIONS,
  INVENTORY_WAREHOUSE_TAB_MAX,
  INV_STOCK_TYPE_TAB_VALUES,
  readInventoryListTabMode,
  writeInventoryListTabMode,
  invStockTypeFilterToTab,
  invStockTypeTabToFilter,
  invWarehouseFilterToTab,
  invWarehouseTabToFilter,
  isWarehouseTabModeAllowed,
  type InventoryListTabModeDimension,
  type InvStockTypeTabId,
  type InvWarehouseTabId
} from '@/utils/inventoryListTabMode'
import { REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'

const router = useRouter()
const { t } = useI18n()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const loading = ref(false)
const tabModeDimension = ref<InventoryListTabModeDimension>(readInventoryListTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)
const list = ref<InventoryOverview[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
/** 库存类型 1/2/3，空为全部（仅前端筛选当前已加载总览） */
const stockTypeFilter = ref<number | undefined>(undefined)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
/** 筛选总览用的仓库主键或编码（与 stock.warehouseId 一致） */
const warehouseFilter = ref<string | undefined>(undefined)
const materialModelFilter = ref('')
const stockCodeFilter = ref('')
const finance = ref<FinanceSummary | null>(null)
const warehouses = ref<WarehouseInfo[]>([])

const TAB_MODE_FILTER_I18N: Record<Exclude<InventoryListTabModeDimension, 'off'>, string> = {
  stockType: 'inventoryList.filters.stockType',
  warehouse: 'inventoryList.filters.warehouse'
}

function tabModeDimensionLabel(dim: Exclude<InventoryListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeInventoryListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<InventoryListTabModeDimension, 'off'>) {
  if (dim === 'warehouse' && !warehouseTabModeAllowed.value) {
    ElMessage.warning(
      t('inventoryList.settingsMenu.warehouseTabDisabled', { max: INVENTORY_WAREHOUSE_TAB_MAX })
    )
    return
  }
  tabModeDimension.value = dim
  writeInventoryListTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const inventoryTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'stockType', label: t('inventoryList.columns.stockType'), width: 138, showOverflowTooltip: true },
  { key: 'materialModel', label: t('inventoryList.columns.materialModel'), minWidth: 160, showOverflowTooltip: true },
  { key: 'materialBrand', label: t('inventoryList.columns.brand'), minWidth: 120, showOverflowTooltip: true },
  { key: 'onHandQty', label: t('inventoryList.columns.onHandQty'), prop: 'onHandQty', width: 110, align: 'right' },
  { key: 'availableQty', label: t('inventoryList.columns.availableQty'), prop: 'availableQty', width: 110, align: 'right' },
  { key: 'lockedQty', label: t('inventoryList.columns.lockedQty'), prop: 'lockedQty', width: 110, align: 'right' },
  { key: 'inventoryAmount', label: t('inventoryList.columns.inventoryAmount'), prop: 'inventoryAmount', width: 120, align: 'right' },
  { key: 'warehouseName', label: t('inventoryList.columns.warehouseName'), width: 160, showOverflowTooltip: true },
  { key: 'region', label: t('inventoryList.columns.region'), width: 100, minWidth: 100, align: 'center', showOverflowTooltip: false },
  { key: 'lastMoveTime', label: t('inventoryList.columns.lastMoveTime'), prop: 'lastMoveTime', width: 170 },
  { key: 'stockCode', label: t('inventoryList.columns.stockCode'), width: 132, showOverflowTooltip: true },
  { key: 'createTime', label: t('inventoryList.columns.createTime'), width: 160 },
  { key: 'createUser', label: t('inventoryList.columns.createUser'), width: 120, showOverflowTooltip: true }
])

/** 兼容接口 camelCase / PascalCase */
function normalizeWarehouseRow(row: WarehouseInfo): WarehouseInfo {
  const r = row as unknown as Record<string, unknown>
  const idRaw = r.id ?? r.Id
  const id = typeof idRaw === 'string' && idRaw.trim() ? idRaw.trim() : undefined
  const code = String(r.warehouseCode ?? r.WarehouseCode ?? '').trim()
  const name = String(r.warehouseName ?? r.WarehouseName ?? '').trim()
  const addr = String(r.address ?? r.Address ?? '')
  const st = r.status ?? r.Status
  const status = typeof st === 'number' ? st : 1
  const regionType = normalizeRegionType(r.regionType ?? r.RegionType)
  return { id, warehouseCode: code, warehouseName: name, address: addr, regionType, status }
}

/** 库存编号下拉：仓库编码 + 名称，值为 id 优先否则编码 */
const warehouseSelectOptions = computed(() => {
  const rows = warehouses.value.map(normalizeWarehouseRow)
  const opts = rows
    .map((n) => {
      const value = (n.id?.trim() || n.warehouseCode || '').trim()
      if (!value) return null
      const code = n.warehouseCode?.trim()
      const name = n.warehouseName?.trim()
      const label =
        code && name ? `${code} · ${name}` : code || name || value
      return { value, label }
    })
    .filter((x): x is { value: string; label: string } => x != null)
  const byVal = new Map<string, { value: string; label: string }>()
  for (const o of opts) {
    if (!byVal.has(o.value)) byVal.set(o.value, o)
  }
  return [...byVal.values()].sort((a, b) => a.label.localeCompare(b.label, 'zh-CN'))
})

const warehouseTabModeAllowed = computed(() =>
  isWarehouseTabModeAllowed(warehouseSelectOptions.value.length)
)

/** 偏好为仓库且仓数 ≤10 时才用页签条；>10 强制回退下拉（偏好可保留） */
const warehouseTabStripActive = computed(
  () => tabModeDimension.value === 'warehouse' && warehouseTabModeAllowed.value
)

const filterTabStripVisible = computed(
  () =>
    tabModeDimension.value === 'stockType' ||
    (tabModeDimension.value === 'warehouse' && warehouseTabModeAllowed.value)
)

const filterTabStripAriaLabel = computed(() => {
  if (tabModeDimension.value === 'off') return ''
  if (tabModeDimension.value === 'warehouse' && !warehouseTabModeAllowed.value) return ''
  return tabModeDimensionLabel(tabModeDimension.value)
})

function stockTypeFilterLabel(n: number) {
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  if (n === 1) return t('inventoryList.stockTypes.customer')
  return t('inventoryList.stockTypes.unknown')
}

type InvFilterTabId = InvStockTypeTabId | InvWarehouseTabId

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'stockType') {
    return [
      { id: 'all' as const, label: t('inventoryList.filterTabs.all') },
      ...INV_STOCK_TYPE_TAB_VALUES.map((value) => ({
        id: String(value) as InvStockTypeTabId,
        label: stockTypeFilterLabel(value)
      }))
    ]
  }
  if (dim === 'warehouse' && warehouseTabModeAllowed.value) {
    return [
      { id: 'all' as const, label: t('inventoryList.filterTabs.all') },
      ...warehouseSelectOptions.value.map((opt) => ({
        id: opt.value as InvWarehouseTabId,
        label: opt.label
      }))
    ]
  }
  return [] as Array<{ id: InvFilterTabId; label: string }>
})

const activeFilterTabId = computed((): InvFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'stockType') return invStockTypeFilterToTab(stockTypeFilter.value)
  if (dim === 'warehouse') return invWarehouseFilterToTab(warehouseFilter.value)
  return 'all'
})

function onFilterTabClick(tab: InvFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'stockType') {
    const next = invStockTypeTabToFilter(tab as InvStockTypeTabId)
    if (stockTypeFilter.value === next) return
    stockTypeFilter.value = next
    void fetchList()
    return
  }
  if (dim === 'warehouse' && warehouseTabModeAllowed.value) {
    const next = invWarehouseTabToFilter(tab as InvWarehouseTabId)
    if ((warehouseFilter.value ?? undefined) === next) return
    warehouseFilter.value = next
    void fetchList()
  }
}

function rowStockTypeNum(row: InventoryOverview): number {
  const r = row as unknown as Record<string, unknown>
  const n = Number(r.stockType ?? r.StockType ?? 1)
  return n >= 1 && n <= 3 ? n : 1
}

const stockTypeLabel = (row: InventoryOverview) => {
  const n = rowStockTypeNum(row)
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  if (n === 1) return t('inventoryList.stockTypes.customer')
  return t('inventoryList.stockTypes.unknown')
}

watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

function onInventoryPageSizeChange() {
  void runInventoryFetch(true)
}

function onInventoryPageChange() {
  void runInventoryFetch(false)
}

function resetInventorySearch() {
  stockTypeFilter.value = undefined
  warehouseFilter.value = undefined
  materialModelFilter.value = ''
  stockCodeFilter.value = ''
  void fetchList()
}

const formatMoney = (v: number) =>
  v == null
    ? '—'
    : Number(v).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const financeByCurrencyRows = computed(() => (finance.value?.currencyBreakdowns ?? []).slice().sort((a, b) => a.currency - b.currency))
const currencyCodeText = (currency?: number) => CURRENCY_CODE_TO_TEXT[Number(currency) || 1] ?? 'RMB'

/** 数量列：与《业务列表规范》§3.2 一致（千分位、tabular） */
const formatQtyCell = (v: unknown) => {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

const inventoryAmountHasValue = (v: unknown) => {
  if (v == null || v === '') return false
  const n = Number(v)
  return Number.isFinite(n)
}

/** 列表金额拆段（formatToParts），与 RFQ 采购报价阶梯一致 */
const splitInventoryMoneyParts = (n: number): { intPart: string; fracPart: string } => {
  const parts = new Intl.NumberFormat('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).formatToParts(n)
  let intPart = ''
  let fracPart = ''
  for (const p of parts) {
    if (p.type === 'integer' || p.type === 'group') intPart += p.value
    else if (p.type === 'decimal' || p.type === 'fraction') fracPart += p.value
  }
  if (!fracPart) {
    const fallback = n.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    return { intPart: fallback, fracPart: '' }
  }
  return { intPart, fracPart }
}

/** 币别枚举 → 三位字母（与采购明细 currency 一致） */
const inventoryCurrencyIso = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const codeNum = Number(r.currency ?? r.Currency ?? 1)
  return CURRENCY_CODE_TO_TEXT[Number.isFinite(codeNum) ? codeNum : 1] ?? 'RMB'
}

/** 币别色 class（与 crm-quote-tier-dock.scss 一致） */
const inventoryCurrencyClass = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const n = Number(r.currency ?? r.Currency ?? 1)
  if (n === 2) return 'dock-tier-ccy--usd'
  if (n === 3) return 'dock-tier-ccy--eur'
  if (n === 4) return 'dock-tier-ccy--hkd'
  if (n === 1 || !Number.isFinite(n) || n === 0) return 'dock-tier-ccy--rmb'
  return 'dock-tier-ccy--purple'
}
/** 列表「地域」：stock.RegionType（接口 camelCase / PascalCase） */
const regionLabel = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

/** 与全库库存明细列表地域列样式一致（胶囊 domestic / overseas） */
function regionTypeKind(row: InventoryOverview): 'domestic' | 'overseas' {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
}

const warehouseNameOf = (warehouseId?: string) => {
  if (!warehouseId) return '—'
  const byId = warehouses.value.find(w => normalizeWarehouseRow(w).id === warehouseId)
  if (byId) {
    const n = normalizeWarehouseRow(byId)
    if (n.warehouseName) return n.warehouseName
  }
  const byCode = warehouses.value.find(w => normalizeWarehouseRow(w).warehouseCode === warehouseId.trim())
  if (byCode) {
    const n = normalizeWarehouseRow(byCode)
    if (n.warehouseName) return n.warehouseName
  }
  return warehouseId
}

function pickRowStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal]
  return typeof v === 'string' ? v : ''
}

const stockCodeDisplay = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const code = pickRowStr(r, 'stockCode', 'StockCode').trim()
  return code || '—'
}

/** 规格型号可复制原文；兼容 PascalCase；无型号时回退物料 ID */
const materialModelCopyValue = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const model = pickRowStr(r, 'materialModel', 'MaterialModel').trim()
  const id = pickRowStr(r, 'materialId', 'MaterialId').trim()
  return model || id
}

/** 品牌可复制原文（接口 materialName：优先 stock 冗余 purchase_brand）；兼容 PascalCase */
const materialBrandCopyValue = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  return pickRowStr(r, 'materialName', 'MaterialName').trim()
}

async function runInventoryFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const [overviewRes, summaryRes, warehouseRes] = await Promise.allSettled([
      inventoryCenterApi.getOverviewPaged({
        warehouseId: warehouseFilter.value?.trim() || undefined,
        materialModel: materialModelFilter.value?.trim() || undefined,
        stockCode: stockCodeFilter.value?.trim() || undefined,
        stockType: stockTypeFilter.value,
        page: listPage.value,
        pageSize: listPageSize.value
      }),
      inventoryCenterApi.getFinanceSummary({
        warehouseId: warehouseFilter.value?.trim() || undefined,
        materialModel: materialModelFilter.value?.trim() || undefined,
        stockCode: stockCodeFilter.value?.trim() || undefined,
        stockType: stockTypeFilter.value
      }),
      inventoryCenterApi.getWarehouses()
    ])

    if (overviewRes.status === 'fulfilled') {
      list.value = overviewRes.value.items
      listTotal.value = overviewRes.value.total
    } else {
      list.value = []
      listTotal.value = 0
      ElMessage.error(getApiErrorMessage(overviewRes.reason, t('inventoryList.messages.loadOverviewFailed')))
    }

    if (summaryRes.status === 'fulfilled') {
      finance.value = summaryRes.value
    } else {
      finance.value = null
      ElMessage.warning(getApiErrorMessage(summaryRes.reason, t('inventoryList.messages.loadFinanceFailed')))
    }

    if (warehouseRes.status === 'fulfilled') {
      warehouses.value = warehouseRes.value
    }
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryList.messages.loadCenterFailed')))
    list.value = []
    listTotal.value = 0
  } finally {
    loading.value = false
  }
}

const fetchList = () => void runInventoryFetch(true)

const openStockDetail = (row: InventoryOverview) => {
  const sid = (row.stockId || '').trim()
  if (!sid) {
    ElMessage.warning(t('inventoryList.messages.missingStockId'))
    return
  }
  router.push({
    path: `/inventory/stocks/${encodeURIComponent(sid)}`,
    query: {
      materialId: row.materialId || undefined,
      stockCode: row.stockCode || undefined,
      materialModel: row.materialModel || undefined,
      materialBrand: row.materialName || undefined,
      warehouseId: row.warehouseId || undefined
    }
  })
}

const onRowClick = (row: InventoryOverview) => {
  openStockDetail(row)
}

const goWarehouseManage = () => {
  router.push({ name: 'WarehouseManage' })
}

onMounted(() => void fetchList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right {
    display: flex;
    align-items: center;
    flex-shrink: 0;
  }
}

// ---- 搜索栏（与客户列表 CustomerList.vue 一致）----
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

.list-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  white-space: nowrap;
}

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
}

/** 《业务列表规范》§3.2：数量字重与字色 */
.inv-list-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .inv-list-qty {
  color: $text-primary;
}

.inv-list-dash {
  color: $text-muted;
}

/** §3.1：金额拆段 + 币别（dock-tier-ccy* 为全局样式） */
.inv-list-amount-cell {
  display: inline-flex;
  align-items: baseline;
  justify-content: flex-end;
  flex-wrap: nowrap;
  width: 100%;
  font-size: 12px;
  line-height: 1.4;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.inv-list-amt-int,
.inv-list-amt-frac {
  font-weight: 700;
  color: #27292c;
}

html[data-theme='dark'] .inv-list-amt-int,
html[data-theme='dark'] .inv-list-amt-frac {
  color: $text-primary;
}

/** §2.5：紧密下列内库存类型单行 */
:deep(.crm-items-table--density-compact) .inv-stock-type-cell {
  flex-wrap: nowrap;
  white-space: nowrap;
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

.status-select--inv-order {
  width: 148px;
}

.status-select--inv-warehouse {
  width: 220px;
}

.search-input {
  width: 180px;
  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}

.search-input--material-model {
  width: 220px;
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
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

.inv-stock-type-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.inv-stock-type-cell--stocking {
  color: #ffc107;
  font-weight: 600;
}
.inv-stock-type-icon {
  font-size: 16px;
  flex-shrink: 0;
}

.region-type-chip {
  display: inline-flex;
  align-items: center;
  gap: 0;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.2;
}

.region-type-chip--domestic {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.14);
}

.region-type-chip--overseas {
  color: #409eff;
  background: rgba(64, 158, 255, 0.14);
}

.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
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

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
}
.stat-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  margin-bottom: 12px;
}
.stat-card {
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 10px 12px;
  .label {
    color: $text-muted;
    font-size: 12px;
  }
  .value {
    color: $cyan-primary;
    font-size: 18px;
    font-weight: 600;
    margin-top: 4px;
  }
}

.stat-subrows {
  margin-top: 8px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.stat-subrow {
  display: flex;
  justify-content: space-between;
  color: #8fa2b7;
  font-size: 12px;
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

.btn-icon-only {
  width: 32px;
  padding-left: 0;
  padding-right: 0;
  justify-content: center;
}

.inv-main-panel {
  width: 100%;
}

.inv-main-panel--with-filter-tabs {
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

.inv-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.inv-filter-tabs__item {
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

html[data-theme='dark'] .inv-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}

</style>

<style lang="scss">
.inv-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.inv-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.inv-list-settings-menu__item {
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

  &:disabled,
  &.is-disabled {
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

.inv-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.inv-list-settings-menu__submenu {
  position: relative;
}

.inv-list-settings-menu__flyout {
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
