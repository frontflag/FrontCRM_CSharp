<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  salesOrderItemListAnalyticsApi,
  type SalesOrderItemListAnalyticsDashboard,
  type SalesOrderItemListAnalyticsQuery,
  type SalesOrderItemListAnalyticsRankingRow,
  type SalesOrderItemListAnalyticsRankings,
  type SalesOrderItemListAnalyticsTrendPoint
} from '@/api/salesOrderItemAnalytics'
import type { SalesAnalyticsBreakdownGroup } from '@/api/analytics/sales'

const props = defineProps<{
  filters: SalesOrderItemListAnalyticsQuery
}>()

const { t } = useI18n()

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const dashboard = ref<SalesOrderItemListAnalyticsDashboard | null>(null)
const trends = ref<SalesOrderItemListAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const rankings = ref<SalesOrderItemListAnalyticsRankings | null>(null)
const rankingMetricMode = ref<'amount' | 'count'>('amount')

type RankingCountKind = 'line' | 'qty'

interface RankingTableConfig {
  key: string
  titleKey: string
  dataKey: keyof SalesOrderItemListAnalyticsRankings
  countKind: RankingCountKind
}

const rankingTables: RankingTableConfig[] = [
  { key: 'customer', titleKey: 'customerByAmount', dataKey: 'customerByAmount', countKind: 'line' },
  { key: 'pnAmount', titleKey: 'pnByAmount', dataKey: 'pnByAmount', countKind: 'line' },
  { key: 'pnQty', titleKey: 'pnByQty', dataKey: 'pnByQty', countKind: 'qty' },
  { key: 'brandAmount', titleKey: 'brandByAmount', dataKey: 'brandByAmount', countKind: 'line' },
  { key: 'brandQty', titleKey: 'brandByQty', dataKey: 'brandByQty', countKind: 'qty' },
  { key: 'salesUser', titleKey: 'salesUserByAmount', dataKey: 'salesUserByAmount', countKind: 'line' }
]

const maskAmounts = computed(() => dashboard.value?.context.maskAmounts === true)

const effectiveRankingMetricMode = computed<'amount' | 'count'>(() =>
  maskAmounts.value ? 'count' : rankingMetricMode.value
)

function rankingRowsFor(config: RankingTableConfig): SalesOrderItemListAnalyticsRankingRow[] {
  const data = rankings.value?.[config.dataKey]
  return Array.isArray(data) ? data : []
}

function rankingCountLabel(kind: RankingCountKind): string {
  return kind === 'qty'
    ? t('salesOrderItemList.board.rankings.qty')
    : t('salesOrderItemList.board.rankings.lineCount')
}

function rankingMetricHeaderLabel(kind: RankingCountKind): string {
  return effectiveRankingMetricMode.value === 'amount'
    ? t('salesOrderItemList.board.rankings.amount')
    : rankingCountLabel(kind)
}

function formatRankingMetric(row: SalesOrderItemListAnalyticsRankingRow): string {
  if (effectiveRankingMetricMode.value === 'amount') return formatMoney(row.amount)
  return String(row.orderCount ?? 0)
}

function toggleRankingMetricMode() {
  if (maskAmounts.value) return
  rankingMetricMode.value = rankingMetricMode.value === 'amount' ? 'count' : 'amount'
}

