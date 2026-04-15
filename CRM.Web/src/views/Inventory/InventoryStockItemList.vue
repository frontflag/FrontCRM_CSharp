<template>
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
        <div class="count-badge">{{ t('inventoryStockItemList.count', { count: list.length }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left search-left--wrap">
        <el-select
          v-model="filters.outboundStatus"
          clearable
          :placeholder="t('inventoryStockItemList.filters.outboundStatus')"
          class="filter-select"
          :teleported="false"
        >
          <el-option :label="t('inventoryStockItemList.filters.outboundNone')" :value="1" />
          <el-option :label="t('inventoryStockItemList.filters.outboundPartial')" :value="2" />
          <el-option :label="t('inventoryStockItemList.filters.outboundDone')" :value="3" />
        </el-select>
        <input
          v-model="filters.stockInCode"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.stockInCode')"
          @keyup.enter="fetchList"
        />
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
          />
        </div>
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
        <input
          v-model="filters.customerName"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.customerName')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.vendorName"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.vendorName')"
          @keyup.enter="fetchList"
        />
        <el-select
          v-model="filters.salespersonUserId"
          clearable
          filterable
          :placeholder="t('rfqItemList.filters.allSalesUsers')"
          class="filter-select filter-select--user"
          :teleported="false"
        >
          <el-option v-for="u in salesUsers" :key="u.id" :label="salesUserLabel(u)" :value="u.id" />
        </el-select>
        <el-select
          v-model="filters.purchaserUserId"
          clearable
          filterable
          :placeholder="t('rfqItemList.filters.allPurchasers')"
          class="filter-select filter-select--user"
          :teleported="false"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaseUserLabel(u)" :value="u.id" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="fetchList">{{ t('inventoryStockItemList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('inventoryStockItemList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable :data="list" v-loading="loading" @row-dblclick="onRowDblclick">
      <el-table-column :label="t('inventoryStockItemList.columns.outboundStatus')" width="110" align="center">
        <template #default="{ row }">{{ outboundLabel(row.outboundStatus) }}</template>
      </el-table-column>
      <el-table-column prop="stockInCode" :label="t('inventoryStockItemList.columns.stockInCode')" width="150" show-overflow-tooltip />
      <el-table-column :label="t('inventoryStockItemList.columns.stockInDate')" width="118">
        <template #default="{ row }">{{ formatDateOnly(row.stockInDate) }}</template>
      </el-table-column>
      <el-table-column :label="t('inventoryStockItemList.columns.warehouse')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ warehouseCell(row) }}</template>
      </el-table-column>
      <el-table-column prop="purchasePn" :label="t('inventoryStockItemList.columns.purchasePn')" min-width="130" show-overflow-tooltip />
      <el-table-column prop="purchaseBrand" :label="t('inventoryStockItemList.columns.purchaseBrand')" min-width="100" show-overflow-tooltip />
      <el-table-column prop="qtyInbound" :label="t('inventoryStockItemList.columns.qtyInbound')" width="88" align="right" />
      <el-table-column prop="qtyStockOut" :label="t('inventoryStockItemList.columns.qtyStockOut')" width="88" align="right" />
      <el-table-column prop="qtyRepertory" :label="t('inventoryStockItemList.columns.qtyRepertory')" width="88" align="right" />
      <el-table-column prop="customerName" :label="t('inventoryStockItemList.columns.customerName')" min-width="120" show-overflow-tooltip />
      <el-table-column prop="vendorName" :label="t('inventoryStockItemList.columns.vendorName')" min-width="120" show-overflow-tooltip />
      <el-table-column prop="salespersonName" :label="t('inventoryStockItemList.columns.salespersonName')" width="100" show-overflow-tooltip />
      <el-table-column prop="purchaserName" :label="t('inventoryStockItemList.columns.purchaserName')" width="100" show-overflow-tooltip />
      <el-table-column prop="sellOrderItemCode" :label="t('inventoryStockItemList.columns.sellOrderItemCode')" width="120" show-overflow-tooltip />
      <el-table-column prop="batchNo" :label="t('inventoryStockItemList.columns.batchNo')" width="100" show-overflow-tooltip />
      <el-table-column prop="locationId" :label="t('inventoryStockItemList.columns.locationId')" min-width="100" show-overflow-tooltip />
      <el-table-column :label="t('inventoryStockItemList.columns.profitOutBizUsd')" width="120" align="right">
        <template #default="{ row }">{{
          row.profitOutBizUsd != null ? `$${Number(row.profitOutBizUsd).toFixed(2)}` : '—'
        }}</template>
      </el-table-column>
    </CrmDataTable>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { authApi, type PurchaseUserSelectOption, type SalesUserSelectOption } from '@/api/auth'
import { inventoryCenterApi, type StockItemListQuery, type StockItemListRow } from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const { t } = useI18n()
const loading = ref(false)
const list = ref<StockItemListRow[]>([])
const dateFrom = ref<string | null>(null)
const dateTo = ref<string | null>(null)
const salesUsers = ref<SalesUserSelectOption[]>([])
const purchaseUsers = ref<PurchaseUserSelectOption[]>([])

const filters = reactive({
  stockInCode: '',
  purchasePn: '',
  purchaseBrand: '',
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
  return {
    stockInCode: filters.stockInCode.trim() || undefined,
    stockInDateFrom: dateFrom.value?.trim() || undefined,
    stockInDateTo: dateTo.value?.trim() || undefined,
    purchasePn: filters.purchasePn.trim() || undefined,
    purchaseBrand: filters.purchaseBrand.trim() || undefined,
    outboundStatus: filters.outboundStatus,
    customerName: filters.customerName.trim() || undefined,
    vendorName: filters.vendorName.trim() || undefined,
    salespersonUserId: filters.salespersonUserId?.trim() || undefined,
    purchaserUserId: filters.purchaserUserId?.trim() || undefined
  }
}

const fetchList = async () => {
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

const resetFilters = () => {
  filters.stockInCode = ''
  filters.purchasePn = ''
  filters.purchaseBrand = ''
  filters.outboundStatus = undefined
  filters.customerName = ''
  filters.vendorName = ''
  filters.salespersonUserId = undefined
  filters.purchaserUserId = undefined
  dateFrom.value = null
  dateTo.value = null
  void fetchList()
}

const formatDateOnly = (v?: string | null) => {
  if (!v) return t('quoteList.na')
  return formatDisplayDateTime(v).split(/\s+/)[0] || t('quoteList.na')
}

const outboundLabel = (s: number) => {
  if (s === 1) return t('inventoryStockItemList.filters.outboundNone')
  if (s === 2) return t('inventoryStockItemList.filters.outboundPartial')
  if (s === 3) return t('inventoryStockItemList.filters.outboundDone')
  return '—'
}

const warehouseCell = (row: StockItemListRow) => {
  const code = row.warehouseCode?.trim()
  const id = row.warehouseId?.trim()
  if (code && id) return `${code} · ${id}`
  return code || id || t('quoteList.na')
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
  margin-bottom: 16px;
}

.search-left {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
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

.filter-select {
  width: 150px;
}

.filter-select--user {
  width: 168px;
  min-width: 140px;
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
</style>
