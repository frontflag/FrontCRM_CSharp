<template>
  <!-- 业务列表页：结构对齐《业务列表规范》；表格见 CrmDataTable + 全局 crm-unified-list.scss -->
  <div class="finance-page stock-accumulated-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="finance-list-page-title">{{ t('stockAccumulated.summaryTitle') }}</h1>
      </div>
      <div class="header-right">
        <div class="header-period-nav">
          <button
            type="button"
            class="period-step-btn"
            :aria-label="t('stockAccumulated.actions.prevMonth')"
            :title="t('stockAccumulated.actions.prevMonth')"
            @click="() => shiftMonth(-1)"
          >
            &lt;
          </button>
          <el-select
            v-model="selectedYear"
            class="header-filter"
            :placeholder="t('stockAccumulated.filters.yearPlaceholder')"
            @change="onYearSelectChange"
          >
            <el-option v-for="y in yearOptions" :key="y" :label="y" :value="y" />
          </el-select>
          <button
            type="button"
            class="period-step-btn"
            :aria-label="t('stockAccumulated.actions.nextMonth')"
            :title="t('stockAccumulated.actions.nextMonth')"
            @click="() => shiftMonth(1)"
          >
            &gt;
          </button>
        </div>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-stock-accumulated-summary-v5"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="rows"
      v-loading="loading"
      row-key="yearMonth"
      highlight-current-row
      :row-class-name="stockSummaryRowClassName"
      @row-click="onRowClick"
      @row-dblclick="onRowDblClick"
    >
      <template #col-prvAmountTotal="{ row }">
        <span class="accumulated-amount-neutral">{{ formatAccumulatedUsd(maskAmounts, row.prvAmountTotal) }}</span>
      </template>
      <template #col-currentStockInAmountTotal="{ row }">
        <span class="accumulated-amount-neutral">{{ formatAccumulatedUsd(maskAmounts, row.currentStockInAmountTotal) }}</span>
      </template>
      <template #col-currentStockOutAmountTotal="{ row }">
        <span class="accumulated-amount-neutral">{{ formatAccumulatedUsd(maskAmounts, row.currentStockOutAmountTotal) }}</span>
      </template>
      <template #col-balanceAmountTotal="{ row }">
        <span
          class="amount-text amount-text--balance dock-quote-tier-line"
          :class="{ 'accumulated-balance-negative': isAccumulatedNegative(row.balanceAmountTotal) }"
        >{{ formatAccumulatedUsd(maskAmounts, row.balanceAmountTotal) }}</span>
      </template>
      <template #col-prvStockQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.prvStockQty) }}</span>
      </template>
      <template #col-stockInQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.stockInQty) }}</span>
      </template>
      <template #col-stockOutQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.stockOutQty) }}</span>
      </template>
      <template #col-balanceStockQty="{ row }">
        <span
          class="accumulated-qty-balance"
          :class="{ 'accumulated-balance-negative': isAccumulatedNegative(row.balanceStockQty) }"
        >{{ formatAccumulatedQty(row.balanceStockQty) }}</span>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <el-button link type="primary" size="small" @click.stop="goDetail(row.yearMonth)">
            {{ t('stockAccumulated.actions.detail') }}
          </el-button>
        </div>
      </template>
    </CrmDataTable>

    <div class="pagination-wrap">
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
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS,
  buildAccumulatedSummaryMetricColumn,
  formatAccumulatedQty,
  formatAccumulatedUsd,
  isAccumulatedNegative,
  resolveAccumulatedSummaryMetricMinWidth,
  useFinanceAccumulatedTableFooter
} from '@/composables/useFinanceAccumulatedListUi'
import {
  financeStockAccumulatedApi,
  type FinanceStockAccumulatedMonthRow
} from '@/api/financeStockAccumulated'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const router = useRouter()
const { dataTableRef, rowDensityToggleAnchorEl } = useFinanceAccumulatedTableFooter()

const loading = ref(false)
const selectedYear = ref(String(new Date().getFullYear()))
const selectedMonth = ref(formatCurrentMonth())
const yearOptions = ref<string[]>([])
const rows = ref<FinanceStockAccumulatedMonthRow[]>([])
const maskAmounts = ref(false)

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const metricMinWidth = resolveAccumulatedSummaryMetricMinWidth((key) => t(`stockAccumulated.columns.${key}`))
  return [
    { key: 'yearMonth', label: t('stockAccumulated.columns.yearMonth'), prop: 'yearMonth', width: 110, minWidth: 100 },
    ...ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS.map((key) =>
      buildAccumulatedSummaryMetricColumn(key, t(`stockAccumulated.columns.${key}`), key, metricMinWidth)
    ),
    { key: 'actions', label: t('stockAccumulated.columns.actions'), prop: 'actions', width: 90, minWidth: 90, fixed: 'right', className: 'op-col' }
  ]
})

function formatCurrentMonth(): string {
  const d = new Date()
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  return `${y}-${m}`
}

function shiftMonth(delta: number) {
  const [yRaw, mRaw] = selectedMonth.value.split('-')
  let y = Number(yRaw)
  let m = Number(mRaw)
  if (!Number.isFinite(y) || !Number.isFinite(m)) return
  m += delta
  while (m < 1) {
    m += 12
    y -= 1
  }
  while (m > 12) {
    m -= 12
    y += 1
  }
  const newYear = String(y)
  const newMonth = `${y}-${String(m).padStart(2, '0')}`
  selectedMonth.value = newMonth
  if (selectedYear.value !== newYear) {
    selectedYear.value = newYear
    void fetchSummary()
  }
}

function onYearSelectChange() {
  const mm = selectedMonth.value.split('-')[1] || '01'
  selectedMonth.value = `${selectedYear.value}-${mm}`
  void fetchSummary()
}

function stockSummaryRowClassName({ row }: { row: FinanceStockAccumulatedMonthRow }) {
  const classes = ['table-row-pointer']
  if (row.yearMonth === selectedMonth.value) {
    classes.push('accumulated-month-row--active')
  }
  return classes.join(' ')
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
  selectedMonth.value = row.yearMonth
}

function onRowDblClick(row: FinanceStockAccumulatedMonthRow) {
  goDetail(row.yearMonth)
}

onMounted(async () => {
  selectedMonth.value = formatCurrentMonth()
  selectedYear.value = selectedMonth.value.slice(0, 4)
  try {
    await loadYears()
    await fetchSummary()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockAccumulated.messages.loadFailed')))
  }
})
</script>

<style scoped lang="scss">
@import './finance-accumulated-list-common.scss';
</style>