function formatMoney(v?: number | null): string {
  if (v == null) return '—'
  return `$\u00a0${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function formatOriginalMoney(amount: number | null | undefined, currencyLabel: string): string {
  if (amount == null) return '—'
  return `${amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currencyLabel}`
}

function formatDays(v?: number | null): string {
  if (v == null || v === undefined) return '—'
  return `${v}`
}

const orderKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  const currencyItems = maskAmounts.value
    ? []
    : (s.currencyLines ?? []).map((line) => ({
        currencyLabel: line.currencyLabel,
        originalText: formatOriginalMoney(line.originalAmount, line.currencyLabel),
        usdText: ''
      }))

  return [
    {
      key: 'approvedCustomers',
      label: t('salesOrderItemList.board.kpi.approvedCustomers'),
      value: String(s.approvedCustomerCount)
    },
    {
      key: 'approvedOrders',
      label: t('salesOrderItemList.board.kpi.approvedOrders'),
      value: String(s.approvedOrderCount)
    },
    {
      key: 'approvedLines',
      label: t('salesOrderItemList.board.kpi.approvedLines'),
      value: String(s.approvedLineCount)
    },
    {
      key: 'approvedAmount',
      label: t('salesOrderItemList.board.kpi.approvedAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.approvedAmountUsd),
      valueFormat: 'money' as const,
      layout: 'split' as const,
      valueCaption: maskAmounts.value ? undefined : t('salesOrderItemList.board.kpi.usdCaption'),
      currencyCaption: currencyItems.length ? t('salesOrderItemList.board.kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined
    }
  ]
})

const profitKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'purchaseProfit',
      label: t('salesOrderItemList.board.kpi.purchaseProfit'),
      value: maskAmounts.value ? '—' : formatMoney(s.purchaseProfitUsd),
      valueFormat: 'money' as const
    },
    {
      key: 'outboundProfit',
      label: t('salesOrderItemList.board.kpi.outboundProfit'),
      value: maskAmounts.value ? '—' : formatMoney(s.outboundProfitUsd),
      valueFormat: 'money' as const
    }
  ]
})

const inStockKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'inStockCustomers',
      label: t('salesOrderItemList.board.kpi.inStockCustomers'),
      value: String(s.inStockCustomerCount)
    },
    {
      key: 'inStockLines',
      label: t('salesOrderItemList.board.kpi.inStockLines'),
      value: String(s.inStockLineCount)
    },
    {
      key: 'inStockAmount',
      label: t('salesOrderItemList.board.kpi.inStockAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.inStockAmountUsd),
      valueFormat: 'money' as const
    },
    {
      key: 'maxStockAge',
      label: t('salesOrderItemList.board.kpi.maxStockAge'),
      value: formatDays(s.maxStockAgeDays)
    }
  ]
})

const receivableKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  const currencyItems = maskAmounts.value
    ? []
    : (s.receivableCurrencyLines ?? []).map((line) => ({
        currencyLabel: line.currencyLabel,
        originalText: formatOriginalMoney(line.originalAmount, line.currencyLabel),
        usdText: ''
      }))

  return [
    {
      key: 'receivableCustomers',
      label: t('salesOrderItemList.board.kpi.receivableCustomers'),
      value: String(s.receivableCustomerCount)
    },
    {
      key: 'receivableLines',
      label: t('salesOrderItemList.board.kpi.receivableLines'),
      value: String(s.receivableLineCount)
    },
    {
      key: 'receivableAmount',
      label: t('salesOrderItemList.board.kpi.receivableAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.receivableAmountUsd),
      valueFormat: 'money' as const,
      layout: currencyItems.length ? ('split' as const) : undefined,
      valueCaption: maskAmounts.value ? undefined : t('salesOrderItemList.board.kpi.usdCaption'),
      currencyCaption: currencyItems.length ? t('salesOrderItemList.board.kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined
    },
    {
      key: 'maxReceivableAge',
      label: t('salesOrderItemList.board.kpi.maxReceivableAge'),
      value: formatDays(s.maxReceivableAgeDays)
    }
  ]
})

const trendOrderPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.approvedOrderCount }))
)

const trendLinePoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.approvedLineCount }))
)

const trendAmountPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.approvedLineAmountUsd ?? 0 }))
)

const pieBreakdownKeys = new Set([
  'currency',
  'purchaseProgress',
  'stockInProgress',
  'stockOutNotifyProgress',
  'receiptProgress',
  'invoiceProgress',
  'brandQty',
  'brandAmount',
  'dateCode',
  'salesUser'
])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

function breakdownValueFormat(groupKey: string): 'money' | 'number' {
  if (groupKey === 'currency' && !maskAmounts.value) return 'money'
  if (groupKey === 'brandAmount' && !maskAmounts.value) return 'money'
  if (groupKey === 'salesUser' && !maskAmounts.value) return 'money'
  if (
    (groupKey === 'purchaseProgress' ||
      groupKey === 'stockInProgress' ||
      groupKey === 'stockOutNotifyProgress' ||
      groupKey === 'receiptProgress' ||
      groupKey === 'invoiceProgress') &&
    !maskAmounts.value
  ) {
    return 'money'
  }
  return 'number'
}

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `salesOrderItemList.board.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  if (maskAmounts.value && group.groupKey !== 'itemStatus' && group.groupKey !== 'brandQty' && group.groupKey !== 'dateCode') {
    return `${base}（${t('salesOrderItemList.board.breakdown.byCount')}）`
  }
  return base
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'itemStatus') {
    const statusNum = Number(item.key)
    if (statusNum === 0) return t('salesOrderItemList.board.itemStatus.normal')
    if (statusNum === 1) return t('salesOrderItemList.board.itemStatus.cancelled')
  }
  const progressMap: Record<string, string> = {
    purchaseProgress: 'purchase',
    stockInProgress: 'stockIn',
    stockOutNotifyProgress: 'stockOutNotify',
    receiptProgress: 'receipt',
    invoiceProgress: 'invoice'
  }
  const kind = progressMap[groupKey]
  if (kind) {
    const statusNum = Number(item.key)
    if (statusNum === 0) return t(`salesOrderItemList.extendProgress.${kind}.pending`)
    if (statusNum === 1) return t(`salesOrderItemList.extendProgress.${kind}.partial`)
    if (statusNum === 2) return t(`salesOrderItemList.extendProgress.${kind}.complete`)
  }
  return item.label
}

