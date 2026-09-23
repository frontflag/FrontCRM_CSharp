<template>
  <div class="inventory-stock-detail-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">{{ t('inventoryStockDetail.title') }}</h1>
        <div class="summary-line">
          <span class="summary-item"
            >{{ t('inventoryStockDetail.summary.stockCode') }}: {{ summaryStockCode }}</span
          >
          <span class="summary-item"
            >{{ t('inventoryStockDetail.summary.materialModel') }}: {{ summaryMaterialModel }}</span
          >
          <span class="summary-item"
            >{{ t('inventoryStockDetail.summary.brand') }}: {{ summaryMaterialBrand }}</span
          >
          <span class="summary-item"
            >{{ t('inventoryStockDetail.summary.warehouse') }}: {{ summaryWarehouse }}</span
          >
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-secondary" @click="goBack">{{ t('inventoryStockDetail.back') }}</button>
        <button type="button" class="btn-primary" @click="reload">{{ t('inventoryStockDetail.refresh') }}</button>
      </div>
    </div>

    <section class="section">
      <h2 class="section-title">{{ t('inventoryOnHandList.title') }}</h2>
      <CrmDataTable
        column-layout-key="inventory-stock-detail-on-hand-v1"
        :columns="onHandTableColumns"
        :show-column-settings="false"
        :data="onHandRows"
        v-loading="loadingOnHand"
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
            :class="{ 'inv-stock-type-cell--stocking': onHandStockTypeNum(row) === 2 }"
          >
            <span>{{ onHandStockTypeLabel(row) }}</span>
            <el-icon v-if="onHandStockTypeNum(row) === 2" class="inv-stock-type-icon" aria-hidden="true">
              <Box />
            </el-icon>
          </span>
        </template>
        <template #col-warehouse="{ row }">{{ onHandWarehouseLabel(row) }}</template>
        <template #col-onHandQty="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(row.onHandQty) }}</span>
        </template>
        <template v-for="ccy in onHandDisplayCurrencies" :key="'amt-slot-' + ccy" #[`col-amount-${ccy}`]="{ row }">
          <template v-if="maskPurchaseSensitiveFields">
            <span class="inv-list-dash">—</span>
          </template>
          <template v-else-if="!inventoryAmountHasValue(onHandAmountOf(row, ccy))">
            <span class="inv-list-dash">—</span>
          </template>
          <div v-else class="inv-list-amount-cell dock-tier-price-line">
            <template v-for="amt in [splitInventoryMoneyParts(Number(onHandAmountOf(row, ccy)))]" :key="'oh-' + ccy">
              <span class="inv-list-amt">
                <span class="inv-list-amt-int">{{ amt.intPart }}</span><span class="inv-list-amt-frac">{{ amt.fracPart }}</span>
              </span>
            </template>
            <span class="dock-tier-ccy-gap">&nbsp;</span>
            <span :class="['dock-tier-ccy', currencyClass(ccy)]">{{ currencyIso(ccy) }}</span>
          </div>
        </template>
      </CrmDataTable>
    </section>

    <section class="section">
      <h2 class="section-title">{{ t('inventoryStockDetail.stockItemsSection') }}</h2>
      <CrmDataTable :data="pagedStockItems" v-loading="loadingItems">
        <el-table-column :label="t('inventoryStockDetail.columns.stockItemCode')" width="188" min-width="168">
          <template #default="{ row }">
            <span class="stock-item-code-with-badge">
              <span>{{ row.stockItemCode || '—' }}</span>
              <el-tooltip
                v-if="isStockingStockItem(row)"
                :content="t('inventoryList.stockTypes.stocking')"
                placement="top"
                :hide-after="0"
              >
                <span class="inv-stock-item-code-stocking-hit" role="img" :aria-label="t('inventoryList.stockTypes.stocking')">
                  <el-icon class="inv-stock-item-code-stocking-icon" aria-hidden="true">
                    <Box />
                  </el-icon>
                </span>
              </el-tooltip>
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="stockInCode" :label="t('inventoryStockDetail.columns.stockInCode')" width="150" />
        <el-table-column prop="batchNo" :label="t('inventoryStockDetail.columns.batchNo')" width="120" />
        <el-table-column :label="t('inventoryStockDetail.columns.productionDate')" width="120">
          <template #default="{ row }">{{ formatDateOnly(row.productionDate) }}</template>
        </el-table-column>
        <CrmCopyableTableColumn prop="purchasePn" :label="t('inventoryStockDetail.columns.pn')" min-width="120" />
        <CrmCopyableTableColumn prop="purchaseBrand" :label="t('inventoryStockDetail.columns.brand')" min-width="100" />
        <el-table-column :label="t('inventoryStockDetail.columns.regionType')" width="88" align="center">
          <template #default="{ row }">
            <span class="region-type-chip" :class="`region-type-chip--${regionTypeKind(row)}`">
              <span>{{ stockItemRegionLabel(row) }}</span>
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="sellOrderItemCode" :label="t('inventoryStockDetail.columns.sellLineCode')" width="140" show-overflow-tooltip />
        <el-table-column prop="qtyInbound" :label="t('inventoryStockDetail.columns.qtyInbound')" width="100" align="right" />
        <el-table-column prop="qtyStockOut" :label="t('inventoryStockDetail.columns.qtyStockOut')" width="100" align="right" />
        <el-table-column prop="qtyRepertory" :label="t('inventoryStockDetail.columns.qtyRepertory')" width="100" align="right" />
        <el-table-column prop="qtyRepertoryAvailable" :label="t('inventoryStockDetail.columns.qtyAvailable')" width="100" align="right" />
        <el-table-column prop="qtyOccupy" :label="t('inventoryStockDetail.columns.qtyOccupy')" width="90" align="right" />
        <el-table-column prop="qtySales" :label="t('inventoryStockDetail.columns.qtySales')" width="90" align="right" />
        <el-table-column
          :label="t('inventoryStockDetail.columns.purchasePrice')"
          min-width="140"
          align="right"
          class-name="stock-item-unit-price-col"
        >
          <template #default="{ row }">
            <span v-if="maskPurchaseSensitiveFields">—</span>
            <template v-else-if="unitPriceDockHasValue(row.purchasePrice)">
              <div class="dock-tier-price-line">
                <template v-for="amt in [splitUnitPriceDockParts(row.purchasePrice)]" :key="'pp-' + row.stockItemId">
                  <span class="dock-tier-amt">
                    <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                    ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                  </span>
                </template>
                <span class="dock-tier-ccy-gap">&nbsp;</span>
                <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.purchaseCurrency)]">{{
                  listAmountCurrencyIso(row.purchaseCurrency)
                }}</span>
              </div>
            </template>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column
          :label="t('inventoryStockDetail.columns.purchasePriceUsd')"
          min-width="132"
          align="right"
          class-name="stock-item-unit-price-col"
        >
          <template #default="{ row }">
            <span v-if="maskPurchaseSensitiveFields">—</span>
            <template v-else-if="unitPriceDockHasValue(row.purchasePriceUsd)">
              <div class="dock-tier-price-line">
                <template v-for="amt in [splitUnitPriceDockParts(row.purchasePriceUsd)]" :key="'ppu-' + row.stockItemId">
                  <span class="dock-tier-amt">
                    <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                    ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                  </span>
                </template>
                <span class="dock-tier-ccy-gap">&nbsp;</span>
                <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
              </div>
            </template>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('inventoryStockDetail.columns.salesPrice')" width="128" align="right">
          <template #default="{ row }">
            <span v-if="maskSaleSensitiveFields">—</span>
            <template v-else-if="row.salesPrice != null && row.salesPrice !== undefined">{{
              formatCurrencyUnitPrice(row.salesPrice, row.salesCurrency ?? undefined)
            }}</template>
            <span v-else>{{ t('quoteList.na') }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('inventoryStockDetail.columns.salesPriceUsd')" width="118" align="right">
          <template #default="{ row }">
            <span v-if="maskSaleSensitiveFields">—</span>
            <template v-else-if="row.salesPriceUsd != null && row.salesPriceUsd !== undefined">{{
              formatCurrencyUnitPrice(row.salesPriceUsd, 2)
            }}</template>
            <span v-else>{{ t('quoteList.na') }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="vendorName" :label="t('inventoryStockDetail.columns.vendor')" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ maskPurchaseSensitiveFields ? '—' : (row.vendorName?.trim() || t('quoteList.na')) }}
          </template>
        </el-table-column>
        <el-table-column prop="customerName" :label="t('inventoryStockDetail.columns.customer')" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() || t('quoteList.na')) }}
          </template>
        </el-table-column>
        <el-table-column prop="locationId" :label="t('inventoryStockDetail.columns.location')" min-width="100" show-overflow-tooltip />
        <el-table-column :label="t('inventoryStockDetail.columns.createTime')" width="170">
          <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
        </el-table-column>
        <el-table-column
          fixed="right"
          :label="t('inventoryStockDetail.columns.actions')"
          :width="stockDetailOpColWidth"
          :min-width="stockDetailOpColMinWidth"
          align="center"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="stockDetailOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleStockDetailOpCol"
            >
              {{ stockDetailOpColExpanded ? '>' : '<' }}
            </button>
          </div>
          </template>
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="stockDetailOpColExpanded" class="action-btns">
                <el-button type="primary" link @click.stop="goManualTransfer(row.stockItemId)">
                  {{ t('inventoryStockDetail.actions.manualTransfer') }}
                </el-button>
              </div>
              <el-dropdown v-else trigger="click" placement="bottom-end">
                <div class="op-more-dropdown-trigger">
                  <button type="button" class="op-more-trigger">...</button>
                </div>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop="goManualTransfer(row.stockItemId)">
                      <span class="op-more-item op-more-item--primary">{{ t('inventoryStockDetail.actions.manualTransfer') }}</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
      </CrmDataTable>
      <div v-if="!loadingItems && stockItems.length > 0" class="stock-items-pagination">
        <el-pagination
          v-model:current-page="stockItemsPage"
          v-model:page-size="stockItemsPageSize"
          :total="stockItems.length"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="stockItemsPage = 1"
        />
      </div>
      <p v-if="!loadingItems && stockItems.length === 0" class="empty-hint">{{ t('inventoryStockDetail.noStockItems') }}</p>
    </section>

    <section class="section section--trace">
      <h2 class="section-title">{{ t('inventoryStockDetail.traceSection') }}</h2>
      <p v-if="!materialIdForTrace" class="empty-hint">{{ t('inventoryStockDetail.noMaterialForTrace') }}</p>
      <CrmDataTable v-else :data="traceList" v-loading="loadingTrace">
        <el-table-column prop="stockInTime" :label="t('inventoryTrace.columns.stockInTime')" width="170">
          <template #default="{ row }">{{ formatTime(row.stockInTime) }}</template>
        </el-table-column>
        <el-table-column prop="stockInCode" :label="t('inventoryTrace.columns.stockInCode')" width="160" />
        <el-table-column
          prop="purchasePn"
          :label="t('inventoryTrace.columns.purchasePn')"
          min-width="160"
          show-overflow-tooltip
        />
        <el-table-column
          prop="purchaseBrand"
          :label="t('inventoryTrace.columns.purchaseBrand')"
          min-width="120"
          show-overflow-tooltip
        />
        <el-table-column prop="quantity" :label="t('inventoryTrace.columns.quantity')" width="100" align="right" />
        <el-table-column
          prop="unitPrice"
          :label="t('inventoryTrace.columns.unitPrice')"
          min-width="140"
          align="center"
          class-name="trace-unit-price-col"
          label-class-name="trace-unit-price-col"
        >
          <template #default="{ row }">
            <span v-if="maskPurchaseSensitiveFields">—</span>
            <template v-else-if="unitPriceDockHasValue(row.unitPrice)">
              <div class="dock-tier-price-line trace-unit-price-line">
                <template v-for="amt in [splitUnitPriceDockParts(row.unitPrice)]" :key="'up-' + row.stockInCode">
                  <span class="dock-tier-amt">
                    <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                    ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                  </span>
                </template>
                <span class="dock-tier-ccy-gap">&nbsp;</span>
                <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{
                  listAmountCurrencyIso(row.currency)
                }}</span>
              </div>
            </template>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column prop="purchaseOrderCode" :label="t('inventoryTrace.columns.purchaseOrderCode')" width="150" />
        <el-table-column prop="purchaseUserName" :label="t('inventoryTrace.columns.purchaser')" width="130" />
        <el-table-column prop="qcCode" :label="t('inventoryTrace.columns.qcCode')" width="150" />
        <el-table-column prop="qcStatus" :label="t('inventoryTrace.columns.qcStatus')" width="110">
          <template #default="{ row }">{{ qcText(row.qcStatus) }}</template>
        </el-table-column>
        <el-table-column :label="t('inventoryTrace.columns.warehouse')" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.warehouseName || row.warehouseId || t('quoteList.na') }}</template>
        </el-table-column>
        <el-table-column prop="locationId" :label="t('inventoryTrace.columns.location')" min-width="130" />
      </CrmDataTable>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onActivated, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Box } from '@element-plus/icons-vue'
