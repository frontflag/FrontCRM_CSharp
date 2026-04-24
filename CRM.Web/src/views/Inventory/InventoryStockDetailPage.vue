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
      <h2 class="section-title">{{ t('inventoryStockDetail.stockItemsSection') }}</h2>
      <CrmDataTable :data="pagedStockItems" v-loading="loadingItems">
        <el-table-column prop="stockItemCode" :label="t('inventoryStockDetail.columns.stockItemCode')" width="168" show-overflow-tooltip />
        <el-table-column prop="stockInCode" :label="t('inventoryStockDetail.columns.stockInCode')" width="150" />
        <el-table-column prop="batchNo" :label="t('inventoryStockDetail.columns.batchNo')" width="120" />
        <el-table-column :label="t('inventoryStockDetail.columns.productionDate')" width="120">
          <template #default="{ row }">{{ formatDateOnly(row.productionDate) }}</template>
        </el-table-column>
        <el-table-column prop="purchasePn" :label="t('inventoryStockDetail.columns.pn')" min-width="120" show-overflow-tooltip />
        <el-table-column prop="purchaseBrand" :label="t('inventoryStockDetail.columns.brand')" min-width="100" show-overflow-tooltip />
        <el-table-column prop="sellOrderItemCode" :label="t('inventoryStockDetail.columns.sellLineCode')" width="140" show-overflow-tooltip />
        <el-table-column prop="qtyInbound" :label="t('inventoryStockDetail.columns.qtyInbound')" width="100" align="right" />
        <el-table-column prop="qtyStockOut" :label="t('inventoryStockDetail.columns.qtyStockOut')" width="100" align="right" />
        <el-table-column prop="qtyRepertory" :label="t('inventoryStockDetail.columns.qtyRepertory')" width="100" align="right" />
        <el-table-column prop="qtyRepertoryAvailable" :label="t('inventoryStockDetail.columns.qtyAvailable')" width="100" align="right" />
        <el-table-column prop="qtyOccupy" :label="t('inventoryStockDetail.columns.qtyOccupy')" width="90" align="right" />
        <el-table-column prop="qtySales" :label="t('inventoryStockDetail.columns.qtySales')" width="90" align="right" />
        <el-table-column :label="t('inventoryStockDetail.columns.purchasePrice')" width="128" align="right">
          <template #default="{ row }">
            <span v-if="maskPurchaseSensitiveFields">—</span>
            <span v-else>{{ formatCurrencyUnitPrice(row.purchasePrice, row.purchaseCurrency) }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('inventoryStockDetail.columns.purchasePriceUsd')" width="118" align="right">
          <template #default="{ row }">
            <span v-if="maskPurchaseSensitiveFields">—</span>
            <span v-else>{{ formatCurrencyUnitPrice(row.purchasePriceUsd, 2) }}</span>
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
        <el-table-column prop="quantity" :label="t('inventoryTrace.columns.quantity')" width="100" align="right" />
        <el-table-column prop="unitPrice" :label="t('inventoryTrace.columns.unitPrice')" width="110" align="right">
          <template #default="{ row }">
            <span v-if="maskPurchaseSensitiveFields">—</span>
            <span v-else>{{ formatUnitPriceNumber(row.unitPrice) }}</span>
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
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { inventoryCenterApi, type MaterialTrace, type StockItemRow, type WarehouseInfo } from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatCurrencyUnitPrice, formatUnitPriceNumber } from '@/utils/moneyFormat'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

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
const loadingItems = ref(false)
const loadingTrace = ref(false)

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
  await loadTrace()
}

const reload = async () => {
  await loadWarehouses()
  await loadStockItems()
}

const goBack = () => {
  router.push('/inventory/list')
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
</style>