function localizedBreakdownItems(group: SalesAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function buildQuery(): SalesOrderItemListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      salesOrderItemListAnalyticsApi.getDashboard(q),
      salesOrderItemListAnalyticsApi.getTrends(q),
      salesOrderItemListAnalyticsApi.getBreakdowns(q),
      salesOrderItemListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('salesOrderItemList.board.loadFailed')
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
}

watch(() => ({ ...props.filters, groupBy: groupBy.value }), () => void loadData(), { deep: true })
onMounted(() => void loadData())

defineExpose({ reload: loadData })
</script>

<template>
  <div class="so-item-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <span class="board-hint">{{ t('salesOrderItemList.board.hint') }}</span>
      <el-select v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="t('salesOrderItemList.board.groupBy.day')" />
        <el-option value="week" :label="t('salesOrderItemList.board.groupBy.week')" />
        <el-option value="month" :label="t('salesOrderItemList.board.groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData">{{ t('salesOrderItemList.board.refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('salesOrderItemList.board.sections.orderKpi') }}</h3>
      <AnalyticsKpiGrid :items="orderKpiItems" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ t('salesOrderItemList.board.sections.profitKpi') }}</h3>
      <AnalyticsKpiGrid :items="profitKpiItems" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ t('salesOrderItemList.board.sections.inStockKpi') }}</h3>
      <AnalyticsKpiGrid :items="inStockKpiItems" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ t('salesOrderItemList.board.sections.receivableKpi') }}</h3>
      <AnalyticsKpiGrid :items="receivableKpiItems" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('salesOrderItemList.board.sections.trendOrders') }}</h3>
        <AnalyticsTrendChart
          :points="trendOrderPoints"
          :value-suffix="t('salesOrderItemList.board.trendUnit.orders')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('salesOrderItemList.board.sections.trendLines') }}</h3>
        <AnalyticsTrendChart
          :points="trendLinePoints"
          :value-suffix="t('salesOrderItemList.board.trendUnit.lines')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('salesOrderItemList.board.sections.trendAmount') }}</h3>
        <AnalyticsTrendChart
          :points="trendAmountPoints"
          value-format="money"
          :unit-caption="t('salesOrderItemList.board.trendUnit.moneyCaption')"
        />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div v-for="group in breakdowns" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          v-if="isPieBreakdown(group.groupKey)"
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          :value-format="breakdownValueFormat(group.groupKey)"
        />
        <AnalyticsBreakdownChart
          v-else
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
        />
      </div>
    </div>

    <div class="rankings-section">
      <div v-if="!maskAmounts" class="rankings-toolbar">
        <span class="rankings-toolbar-label">{{ t('salesOrderItemList.board.rankings.metricMode') }}</span>
        <el-radio-group v-model="rankingMetricMode" size="small">
          <el-radio-button value="amount">{{ t('salesOrderItemList.board.rankings.amount') }}</el-radio-button>
          <el-radio-button value="count">{{ t('salesOrderItemList.board.rankings.lineCount') }}</el-radio-button>
        </el-radio-group>
      </div>

      <div class="rankings-row">
        <div v-for="table in rankingTables" :key="table.key" class="card ranking-panel">
          <h3 class="section-title">{{ t(`salesOrderItemList.board.rankings.${table.titleKey}`) }}</h3>
          <el-table :data="rankingRowsFor(table)" size="small" stripe class="ranking-table">
            <el-table-column
              prop="name"
              :label="t('salesOrderItemList.board.rankings.name')"
              min-width="200"
              class-name="ranking-name-col"
            >
              <template #default="{ row }">
                <span class="ranking-name-cell">{{ row.name }}</span>
              </template>
            </el-table-column>
            <el-table-column width="130" align="right" class-name="ranking-metric-col">
              <template #header>
                <button
                  type="button"
                  class="ranking-metric-header"
                  :class="{ 'ranking-metric-header--toggle': !maskAmounts }"
                  :disabled="maskAmounts"
                  @click="toggleRankingMetricMode"
                >
                  <span>{{ rankingMetricHeaderLabel(table.countKind) }}</span>
                  <span v-if="!maskAmounts" class="ranking-metric-switch" aria-hidden="true">⇄</span>
                </button>
              </template>
              <template #default="{ row }">
                <span class="ranking-metric-value">{{ formatRankingMetric(row) }}</span>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.so-item-list-board {
  margin-bottom: 16px;
}