import {
  inventoryCenterApi,
  type InventoryOnHandSummaryRow,
  type MaterialTrace,
  type StockItemRow,
  type WarehouseInfo
} from '@/api/inventoryCenter'
import { readInventoryOnHandSplit } from '@/utils/inventoryOnHandSplit'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  formatCurrencyUnitPrice,
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  splitUnitPriceDockParts,
  unitPriceDockHasValue
} from '@/utils/moneyFormat'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const stockId = computed(() => String(route.params.stockId || '').trim())
const qStockCode = computed(() => String(route.query.stockCode || '').trim())
const qMaterialModel = computed(() => String(route.query.materialModel || '').trim())
const qMaterialBrand = computed(() => String(route.query.materialBrand || '').trim())
const qWarehouseId = computed(() => String(route.query.warehouseId || '').trim())
const qMaterialId = computed(() => String(route.query.materialId || '').trim())

const warehouses = ref<WarehouseInfo[]>([])
const stockItems = ref<StockItemRow[]>([])
const stockItemsPage = ref(1)
const stockItemsPageSize = ref(10)
const traceList = ref<MaterialTrace[]>([])
const onHandRows = ref<InventoryOnHandSummaryRow[]>([])
const onHandCurrencies = ref<number[]>([])
const onHandDisplayCurrencies = computed(() => {
  const set = new Set<number>()
  for (const raw of onHandCurrencies.value) {
    const n = Number(raw)
    if (n >= 1 && n <= 6) set.add(n)
  }
  if (set.has(2)) set.add(1)
  return [...set].sort((a, b) => a - b)
})
const onHandSplit = ref(readInventoryOnHandSplit())
const loadingItems = ref(false)
const loadingTrace = ref(false)
const loadingOnHand = ref(false)

