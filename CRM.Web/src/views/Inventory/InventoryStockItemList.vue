<template>
  <!-- 业务列表页：结构对齐《业务页面规范》索引的《业务列表规范》《列表搜索栏规范》；表格皮肤见全局 crm-unified-list.scss -->
  <div class="inventory-stock-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z" />
              <polyline points="3.27 6.96 12 12.01 20.73 6.96" />
              <line x1="12" y1="22.08" x2="12" y2="12" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('inventoryStockItemList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('inventoryStockItemList.count', { count: listTotal }) }}</div>
      </div>
      <div class="header-right" aria-hidden="true" />
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filters.outboundStatus"
          clearable
          :placeholder="t('inventoryStockItemList.filters.outboundStatusAll')"
          class="status-select"
          :teleported="false"
          @change="fetchList"
        >
          <el-option :label="t('inventoryStockItemList.filters.outboundNone')" :value="1" />
          <el-option :label="t('inventoryStockItemList.filters.outboundPartial')" :value="2" />
          <el-option :label="t('inventoryStockItemList.filters.outboundDone')" :value="3" />
        </el-select>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.stockInCode"
            class="search-input search-input--keyword"
            :placeholder="t('inventoryStockItemList.filters.stockInCode')"
            @keyup.enter="fetchList"
          />
        </div>
        <div
          class="filter-date-range"
          role="group"
          :aria-label="t('inventoryStockItemList.filters.stockInDateRange')"
        >
          <el-date-picker
            v-model="dateFrom"
            type="date"
            value-format="YYYY-MM-DD"
            clearable
            :placeholder="t('inventoryStockItemList.filters.stockInDateFrom')"
            class="filter-date-range__picker filter-date-range__picker--start"
            :teleported="false"
            @change="fetchList"
          />
          <span class="filter-date-range__sep">{{ t('inventoryStockItemList.filters.stockInDateSep') }}</span>
          <el-date-picker
            v-model="dateTo"
            type="date"
            value-format="YYYY-MM-DD"
            clearable
            :placeholder="t('inventoryStockItemList.filters.stockInDateTo')"
            class="filter-date-range__picker filter-date-range__picker--end"
            :teleported="false"
            @change="fetchList"
          />
        </div>
        <el-select
          v-model="filters.warehouseId"
          clearable
          :placeholder="t('inventoryStockItemList.filters.warehouse')"
          class="status-select status-select--warehouse"
          :teleported="false"
          @change="fetchList"
        >
          <el-option
            v-for="w in warehouseOptions"
            :key="w.id"
            :label="warehouseOptionLabel(w)"
            :value="w.id"
          />
        </el-select>
        <input
          v-model="filters.purchasePn"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.purchasePn')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.purchaseBrand"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.purchaseBrand')"
          @keyup.enter="fetchList"
        />
        <template v-if="!maskSaleSensitiveFields">
          <input
            v-model="filters.customerName"
            class="search-input search-input--filter"
            :placeholder="t('inventoryStockItemList.filters.customerName')"
            @keyup.enter="fetchList"
          />
        </template>
        <template v-if="!maskPurchaseSensitiveFields">
          <input
            v-model="filters.vendorName"
            class="search-input search-input--filter"
            :placeholder="t('inventoryStockItemList.filters.vendorName')"
            @keyup.enter="fetchList"
          />
        </template>
        <template v-if="!maskSaleSensitiveFields">
          <el-select
            v-model="filters.salespersonUserId"
            clearable
            filterable
            :placeholder="t('rfqItemList.filters.allSalesUsers')"
            class="status-select status-select--sales"
            :teleported="false"
            @change="fetchList"
          >
            <el-option v-for="u in salesUsers" :key="u.id" :label="salesUserLabel(u)" :value="u.id" />
          </el-select>
        </template>
        <el-select
          v-model="filters.purchaserUserId"
          clearable
          filterable
          :placeholder="t('rfqItemList.filters.allPurchasers')"
          class="status-select status-select--purchaser"
          :teleported="false"
          @change="fetchList"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaseUserLabel(u)" :value="u.id" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="fetchList">{{ t('inventoryStockItemList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('inventoryStockItemList.filters.reset') }}</button>
      </div>
    </div>

    <!-- 主表：列设置齿轮 + 行高密度锚点见《业务列表规范》§1.2、§2.3；双击行见《列表双击进入详情规范》 -->
    <CrmDataTable
      ref="dataTableRef"
      class="inventory-stock-item-list-crm-table"
      column-layout-key="inventory-stock-item-list-main"
      :columns="stockItemTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedList"
      v-loading="loading"
      @row-dblclick="onRowDblclick"
    >
      <template #col-outboundStatus="{ row }">
        <span class="outbound-status-chip" :class="`outbound-status-chip--${outboundStatusKind(row.outboundStatus)}`">
          <span>{{ outboundLabel(row.outboundStatus) }}</span>
        </span>
      </template>
      <template #col-stockItemCode="{ row }">
        <span class="stock-item-code-with-badge">
          <span>{{ row.stockItemCode || '—' }}</span>
          <el-tag v-if="Number(row.stockType ?? 0) === 2" type="warning" effect="plain" size="small" round>备货</el-tag>
        </span>
      </template>
      <template #col-stockInDate="{ row }">
        <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.stockInDate)]" :key="'sid-' + row.stockItemId">
          <span v-if="!p" class="inv-list-dash">—</span>
          <span v-else-if="isTimeMidnightOnly(p.time)" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
          </span>
          <span v-else class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
        </template>
      </template>
      <template #col-warehouse="{ row }">{{ warehouseCell(row) }}</template>
      <template #col-regionType="{ row }">
        <span class="region-type-chip" :class="`region-type-chip--${regionTypeKind(row)}`">
          <span>{{ stockItemRegionLabel(row) }}</span>
        </span>
      </template>
      <template #col-qtyInbound="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.qtyInbound) }}</span>
      </template>
      <template #col-qtyStockOut="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.qtyStockOut) }}</span>
      </template>
      <template #col-qtyRepertory="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.qtyRepertory) }}</span>
      </template>
      <template #col-customerName="{ row }">
        <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() ? row.customerName : '—') }}</span>
      </template>
      <template #col-salespersonName="{ row }">
        <span>{{ maskSaleSensitiveFields ? '—' : (row.salespersonName?.trim() ? row.salespersonName : '—') }}</span>
      </template>
      <template #col-vendorName="{ row }">
        <span>{{ maskPurchaseSensitiveFields ? '—' : (row.vendorName?.trim() ? row.vendorName : '—') }}</span>
      </template>
      <template #col-profitOutBizUsd="{ row }">
        <span v-if="maskPurchaseSensitiveFields || maskSaleSensitiveFields" class="inv-list-dash">—</span>
        <template v-else-if="row.profitOutBizUsd == null">
          <span class="inv-list-dash">—</span>
        </template>
        <div v-else class="inv-list-amount-cell dock-tier-price-line">
          <template v-for="amt in [splitUsdMoneyParts(Number(row.profitOutBizUsd))]" :key="'p-' + row.stockItemId">
            <span class="inv-list-amt">
              <span class="inv-list-amt-int">{{ amt.intPart }}</span><span class="inv-list-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
        </div>
      </template>
    </CrmDataTable>
    <div class="pagination-wrapper">
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
        @size-change="listPage = 1"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { authApi, type PurchaseUserSelectOption, type SalesUserSelectOption } from '@/api/auth'