.card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 16px;
}

.board-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.board-hint {
  flex: 1;
  font-size: 13px;
  color: var(--el-text-color-secondary);
  min-width: 200px;
}

.section {
  margin-bottom: 16px;
}

.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
}

.charts-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.breakdown-row {
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
}

.rankings-section {
  margin-bottom: 16px;
}

.rankings-toolbar {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.rankings-toolbar-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.rankings-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
  gap: 16px;
}

.chart-panel,
.ranking-panel {
  min-height: 240px;
}

.ranking-table {
  width: 100%;
}

.ranking-name-cell {
  display: block;
  white-space: normal;
  word-break: break-word;
  line-height: 1.45;
  padding: 2px 0;
}

.ranking-metric-header {
  display: inline-flex;
  align-items: center;
  justify-content: flex-end;
  gap: 4px;
  width: 100%;
  padding: 0;
  border: none;
  background: transparent;
  font: inherit;
  font-weight: 600;
  color: var(--el-text-color-regular);
  cursor: default;
}

.ranking-metric-header--toggle {
  cursor: pointer;
  color: var(--el-color-primary);

  &:hover {
    color: var(--el-color-primary-light-3);
  }
}

.ranking-metric-header:disabled {
  cursor: default;
  color: var(--el-text-color-regular);
}

.ranking-metric-switch {
  font-size: 12px;
  opacity: 0.85;
}

.ranking-metric-value {
  font-variant-numeric: tabular-nums;
}

:deep(.ranking-name-col .cell) {
  white-space: normal;
}

:deep(.ranking-metric-col .cell) {
  white-space: nowrap;
}
</style>