/** 《列表操作列规范》 */
const stockDetailOpColExpanded = ref(false)
const STOCK_DETAIL_OP_COL_COLLAPSED = 43
const STOCK_DETAIL_OP_COL_EXPANDED = 173
const STOCK_DETAIL_OP_COL_EXPANDED_MIN = 160
const stockDetailOpColWidth = computed(() =>
  stockDetailOpColExpanded.value ? STOCK_DETAIL_OP_COL_EXPANDED : STOCK_DETAIL_OP_COL_COLLAPSED
)
const stockDetailOpColMinWidth = computed(() =>
  stockDetailOpColExpanded.value ? STOCK_DETAIL_OP_COL_EXPANDED_MIN : STOCK_DETAIL_OP_COL_COLLAPSED
)
function toggleStockDetailOpCol() {
  stockDetailOpColExpanded.value = !stockDetailOpColExpanded.value
}

const pagedStockItems = computed(() => {
  const all = stockItems.value
  const size = Math.max(1, stockItemsPageSize.value)
  const page = Math.max(1, stockItemsPage.value)
  const start = (page - 1) * size
  return all.slice(start, start + size)
})

const summaryStockCode = computed(() => qStockCode.value || t('quoteList.na'))
const summaryMaterialModel = computed(() => qMaterialModel.value || t('quoteList.na'))
const summaryMaterialBrand = computed(() => qMaterialBrand.value || t('quoteList.na'))