import { inventoryCenterApi, type StockItemListQuery, type StockItemListRow, type WarehouseInfo } from '@/api/inventoryCenter'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const router = useRouter()
const { t } = useI18n()
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const loading = ref(false)
const list = ref<StockItemListRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotal = computed(() => list.value.length)
const pagedList = computed(() => {
  const start = (listPage.value - 1) * listPageSize.value
  return list.value.slice(start, start + listPageSize.value)
})
watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

const stockItemTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'outboundStatus', label: t('inventoryStockItemList.columns.outboundStatus'), width: 110, align: 'center' },
  { key: 'stockItemCode', label: t('inventoryStockItemList.columns.stockItemCode'), prop: 'stockItemCode', width: 168, showOverflowTooltip: true },
  { key: 'stockInCode', label: t('inventoryStockItemList.columns.stockInCode'), prop: 'stockInCode', width: 150, showOverflowTooltip: true },
  { key: 'stockInDate', label: t('inventoryStockItemList.columns.stockInDate'), prop: 'stockInDate', width: 118 },
  { key: 'warehouse', label: t('inventoryStockItemList.columns.warehouse'), minWidth: 120, showOverflowTooltip: true },
  { key: 'regionType', label: t('inventoryStockItemList.columns.regionType'), width: 88, align: 'center', showOverflowTooltip: true },
  { key: 'purchasePn', label: t('inventoryStockItemList.columns.purchasePn'), prop: 'purchasePn', minWidth: 130, showOverflowTooltip: true },
  { key: 'purchaseBrand', label: t('inventoryStockItemList.columns.purchaseBrand'), prop: 'purchaseBrand', minWidth: 100, showOverflowTooltip: true },
  { key: 'qtyInbound', label: t('inventoryStockItemList.columns.qtyInbound'), prop: 'qtyInbound', width: 88, align: 'right' },
  { key: 'qtyStockOut', label: t('inventoryStockItemList.columns.qtyStockOut'), prop: 'qtyStockOut', width: 88, align: 'right' },
  { key: 'qtyRepertory', label: t('inventoryStockItemList.columns.qtyRepertory'), prop: 'qtyRepertory', width: 88, align: 'right' },
  { key: 'vendorName', label: t('inventoryStockItemList.columns.vendorName'), prop: 'vendorName', minWidth: 120, showOverflowTooltip: true },
  { key: 'purchaserName', label: t('inventoryStockItemList.columns.purchaserName'), prop: 'purchaserName', width: 100, showOverflowTooltip: true },
  { key: 'purchaseOrderItemCode', label: t('inventoryStockItemList.columns.purchaseOrderItemCode'), prop: 'purchaseOrderItemCode', width: 136, showOverflowTooltip: true },
  { key: 'customerName', label: t('inventoryStockItemList.columns.customerName'), prop: 'customerName', minWidth: 120, showOverflowTooltip: true },
  { key: 'salespersonName', label: t('inventoryStockItemList.columns.salespersonName'), prop: 'salespersonName', width: 100, showOverflowTooltip: true },
  { key: 'sellOrderItemCode', label: t('inventoryStockItemList.columns.sellOrderItemCode'), prop: 'sellOrderItemCode', width: 120, showOverflowTooltip: true },
  { key: 'batchNo', label: t('inventoryStockItemList.columns.batchNo'), prop: 'batchNo', width: 100, showOverflowTooltip: true },
  { key: 'locationId', label: t('inventoryStockItemList.columns.locationId'), prop: 'locationId', minWidth: 100, showOverflowTooltip: true },
  { key: 'profitOutBizUsd', label: t('inventoryStockItemList.columns.profitOutBizUsd'), prop: 'profitOutBizUsd', width: 148, align: 'right' }
])
const dateFrom = ref<string | null>(null)
const dateTo = ref<string | null>(null)
const salesUsers = ref<SalesUserSelectOption[]>([])
const purchaseUsers = ref<PurchaseUserSelectOption[]>([])
const warehouses = ref<WarehouseInfo[]>([])
const warehouseOptions = computed(() => warehouses.value.filter((w) => !!(w.id && String(w.id).trim())))

