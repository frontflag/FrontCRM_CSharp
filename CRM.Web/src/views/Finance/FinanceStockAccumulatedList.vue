<template>
  <div class="finance-page stock-accumulated-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="finance-list-page-title">{{ t('stockAccumulated.summaryTitle') }}</h1>
      </div>
      <div class="header-right">
        <el-select
          v-model="selectedYear"
          class="year-select"
          :placeholder="t('stockAccumulated.filters.yearPlaceholder')"
          @change="() => void fetchSummary()"
        >
          <el-option v-for="y in yearOptions" :key="y" :label="y" :value="y" />
        </el-select>
      </div>
    </div>

    <el-table
      v-loading="loading"
      :data="rows"
      stripe
      class="stock-accumulated-table"
      @row-click="onRowClick"
    >
      <el-table-column prop="yearMonth" :label="t('stockAccumulated.columns.yearMonth')" min-width="110" />
      <el-table-column :label="t('stockAccumulated.columns.prvAmountTotal')" min-width="130" align="right">
        <template #default="{ row }">{{ formatUsd(row.prvAmountTotal) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockAccumulated.columns.currentStockInAmountTotal')" min-width="130" align="right">
        <template #default="{ row }">{{ formatUsd(row.currentStockInAmountTotal) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockAccumulated.columns.currentStockOutAmountTotal')" min-width="130" align="right">
        <template #default="{ row }">{{ formatUsd(row.currentStockOutAmountTotal) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockAccumulated.columns.balanceAmountTotal')" min-width="130" align="right">
        <template #default="{ row }">
          <span class="balance-cell">{{ formatUsd(row.balanceAmountTotal) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="prvStockQty" :label="t('stockAccumulated.columns.prvStockQty')" min-width="100" align="right" />
      <el-table-column prop="stockInQty" :label="t('stockAccumulated.columns.stockInQty')" min-width="100" align="right" />
      <el-table-column prop="stockOutQty" :label="t('stockAccumulated.columns.stockOutQty')" min-width="100" align="right" />
      <el-table-column prop="balanceStockQty" :label="t('stockAccumulated.columns.balanceStockQty')" min-width="100" align="right" />
      <el-table-column :label="t('stockAccumulated.columns.actions')" width="90" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" link @click.stop="goDetail(row.yearMonth)">
            {{ t('stockAccumulated.actions.detail') }}
          </el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  financeStockAccumulatedApi,
  type FinanceStockAccumulatedMonthRow
} from '@/api/financeStockAccumulated'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const selectedYear = ref(String(new Date().getFullYear()))
const yearOptions = ref<string[]>([])
const rows = ref<FinanceStockAccumulatedMonthRow[]>([])
const maskAmounts = ref(false)

function formatUsd(value: number | null | undefined): string {
  if (maskAmounts.value || value == null) return '—'
  return `$ ${value.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

async function loadYears() {
  const data = await financeStockAccumulatedApi.getSearchOptions()
  yearOptions.value = data.years?.length ? data.years : [selectedYear.value]
  if (!yearOptions.value.includes(selectedYear.value)) {
    selectedYear.value = yearOptions.value[0]
  }
}

async function fetchSummary() {
  if (!selectedYear.value) {
    ElMessage.warning(t('stockAccumulated.messages.yearRequired'))
    return
  }
  loading.value = true
  try {
    const data = await financeStockAccumulatedApi.getStockSummary(selectedYear.value)
    rows.value = data.items ?? []
    maskAmounts.value = data.maskAmounts === true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockAccumulated.messages.loadFailed')))
  } finally {
    loading.value = false
  }
}

function goDetail(month: string) {
  router.push({ name: 'FinanceStockAccumulatedItemList', query: { month } })
}

function onRowClick(row: FinanceStockAccumulatedMonthRow) {
  goDetail(row.yearMonth)
}

onMounted(async () => {
  try {
    await loadYears()
    await fetchSummary()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockAccumulated.messages.loadFailed')))
  }
})
</script>

<style scoped>
.stock-accumulated-page .page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.year-select {
  width: 140px;
}

.stock-accumulated-table :deep(.el-table__row) {
  cursor: pointer;
}

.balance-cell {
  font-weight: 600;
}
</style>