const summaryWarehouse = computed(() => {
  const wid = qWarehouseId.value
  if (!wid) return t('quoteList.na')
  const w = warehouses.value.find(x => (x.id || '').trim() === wid || (x.warehouseCode || '').trim() === wid)
  if (w?.warehouseName) return w.warehouseName
  return wid
})

const materialIdForTrace = computed(() => {
  if (qMaterialId.value) return qMaterialId.value
  const first = stockItems.value[0]
  return first?.materialId?.trim() || ''
})

const stockItemRegionLabel = (row: StockItemRow) => {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function regionTypeKind(row: StockItemRow): 'domestic' | 'overseas' {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
}

/** 备货库存：<c>stock_type === 2</c>，与全库库存明细列表一致 */
function isStockingStockItem(row: StockItemRow): boolean {
  return Number(row.stockType ?? 0) === 2
}

const formatTime = (v?: string) => formatDisplayDateTime(v)
const formatDateOnly = (v?: string) => {
  if (!v) return t('quoteList.na')
  return formatDisplayDateTime(v).split(/\s+/)[0] || t('quoteList.na')
}
const qcText = (s?: number) =>
  ({
    [-1]: t('inventoryTrace.qc.failed'),
    10: t('inventoryTrace.qc.partial'),
    100: t('inventoryTrace.qc.passed')
  }[s ?? 0] || t('quoteList.na'))

const loadWarehouses = async () => {
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch {
    warehouses.value = []
  }
}

function onHandIdentity(): { pn: string; brand: string } {
  const item = stockItems.value.find((x) => (x.purchasePn || '').trim() || (x.purchaseBrand || '').trim())
  return {
    pn: (item?.purchasePn || qMaterialModel.value || '').trim(),
    brand: (item?.purchaseBrand || qMaterialBrand.value || '').trim()
  }
}

function normOnHandKey(value?: string | null) {
  return (value || '').trim().toLowerCase()
}

function onHandStockTypeNum(row: InventoryOnHandSummaryRow): number {
  const n = Number(row.stockType ?? 0)
  return n >= 1 && n <= 3 ? n : 0
}

function onHandStockTypeLabel(row: InventoryOnHandSummaryRow) {
  const n = onHandStockTypeNum(row)
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  if (n === 1) return t('inventoryList.stockTypes.customer')
  return t('inventoryList.stockTypes.unknown')
}

function onHandWarehouseLabel(row: InventoryOnHandSummaryRow) {
  const code = (row.warehouseCode || '').trim()
  const name = (row.warehouseName || '').trim()
  if (code && name) return `${code} · ${name}`
  return name || code || (row.warehouseId || '').trim() || '—'
}

function onHandAmountOf(row: InventoryOnHandSummaryRow, currency: number): number | null {
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

/** 表头估算里 ASCII 空格约 7px；列宽 = 可显示内容 + 4 个空格。与 /inventory/list 一致。 */
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

const onHandTableColumns = computed<CrmTableColumnDef[]>(() => {
  const modelLabel = t('inventoryList.columns.materialModel')
  const brandLabel = t('inventoryList.columns.brand')
  const modelW = fitTextColumnWidth(
    modelLabel,
    onHandRows.value.map((r) => (r.materialModel || '').trim())
  )
  const brandW = fitTextColumnWidth(
    brandLabel,
    onHandRows.value.map((r) => (r.purchaseBrand || '').trim())
  )
  const cols: CrmTableColumnDef[] = [
    { key: 'materialModel', label: modelLabel, width: modelW, minWidth: modelW, showOverflowTooltip: true },
    { key: 'purchaseBrand', label: brandLabel, width: brandW, minWidth: brandW, showOverflowTooltip: true }
  ]
  if (onHandSplit.value.stockType) {
    cols.push({ key: 'stockType', label: t('inventoryList.columns.stockType'), width: 138, showOverflowTooltip: true })
  }
  if (onHandSplit.value.warehouse) {
    cols.push({ key: 'warehouse', label: t('inventoryList.columns.warehouseName'), width: 180, showOverflowTooltip: true })
  }
  cols.push({ key: 'onHandQty', label: t('inventoryList.columns.onHandQty'), prop: 'onHandQty', width: 110, align: 'right' })
  for (const ccy of onHandDisplayCurrencies.value) {
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

const loadOnHandCenter = async () => {
  const { pn, brand } = onHandIdentity()
  onHandSplit.value = readInventoryOnHandSplit()
  if (!pn && !brand) {
    onHandRows.value = []
    onHandCurrencies.value = []
    return
  }
  loadingOnHand.value = true
  try {
    const page = await inventoryCenterApi.getOnHandSummaryPaged({
      materialModel: pn || undefined,
      purchaseBrand: brand || undefined,
      groupByStockType: onHandSplit.value.stockType,
      groupByWarehouse: onHandSplit.value.warehouse,
      page: 1,
      pageSize: 200
    })
    const pnKey = normOnHandKey(pn)
    const brandKey = normOnHandKey(brand)
    onHandRows.value = (page.items || []).filter((row) => {
      if (pnKey && normOnHandKey(row.materialModel) !== pnKey) return false
      if (brandKey && normOnHandKey(row.purchaseBrand) !== brandKey) return false
      return true
    })
    onHandCurrencies.value = page.currencies ?? []
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryOnHandList.messages.loadFailed')))
    onHandRows.value = []
    onHandCurrencies.value = []
  } finally {
    loadingOnHand.value = false
  }
}

const loadTrace = async () => {
  const mid = materialIdForTrace.value
  if (!mid) {
    traceList.value = []
    return
  }
  loadingTrace.value = true
  try {
    traceList.value = await inventoryCenterApi.getMaterialTrace(mid)
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryTrace.loadFailed')))
    traceList.value = []
  } finally {
    loadingTrace.value = false
  }
}

const loadStockItems = async () => {
  if (!stockId.value) {
    stockItems.value = []
    stockItemsPage.value = 1
    await loadTrace()
    return
  }
  loadingItems.value = true
  try {
    stockItems.value = await inventoryCenterApi.getStockItemsForStock(stockId.value)
    stockItemsPage.value = 1
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryStockDetail.loadItemsFailed')))
    stockItems.value = []
    stockItemsPage.value = 1
  } finally {
    loadingItems.value = false
  }
  await Promise.all([loadTrace(), loadOnHandCenter()])
}

const reload = async () => {
  await loadWarehouses()
  await loadStockItems()
}

const goBack = () => {
  router.push('/inventory/bucket')
}

const goManualTransfer = (stockItemId: string) => {
  const id = (stockItemId || '').trim()
  if (!id) return
  const q: Record<string, string> = {
    sourceStockItemId: id,
    returnStockId: stockId.value
  }
  if (qStockCode.value) q.stockCode = qStockCode.value
  if (qMaterialModel.value) q.materialModel = qMaterialModel.value
  if (qMaterialBrand.value) q.materialBrand = qMaterialBrand.value
  if (qWarehouseId.value) q.warehouseId = qWarehouseId.value
  if (qMaterialId.value) q.materialId = qMaterialId.value
  router.push({ name: 'InventoryTransfersManual', query: q })
}

watch(
  () => stockId.value,
  async () => {
    await loadWarehouses()
    await loadStockItems()
  }
)

watch(
  () => stockItems.value.length,
  () => {
    const maxPage = Math.max(1, Math.ceil(stockItems.value.length / Math.max(1, stockItemsPageSize.value)) || 1)
    if (stockItemsPage.value > maxPage) stockItemsPage.value = maxPage
  }
)

onMounted(async () => {
  await loadWarehouses()
  await loadStockItems()
})

onActivated(async () => {
  if (!stockId.value) return
  await loadStockItems()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-stock-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
}

.page-title {
  margin: 0 0 8px;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.summary-line {
  display: flex;
  flex-wrap: wrap;
  gap: 12px 20px;
  font-size: 13px;
  color: $text-secondary;
}

.summary-item.muted {
  color: $text-muted;
  font-size: 12px;
}

.section {
  margin-bottom: 28px;
}

.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.empty-hint {
  margin: 8px 0 0;
  font-size: 13px;
  color: $text-muted;
}

.btn-primary,
.btn-secondary {
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
}

.btn-primary {
  background: $primary-color;
  color: #fff;
  margin-left: 8px;
}

.btn-secondary {
  background: $layer-2;
  color: $text-primary;
  border-color: $border-panel;
}

.stock-items-pagination {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
  flex-wrap: wrap;
}

/* 与 /inventory/stock-items 列表一致：地域胶囊、备货图标 */
.stock-item-code-with-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.inv-stock-item-code-stocking-hit {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
  cursor: default;
  line-height: 1;
}

.inv-stock-item-code-stocking-icon {
  font-size: 16px;
  color: #e6a23c;
}

html[data-theme='dark'] .inv-stock-item-code-stocking-icon {
  color: #ebb563;
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

/* 物料入库追溯单价：与 RFQ「单价（阶梯）」dock-tier-price-line 一致，列内居中 */
:deep(.trace-unit-price-col .cell) {
  text-align: center;
}

.trace-unit-price-line {
  justify-content: center;
}

.inv-list-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .inventory-stock-detail-page .inv-list-qty {
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

html[data-theme='dark'] .inventory-stock-detail-page .inv-list-amt-int,
html[data-theme='dark'] .inventory-stock-detail-page .inv-list-amt-frac {
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

:deep(.inv-on-hand-flex-col .cell) {
  padding: 0;
}
</style>