const filters = reactive({
  stockInCode: '',
  purchasePn: '',
  purchaseBrand: '',
  warehouseId: '',
  outboundStatus: undefined as number | undefined,
  customerName: '',
  vendorName: '',
  salespersonUserId: undefined as string | undefined,
  purchaserUserId: undefined as string | undefined
})

function salesUserLabel(u: SalesUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}(${u.userName})` : name
}

function purchaseUserLabel(u: PurchaseUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}(${u.userName})` : name
}

function buildQuery(): StockItemListQuery {
  const q: StockItemListQuery = {
    stockInCode: filters.stockInCode.trim() || undefined,
    stockInDateFrom: dateFrom.value?.trim() || undefined,
    stockInDateTo: dateTo.value?.trim() || undefined,
    warehouseId: filters.warehouseId.trim() || undefined,
    purchasePn: filters.purchasePn.trim() || undefined,
    purchaseBrand: filters.purchaseBrand.trim() || undefined,
    outboundStatus: filters.outboundStatus,
    vendorName: filters.vendorName.trim() || undefined,
    purchaserUserId: filters.purchaserUserId?.trim() || undefined
  }
  if (!maskSaleSensitiveFields.value) {
    q.customerName = filters.customerName.trim() || undefined
    q.salespersonUserId = filters.salespersonUserId?.trim() || undefined
  }
  return q
}

async function runStockItemFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    list.value = await inventoryCenterApi.searchStockItems(buildQuery())
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryStockItemList.messages.loadFailed')))
    list.value = []
  } finally {
    loading.value = false
  }
}

const fetchList = () => void runStockItemFetch(true)

const resetFilters = () => {
  filters.stockInCode = ''
  filters.purchasePn = ''
  filters.purchaseBrand = ''
  filters.warehouseId = ''
  filters.outboundStatus = undefined
  filters.customerName = ''
  filters.vendorName = ''
  filters.salespersonUserId = undefined
  filters.purchaserUserId = undefined
  dateFrom.value = null
  dateTo.value = null
  void fetchList()
}

/** 仅日期（无时区时刻）时入库日常为 00:00，不重复展示时分 */
function isTimeMidnightOnly(time: string) {
  const t0 = (time || '').trim()
  return t0 === '00:00' || t0.startsWith('00:00:')
}

