<template>
  <div class="inventory-on-hand-page">
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
          <h1 class="page-title">{{ t('inventoryOnHandList.title') }}</h1>
        </div>
        <div v-if="viewMode === 'list'" class="count-badge">{{ t('inventoryOnHandList.count', { count: listTotal }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-write-off-desktop" @click="goInventoryBucket">
          <span>{{ t('inventoryOnHandList.goBucket') }}</span>
          <el-icon class="btn-write-off-desktop__arrow"><ArrowRight /></el-icon>
        </button>
      </div>
    </div>

    <el-row v-show="viewMode === 'list'" :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value inv-stat-qty">{{ formatQtyCell(onHandQtyTotal) }}</div>
          <div class="stat-label">{{ t('inventoryOnHandList.stats.onHandQty') }}</div>
        </el-card>
      </el-col>
      <el-col
        v-for="row in totalAmounts"
        :key="'stat-amt-' + row.currency"
        :span="6"
      >
        <el-card class="stat-card stat-info">
          <div v-if="maskPurchaseSensitiveFields" class="stat-value inv-stat-dash">—</div>
          <div v-else class="stat-value inv-stat-amount dock-tier-price-line">
            <template v-for="amt in [splitInventoryMoneyParts(Number(row.amount) || 0)]" :key="'stat-amt-parts-' + row.currency">
              <span class="inv-list-amt">
                <span class="inv-list-amt-int">{{ amt.intPart }}</span><span class="inv-list-amt-frac">{{ amt.fracPart }}</span>
              </span>
            </template>
            <span class="dock-tier-ccy-gap">&nbsp;</span>
            <span :class="['dock-tier-ccy', currencyClass(Number(row.currency))]">{{ currencyIso(Number(row.currency)) }}</span>
          </div>
          <div class="stat-label">{{ t('inventoryOnHandList.stats.amountByCurrency', { currency: currencyIso(Number(row.currency)) }) }}</div>
        </el-card>
      </el-col>
    </el-row>

    <div class="split-bar">
      <span class="split-bar-label">{{ t('inventoryOnHandList.split.label') }}</span>
      <label class="split-check">
        <input
          type="checkbox"
          :checked="splitStockType"
          @change="onToggleStockTypeSplit(($event.target as HTMLInputElement).checked)"
        />
        <span>{{ t('inventoryList.filters.stockType') }}</span>
      </label>
      <label class="split-check">
        <input
          type="checkbox"
          :checked="splitWarehouse"
          @change="onToggleWarehouseSplit(($event.target as HTMLInputElement).checked)"
        />
        <span>{{ t('inventoryList.filters.warehouse') }}</span>
      </label>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model.trim="materialModelFilter"
          :placeholder="t('inventoryList.filters.materialModelPlaceholder')"
          clearable
          class="search-input search-input--material-model"
          @keyup.enter="fetchList"
        />
        <el-input
          v-model.trim="brandFilter"
          :placeholder="t('inventoryOnHandList.filters.brandPlaceholder')"
          clearable
          class="search-input search-input--brand"
          @keyup.enter="fetchList"
        />
        <el-select
          v-if="splitStockType"
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
          v-if="splitWarehouse"
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
        <button type="button" class="btn-primary btn-sm" @click="fetchList">
          {{ t('inventoryList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="resetSearch">
          {{ t('inventoryList.filters.reset') }}
        </button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('inventoryOnHandList.filters.listView') : t('inventoryOnHandList.filters.boardView') }}
        </button>
      </div>
    </div>

    <InventoryOnHandListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <CrmDataTable
      v-show="viewMode === 'list'"
      ref="dataTableRef"
      column-layout-key="inventory-on-hand-list-v2"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
    >
      <template #col-materialModel="{ row }">
        <CrmListCopyableTextCell :text="(row.materialModel || '').trim()" />
      </template>
      <template #col-purchaseBrand="{ row }">
        <CrmListCopyableTextCell :text="(row.purchaseBrand || '').trim()" />
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
      <template #col-warehouse="{ row }">{{ warehouseLabel(row) }}</template>
      <template #col-onHandQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.onHandQty) }}</span>
      </template>
      <template v-for="ccy in currencies" :key="'amt-slot-' + ccy" #[`col-amount-${ccy}`]="{ row }">
        <template v-if="maskPurchaseSensitiveFields">
          <span class="inv-list-dash">—</span>
        </template>
        <template v-else-if="!inventoryAmountHasValue(amountOf(row, ccy))">
          <span class="inv-list-dash">—</span>
        </template>
        <div v-else class="inv-list-amount-cell dock-tier-price-line">
          <template v-for="amt in [splitInventoryMoneyParts(Number(amountOf(row, ccy)))]" :key="'amt-' + ccy">
            <span class="inv-list-amt">
              <span class="inv-list-amt-int">{{ amt.intPart }}</span><span class="inv-list-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', currencyClass(ccy)]">{{ currencyIso(ccy) }}</span>
        </div>
      </template>
    </CrmDataTable>
    <div v-show="viewMode === 'list'" class="pagination-wrapper">
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
        @size-change="onPageSizeChange"
        @current-change="onPageChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { ArrowRight, Box, Setting } from '@element-plus/icons-vue'
import InventoryOnHandListBoard from '@/views/Inventory/InventoryOnHandListBoard.vue'
import { useListBoardHelpOverride } from '@/composables/useHelpDocOverride'
import type { InventoryOnHandListAnalyticsQuery } from '@/api/inventoryOnHandAnalytics'
import {
  inventoryCenterApi,
  type InventoryOnHandAmount,
  type InventoryOnHandSummaryRow,
  type WarehouseInfo
} from '@/api/inventoryCenter'
import { readInventoryOnHandSplit, writeInventoryOnHandSplit } from '@/utils/inventoryOnHandSplit'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { getApiErrorMessage } from '@/utils/apiError'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { normalizeRegionType } from '@/constants/regionType'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t } = useI18n()
const router = useRouter()
const viewMode = ref<'list' | 'board'>('list')
useListBoardHelpOverride('pages/库存中心看板_MENU_INVENTORY_LIST_BOARD.md', viewMode)
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const loading = ref(false)
const list = ref<InventoryOnHandSummaryRow[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
const currencies = ref<number[]>([])
const onHandQtyTotal = ref(0)
const totalAmounts = ref<InventoryOnHandAmount[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const initialSplit = readInventoryOnHandSplit()
const splitStockType = ref(initialSplit.stockType)
const splitWarehouse = ref(initialSplit.warehouse)
const materialModelFilter = ref('')
const brandFilter = ref('')
const stockTypeFilter = ref<number | undefined>(undefined)
const warehouseFilter = ref<string | undefined>(undefined)
const warehouses = ref<WarehouseInfo[]>([])

const boardFilters = computed<InventoryOnHandListAnalyticsQuery>(() => ({
  materialModel: materialModelFilter.value.trim() || undefined,
  purchaseBrand: brandFilter.value.trim() || undefined,
  stockType: splitStockType.value ? stockTypeFilter.value : undefined,
  warehouseId: splitWarehouse.value ? warehouseFilter.value?.trim() || undefined : undefined
}))

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
  if (viewMode.value === 'list') void runFetch(false)
}

function persistSplit() {
  writeInventoryOnHandSplit({
    stockType: splitStockType.value,
    warehouse: splitWarehouse.value
  })
}

function onToggleStockTypeSplit(checked: boolean) {
  splitStockType.value = checked
  if (!checked) stockTypeFilter.value = undefined
  persistSplit()
  void fetchList()
}

function onToggleWarehouseSplit(checked: boolean) {
  splitWarehouse.value = checked
  if (!checked) warehouseFilter.value = undefined
  persistSplit()
  void fetchList()
}

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

const warehouseSelectOptions = computed(() => {
  const rows = warehouses.value.map(normalizeWarehouseRow)
  const opts = rows
    .map((n) => {
      const value = (n.id?.trim() || n.warehouseCode || '').trim()
      if (!value) return null
      const code = n.warehouseCode?.trim()
      const name = n.warehouseName?.trim()
      const label = code && name ? `${code} · ${name}` : code || name || value
      return { value, label }
    })
    .filter((x): x is { value: string; label: string } => x != null)
  const byVal = new Map<string, { value: string; label: string }>()
  for (const o of opts) {
    if (!byVal.has(o.value)) byVal.set(o.value, o)
  }
  return [...byVal.values()].sort((a, b) => a.label.localeCompare(b.label, 'zh-CN'))
})

/** 表头估算里 ASCII 空格约 7px；列宽 = 可显示内容 + 4 个空格。 */
const TEXT_COL_SPACE4_PX = 7 * 4
const TEXT_COL_CELL_PAD_PX = 28

function estimatePlainTextWidthPx(text: string): number {
  let w = 0
  for (const ch of text) {
    w += (ch.codePointAt(0) ?? 0) > 0x7f ? 13 : 7
  }
  return w
}

function fitTextColumnWidth(label: string, cellTexts: string[]): number {
  const headerW = estimateListColumnHeaderMinWidth(label, { extra: TEXT_COL_SPACE4_PX })
  let maxBody = 0
  for (const raw of cellTexts) {
    const value = raw.trim()
    if (!value) continue
    maxBody = Math.max(maxBody, estimatePlainTextWidthPx(value) + TEXT_COL_CELL_PAD_PX + TEXT_COL_SPACE4_PX)
  }
  return Math.max(headerW, Math.ceil(maxBody))
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const modelLabel = t('inventoryList.columns.materialModel')
  const brandLabel = t('inventoryList.columns.brand')
  const modelW = fitTextColumnWidth(
    modelLabel,
    list.value.map((r) => (r.materialModel || '').trim())
  )
  const brandW = fitTextColumnWidth(
    brandLabel,
    list.value.map((r) => (r.purchaseBrand || '').trim())
  )
  const cols: CrmTableColumnDef[] = [
    { key: 'materialModel', label: modelLabel, width: modelW, minWidth: modelW, showOverflowTooltip: true },
    { key: 'purchaseBrand', label: brandLabel, width: brandW, minWidth: brandW, showOverflowTooltip: true }
  ]
  if (splitStockType.value) {
    cols.push({ key: 'stockType', label: t('inventoryList.columns.stockType'), width: 138, showOverflowTooltip: true })
  }
  if (splitWarehouse.value) {
    cols.push({ key: 'warehouse', label: t('inventoryList.columns.warehouseName'), width: 180, showOverflowTooltip: true })
  }
  cols.push({ key: 'onHandQty', label: t('inventoryList.columns.onHandQty'), prop: 'onHandQty', width: 110, align: 'right' })
  for (const ccy of currencies.value) {
    const iso = CURRENCY_CODE_TO_TEXT[ccy] ?? String(ccy)
    cols.push({
      key: `amount-${ccy}`,
      label: t('inventoryOnHandList.columns.amountByCurrency', { currency: iso }),
      width: 150,
      align: 'right'
    })
  }
  cols.push({
    key: 'flexGutter',
    label: '',
    minWidth: 1,
    hideable: false,
    reorderable: false,
    pinned: 'end',
    resizable: false,
    className: 'inv-on-hand-flex-col',
    labelClassName: 'inv-on-hand-flex-col'
  })
  return cols
})

function rowStockTypeNum(row: InventoryOnHandSummaryRow): number {
  const n = Number(row.stockType ?? 0)
  return n >= 1 && n <= 3 ? n : 0
}

function stockTypeLabel(row: InventoryOnHandSummaryRow) {
  const n = rowStockTypeNum(row)
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  if (n === 1) return t('inventoryList.stockTypes.customer')
  return t('inventoryList.stockTypes.unknown')
}

function warehouseLabel(row: InventoryOnHandSummaryRow) {
  const code = (row.warehouseCode || '').trim()
  const name = (row.warehouseName || '').trim()
  if (code && name) return `${code} · ${name}`
  return name || code || (row.warehouseId || '').trim() || '—'
}

function amountOf(row: InventoryOnHandSummaryRow, currency: number): number | null {
  const hit = (row.amounts ?? []).find((a) => Number(a.currency) === currency)
  if (!hit) return 0
  return Number(hit.amount)
}

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

const currencyIso = (ccy: number) => CURRENCY_CODE_TO_TEXT[ccy] ?? String(ccy)

const currencyClass = (n: number) => {
  if (n === 2) return 'dock-tier-ccy--usd'
  if (n === 3) return 'dock-tier-ccy--eur'
  if (n === 4) return 'dock-tier-ccy--hkd'
  if (n === 1 || !Number.isFinite(n) || n === 0) return 'dock-tier-ccy--rmb'
  return 'dock-tier-ccy--purple'
}

watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

function onPageSizeChange() {
  void runFetch(true)
}

function onPageChange() {
  void runFetch(false)
}

function resetSearch() {
  materialModelFilter.value = ''
  brandFilter.value = ''
  stockTypeFilter.value = undefined
  warehouseFilter.value = undefined
  void fetchList()
}

async function runFetch(resetPage: boolean) {
  if (viewMode.value === 'board') return
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const warehousePromise = splitWarehouse.value
      ? inventoryCenterApi.getWarehouses()
      : Promise.resolve([] as WarehouseInfo[])
    const [pageRes, warehouseRes] = await Promise.allSettled([
      inventoryCenterApi.getOnHandSummaryPaged({
        materialModel: materialModelFilter.value.trim() || undefined,
        purchaseBrand: brandFilter.value.trim() || undefined,
        stockType: splitStockType.value ? stockTypeFilter.value : undefined,
        warehouseId: splitWarehouse.value ? warehouseFilter.value?.trim() || undefined : undefined,
        groupByStockType: splitStockType.value,
        groupByWarehouse: splitWarehouse.value,
        page: listPage.value,
        pageSize: listPageSize.value
      }),
      warehousePromise
    ])

    if (pageRes.status === 'fulfilled') {
      list.value = pageRes.value.items
      listTotal.value = pageRes.value.total
      currencies.value = pageRes.value.currencies ?? []
      onHandQtyTotal.value = Number(pageRes.value.onHandQtyTotal ?? 0)
      totalAmounts.value = pageRes.value.totalAmounts ?? []
    } else {
      list.value = []
      listTotal.value = 0
      currencies.value = []
      onHandQtyTotal.value = 0
      totalAmounts.value = []
      ElMessage.error(getApiErrorMessage(pageRes.reason, t('inventoryOnHandList.messages.loadFailed')))
    }

    if (warehouseRes.status === 'fulfilled') {
      warehouses.value = warehouseRes.value
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('inventoryOnHandList.messages.loadFailed')))
    list.value = []
    listTotal.value = 0
    currencies.value = []
    onHandQtyTotal.value = 0
    totalAmounts.value = []
  } finally {
    loading.value = false
  }
}

const fetchList = () => void runFetch(true)

function goInventoryBucket() {
  void router.push('/inventory/bucket')
}

onMounted(() => void fetchList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-on-hand-page {
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
}
.page-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $text-primary;
  background: rgba(255, 255, 255, 0.06);
}
.page-title {
  margin: 0;
  font-size: 18px;
  font-weight: 650;
  color: $text-primary;
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
}

.btn-write-off-desktop {
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
  flex-shrink: 0;

  &:hover {
    background: #ddefff;
    color: #0f172a;
  }

  &:active {
    background: #d0e8ff;
  }

  &__arrow {
    font-size: 14px;
    color: #64748b;
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
  &.stat-info .stat-value {
    color: $info-color;
  }
  &.stat-info .inv-list-amt-int,
  &.stat-info .inv-list-amt-frac {
    color: $info-color;
  }
}
.inv-stat-qty {
  font-variant-numeric: tabular-nums;
}
.inv-stat-dash {
  color: $text-muted;
}
.inv-stat-amount {
  display: inline-flex;
  align-items: baseline;
  justify-content: center;
  flex-wrap: wrap;
  font-variant-numeric: tabular-nums;
}

.split-bar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 12px;
  padding: 8px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.06);
}
.split-bar-label {
  font-size: 12px;
  font-weight: 600;
  color: $text-muted;
}
.split-check {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: $text-primary;
  cursor: pointer;
  user-select: none;
  input {
    margin: 0;
    cursor: pointer;
  }
}

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
.search-input--brand {
  width: 160px;
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

.btn-primary,
.btn-ghost {
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
  background: transparent;
  border: 1px solid $border-panel;
  color: $text-muted;
  font-size: 12px;
  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.inv-list-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}
html[data-theme='dark'] .inv-list-qty {
  color: $text-primary;
}
.inv-list-dash { color: $text-muted; }
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
.inv-stock-type-cell {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.inv-stock-type-cell--stocking {
  color: #ffc107;
  font-weight: 600;
}
.inv-stock-type-icon {
  font-size: 14px;
}
:deep(.crm-items-table--density-compact) .inv-stock-type-cell {
  flex-wrap: nowrap;
  white-space: nowrap;
}

.pagination-wrapper {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 12px;
  gap: 12px;
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
.list-main-pagination {
  margin-left: auto;
}

:deep(.inv-on-hand-flex-col .cell) {
  padding: 0;
}
</style>
