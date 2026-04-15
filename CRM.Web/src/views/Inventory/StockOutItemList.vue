<template>
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
        </div>
        <div class="count-badge">{{ t('stockOutItemList.count', { count: list.length }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left search-left--wrap">
        <el-select
          v-model="filters.status"
          clearable
          :placeholder="t('stockOutItemList.filters.status')"
          class="filter-select"
          :teleported="false"
        >
          <el-option :label="t('stockOutList.status.draft')" :value="0" />
          <el-option :label="t('stockOutList.status.pending')" :value="1" />
          <el-option :label="t('stockOutList.status.done')" :value="2" />
          <el-option :label="t('stockOutList.status.cancelled')" :value="3" />
          <el-option :label="t('stockOutList.status.finished')" :value="4" />
        </el-select>
        <input
          v-model="filters.stockOutCode"
          class="search-input search-input--filter search-input--wide"
          :placeholder="t('stockOutItemList.filters.stockOutCode')"
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
          />
        </div>
        <input
          v-model="filters.customerName"
          class="search-input search-input--filter"
          :placeholder="t('stockOutItemList.filters.customerName')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.salesUserName"
          class="search-input search-input--filter"
          :placeholder="t('stockOutItemList.filters.salesUserName')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.purchasePn"
          class="search-input search-input--filter"
          :placeholder="t('stockOutItemList.filters.purchasePn')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.sellOrderItemCode"
          class="search-input search-input--filter search-input--wide"
          :placeholder="t('stockOutItemList.filters.sellOrderItemCode')"
          @keyup.enter="fetchList"
        />
        <button type="button" class="btn-primary btn-sm" @click="fetchList">{{ t('stockOutItemList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('stockOutItemList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable :data="list" v-loading="loading" row-key="stockOutItemId" @row-dblclick="onRowDblclick">
      <el-table-column :label="t('stockOutItemList.columns.status')" width="100" align="center">
        <template #default="{ row }">
          <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="stockOutCode" :label="t('stockOutItemList.columns.stockOutCode')" width="150" show-overflow-tooltip />
      <el-table-column :label="t('stockOutItemList.columns.stockOutDate')" width="118">
        <template #default="{ row }">{{ formatDateOnly(row.stockOutDate) }}</template>
      </el-table-column>
      <el-table-column prop="customerName" :label="t('stockOutItemList.columns.customerName')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.customerName || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column prop="salesUserName" :label="t('stockOutItemList.columns.salesUserName')" width="100" show-overflow-tooltip>
        <template #default="{ row }">{{ row.salesUserName || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column prop="purchasePn" :label="t('stockOutItemList.columns.purchasePn')" min-width="130" show-overflow-tooltip>
        <template #default="{ row }">{{ row.purchasePn || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column prop="purchaseBrand" :label="t('stockOutItemList.columns.purchaseBrand')" min-width="100" show-overflow-tooltip>
        <template #default="{ row }">{{ row.purchaseBrand || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column prop="outQuantity" :label="t('stockOutItemList.columns.outQuantity')" width="96" align="right" />
      <el-table-column prop="shipmentMethod" :label="t('stockOutItemList.columns.shipmentMethod')" width="110" show-overflow-tooltip>
        <template #default="{ row }">{{ row.shipmentMethod || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column prop="courierTrackingNo" :label="t('stockOutItemList.columns.courierTrackingNo')" width="130" show-overflow-tooltip>
        <template #default="{ row }">{{ row.courierTrackingNo || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column prop="sellOrderItemCode" :label="t('stockOutItemList.columns.sellOrderItemCode')" width="130" show-overflow-tooltip>
        <template #default="{ row }">{{ row.sellOrderItemCode || t('quoteList.na') }}</template>
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
import { stockOutApi, type StockOutItemListQuery, type StockOutItemListRow } from '@/api/stockOut'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const { t } = useI18n()
const loading = ref(false)
const list = ref<StockOutItemListRow[]>([])
const dateFrom = ref<string | null>(null)
const dateTo = ref<string | null>(null)

const filters = reactive({
  status: undefined as number | undefined,
  stockOutCode: '',
  customerName: '',
  salesUserName: '',
  purchasePn: '',
  sellOrderItemCode: ''
})

function buildQuery(): StockOutItemListQuery {
  return {
    status: filters.status,
    stockOutCode: filters.stockOutCode.trim() || undefined,
    stockOutDateFrom: dateFrom.value?.trim() || undefined,
    stockOutDateTo: dateTo.value?.trim() || undefined,
    customerName: filters.customerName.trim() || undefined,
    salesUserName: filters.salesUserName.trim() || undefined,
    purchasePn: filters.purchasePn.trim() || undefined,
    sellOrderItemCode: filters.sellOrderItemCode.trim() || undefined
  }
}

const fetchList = async () => {
  loading.value = true
  try {
    list.value = await stockOutApi.searchItems(buildQuery())
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('stockOutItemList.messages.loadFailed')))
    list.value = []
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  filters.status = undefined
  filters.stockOutCode = ''
  filters.customerName = ''
  filters.salesUserName = ''
  filters.purchasePn = ''
  filters.sellOrderItemCode = ''
  dateFrom.value = null
  dateTo.value = null
  void fetchList()
}

const formatDateOnly = (v?: string | null) => {
  if (!v) return t('quoteList.na')
  return formatDisplayDateTime(v).split(/\s+/)[0] || t('quoteList.na')
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

const onRowDblclick = (row: StockOutItemListRow) => {
  const id = (row.stockOutId || '').trim()
  if (!id) {
    ElMessage.warning(t('stockOutItemList.messages.missingStockOutId'))
    return
  }
  void router.push(`/inventory/stock-out/${encodeURIComponent(id)}`)
}

onMounted(() => {
  void fetchList()
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

.search-input--wide {
  width: 160px;
}

.filter-select {
  width: 130px;
}

.filter-date-range {
  display: inline-flex;
  align-items: stretch;
  border: 1px solid $border-panel;
  border-radius: $border-radius-sm;
  background: $layer-2;
  overflow: hidden;
}

.filter-date-range__sep {
  display: inline-flex;
  align-items: center;
  padding: 0 6px;
  font-size: 12px;
  color: $text-muted;
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

.btn-primary.btn-sm,
.btn-ghost.btn-sm {
  padding: 6px 14px;
  font-size: 13px;
  border-radius: $border-radius-sm;
  cursor: pointer;
}

.btn-primary.btn-sm {
  border: none;
  background: $cyan-primary;
  color: #fff;
}

.btn-ghost.btn-sm {
  border: 1px solid $border-panel;
  background: transparent;
  color: $text-secondary;
}
</style>