/** 数量列：与《业务列表规范》§3.2 一致 */
const stockItemRegionLabel = (row: StockItemListRow) => {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

const regionTypeKind = (row: StockItemListRow): 'domestic' | 'overseas' => {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
}

const formatQtyCell = (v: unknown) => {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

const splitUsdMoneyParts = (n: number): { intPart: string; fracPart: string } => {
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
  if (!fracPart) fracPart = '.00'
  return { intPart, fracPart }
}

const outboundLabel = (s: number) => {
  if (s === 1) return t('inventoryStockItemList.filters.outboundNone')
  if (s === 2) return t('inventoryStockItemList.filters.outboundPartial')
  if (s === 3) return t('inventoryStockItemList.filters.outboundDone')
  return '—'
}

const outboundStatusKind = (s: number): 'none' | 'partial' | 'done' | 'unknown' => {
  if (s === 1) return 'none'
  if (s === 2) return 'partial'
  if (s === 3) return 'done'
  return 'unknown'
}

const warehouseOptionLabel = (w: WarehouseInfo) => {
  const name = (w.warehouseName || '').trim()
  const code = (w.warehouseCode || '').trim()
  if (name && code) return `${name}（${code}）`
  return name || code || '—'
}

const warehouseCell = (row: StockItemListRow) => {
  const code = row.warehouseCode?.trim()
  if (code) return code
  return t('quoteList.na')
}

const onRowDblclick = (row: StockItemListRow) => {
  const sid = (row.stockAggregateId || '').trim()
  if (!sid) {
    ElMessage.warning(t('inventoryStockItemList.messages.missingAggregateId'))
    return
  }
  router.push({
    path: `/inventory/stocks/${encodeURIComponent(sid)}`,
    query: {
      materialId: row.materialId || undefined,
      stockCode: undefined,
      materialModel: row.purchasePn || undefined,
      materialBrand: row.purchaseBrand || undefined,
      warehouseId: row.warehouseId || undefined
    }
  })
}

onMounted(async () => {
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch {
    warehouses.value = []
  }
  try {
    salesUsers.value = await authApi.getSalesUsersForSelect()
  } catch {
    salesUsers.value = []
  }
  try {
    purchaseUsers.value = await authApi.getPurchaseUsersForSelect()
  } catch {
    purchaseUsers.value = []
  }
  void fetchList()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-stock-item-list-page {
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

.header-right {
  flex-shrink: 0;
  min-height: 1px;
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
  margin-top: 6px;
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

.status-select--sales {
  width: 150px;
}

.status-select--purchaser {
  width: 168px;
  min-width: 140px;
}

.status-select--warehouse {
  width: 190px;
}

.stock-item-code-with-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.outbound-status-chip {
  display: inline-flex;
  align-items: center;
  gap: 0;
  justify-content: center;
  min-width: 56px;
  padding: 3px 10px;
  border-radius: 5px;
  font-size: 12px;
  line-height: 1.1;
  font-weight: 400;
  color: #fff;
  border: none;
  white-space: nowrap;
}

.outbound-status-chip--none {
  background: #9ca3af;
}

.outbound-status-chip--partial {
  background: #e6a23c;
}

.outbound-status-chip--done {
  background: #67c23a;
}

.outbound-status-chip--unknown {
  background: #9ca3af;
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

.search-input--filter {
  width: 140px;
  padding: 6px 10px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-sm;
  background: $layer-2;
  color: $text-primary;
  font-size: 13px;
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
  padding: 0 2px;
  font-size: 12px;
  color: $text-muted;
  flex-shrink: 0;
  user-select: none;
  border-left: 1px solid $border-panel;
  border-right: 1px solid $border-panel;
}

.filter-date-range__picker {
  width: 108px;
  flex: 0 0 108px;
  max-width: 108px;
}

.filter-date-range__picker--start :deep(.el-input__wrapper),
.filter-date-range__picker--end :deep(.el-input__wrapper) {
  box-shadow: none !important;
  border: none !important;
  border-radius: 0 !important;
  background: transparent;
  padding-left: 4px;
  padding-right: 4px;
}

.filter-date-range__picker--start :deep(.el-input__prefix),
.filter-date-range__picker--end :deep(.el-input__prefix) {
  margin-inline-end: 2px;
}

.filter-date-range__picker :deep(.el-input__inner) {
  font-variant-numeric: tabular-nums;
}

.btn-primary,
.btn-ghost {
  padding: 6px 14px;
  border-radius: $border-radius-sm;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
}

.btn-primary {
  background: $primary-color;
  color: #fff;
}

.btn-ghost {
  background: transparent;
  border-color: $border-panel;
  color: $text-secondary;
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

// ---- 表格：.table-wrapper / CrmDataTable 全局样式见 assets/styles/crm-unified-list.scss ----
.inventory-stock-item-list-crm-table.table-wrapper {
  :deep(.el-table .cell) {
    line-height: 1.2;
  }
}
</style>
