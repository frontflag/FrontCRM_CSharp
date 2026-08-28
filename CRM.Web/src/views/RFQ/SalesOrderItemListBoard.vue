<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import AnalyticsPanelHeader from '@/components/Analytics/AnalyticsPanelHeader.vue'
import { useAnalyticsDefinition } from '@/composables/useAnalyticsDefinition'
import {
  salesOrderItemListAnalyticsApi,
  type SalesOrderItemListAnalyticsDashboard,
  type SalesOrderItemListAnalyticsQuery,
  type SalesOrderItemListAnalyticsRankingRow,
  type SalesOrderItemListAnalyticsRankings,
  type SalesOrderItemListAnalyticsTrendPoint
} from '@/api/salesOrderItemAnalytics'
import {
  salesAnalyticsApi,
  type SalesAnalyticsBreakdownGroup,
  type SalesAnalyticsQuery
} from '@/api/analytics/sales'

const props = withDefaults(
  defineProps<{
    /** 列表页筛选（mode=list） */
    filters?: SalesOrderItemListAnalyticsQuery
    /** 报表 Scope 查询（mode=report） */
    reportQuery?: SalesAnalyticsQuery
    /** list=明细列表看板；report=报表订单 Tab */
    mode?: 'list' | 'report'
    /** 报表 Tab lazy：仅 active 时加载 */
    active?: boolean
  }>(),
  {
    mode: 'list',
    active: true
  }
)

const { t } = useI18n()
const { def: boardDef } = useAnalyticsDefinition('salesOrderItemList.board')
const { def: reportDef } = useAnalyticsDefinition('salesAnalytics.orderTab')
const def = boardDef

function rankingPanelDef(titleKey: string) {
  const path = `rankings.${titleKey}`
  return props.mode === 'report' ? reportDef(path) : boardDef(path)
}

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const loadedKey = ref('')
const dashboard = ref<SalesOrderItemListAnalyticsDashboard | null>(null)
const trends = ref<SalesOrderItemListAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const rankings = ref<SalesOrderItemListAnalyticsRankings | null>(null)
const rankingMetricMode = ref<'amount' | 'count'>('amount')

const isReportMode = computed(() => props.mode === 'report')
const showTrends = computed(() => isReportMode.value)
const i18nPrefix = computed(() =>
  isReportMode.value ? 'salesAnalytics.orderTab' : 'salesOrderItemList.board'
)

type RankingCountKind = 'line' | 'qty'

interface RankingTableConfig {
  key: string
  titleKey: string
  dataKey: keyof SalesOrderItemListAnalyticsRankings
  countKind: RankingCountKind
}

const rankingTablesAll: RankingTableConfig[] = [
  { key: 'customer', titleKey: 'customerByAmount', dataKey: 'customerByAmount', countKind: 'line' },
  { key: 'pnAmount', titleKey: 'pnByAmount', dataKey: 'pnByAmount', countKind: 'line' },
  { key: 'pnQty', titleKey: 'pnByQty', dataKey: 'pnByQty', countKind: 'qty' },
  { key: 'brandAmount', titleKey: 'brandByAmount', dataKey: 'brandByAmount', countKind: 'line' },
  { key: 'brandQty', titleKey: 'brandByQty', dataKey: 'brandByQty', countKind: 'qty' },
  { key: 'salesUser', titleKey: 'salesUserByAmount', dataKey: 'salesUserByAmount', countKind: 'line' }
]

/** 报表订单 Tab 仅 4 个 Top10（金额/交易频次）；列表看板保留数量维。 */
const rankingTables = computed(() =>
  isReportMode.value
    ? rankingTablesAll.filter((t) => t.countKind === 'line')
    : rankingTablesAll
)

const maskAmounts = computed(() => dashboard.value?.context.maskAmounts === true)

const effectiveRankingMetricMode = computed<'amount' | 'count'>(() =>
  maskAmounts.value ? 'count' : rankingMetricMode.value
)

const rankingCountModeLabel = computed(() =>
  isReportMode.value ? tt('rankings.transactionFrequency') : tt('rankings.lineCount')
)

function rankingQueryParams(): Pick<SalesAnalyticsQuery, 'rankingSort' | 'rankingLineMetric'> {
  if (effectiveRankingMetricMode.value === 'amount') {
    return { rankingSort: 'amount' }
  }
  if (isReportMode.value) {
    return { rankingSort: 'count', rankingLineMetric: 'transactions' }
  }
  return { rankingSort: 'count', rankingLineMetric: 'lines' }
}

function tt(path: string): string {
  return t(`${i18nPrefix.value}.${path}`)
}

function rankingRowsFor(config: RankingTableConfig): SalesOrderItemListAnalyticsRankingRow[] {
  const data = rankings.value?.[config.dataKey]
  return Array.isArray(data) ? data : []
}

function rankingCountLabel(kind: RankingCountKind): string {
  if (kind === 'qty') return tt('rankings.qty')
  if (isReportMode.value) return tt('rankings.transactionFrequency')
  return tt('rankings.lineCount')
}

function rankingMetricHeaderLabel(kind: RankingCountKind): string {
  return effectiveRankingMetricMode.value === 'amount' ? tt('rankings.amount') : rankingCountLabel(kind)
}

function formatRankingMetric(row: SalesOrderItemListAnalyticsRankingRow, kind: RankingCountKind): string {
  if (effectiveRankingMetricMode.value === 'amount') return formatMoney(row.amount)
  if (kind === 'line' && isReportMode.value) return String(row.transactionCount ?? 0)
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
      label: tt('kpi.approvedCustomers'),
      value: String(s.approvedCustomerCount),
      ...def('kpi.approvedCustomers')
    },
    {
      key: 'approvedOrders',
      label: tt('kpi.approvedOrders'),
      value: String(s.approvedOrderCount),
      ...def('kpi.approvedOrders')
    },
    {
      key: 'approvedLines',
      label: tt('kpi.approvedLines'),
      value: String(s.approvedLineCount),
      ...def('kpi.approvedLines')
    },
    {
      key: 'approvedAmount',
      label: tt('kpi.approvedAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.approvedAmountUsd),
      valueFormat: 'money' as const,
      layout: 'split' as const,
      valueCaption: maskAmounts.value ? undefined : tt('kpi.usdCaption'),
      currencyCaption: currencyItems.length ? tt('kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined,
      ...def('kpi.approvedAmount')
    }
  ]
})

const profitKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'purchaseProfit',
      label: tt('kpi.purchaseProfit'),
      value: maskAmounts.value ? '—' : formatMoney(s.purchaseProfitUsd),
      valueFormat: 'money' as const,
      ...def('kpi.purchaseProfit')
    },
    {
      key: 'outboundProfit',
      label: tt('kpi.outboundProfit'),
      value: maskAmounts.value ? '—' : formatMoney(s.outboundProfitUsd),
      valueFormat: 'money' as const,
      ...def('kpi.outboundProfit')
    }
  ]
})

const inStockKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'inStockCustomers',
      label: tt('kpi.inStockCustomers'),
      value: String(s.inStockCustomerCount),
      ...def('kpi.inStockCustomers')
    },
    {
      key: 'inStockLines',
      label: tt('kpi.inStockLines'),
      value: String(s.inStockLineCount),
      ...def('kpi.inStockLines')
    },
    {
      key: 'inStockAmount',
      label: tt('kpi.inStockAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.inStockAmountUsd),
      valueFormat: 'money' as const,
      ...def('kpi.inStockAmount')
    },
    {
      key: 'maxStockAge',
      label: tt('kpi.maxStockAge'),
      value: formatDays(s.maxStockAgeDays),
      ...def('kpi.maxStockAge')
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
      label: tt('kpi.receivableCustomers'),
      value: String(s.receivableCustomerCount),
      ...def('kpi.receivableCustomers')
    },
    {
      key: 'receivableLines',
      label: tt('kpi.receivableLines'),
      value: String(s.receivableLineCount),
      ...def('kpi.receivableLines')
    },
    {
      key: 'receivableAmount',
      label: tt('kpi.receivableAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.receivableAmountUsd),
      valueFormat: 'money' as const,
      layout: currencyItems.length ? ('split' as const) : undefined,
      valueCaption: maskAmounts.value ? undefined : tt('kpi.usdCaption'),
      currencyCaption: currencyItems.length ? tt('kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined,
      ...def('kpi.receivableAmount')
    },
    {
      key: 'maxReceivableAge',
      label: tt('kpi.maxReceivableAge'),
      value: formatDays(s.maxReceivableAgeDays),
      ...def('kpi.maxReceivableAge')
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
  if (maskAmounts.value) return 'number'
  if (
    groupKey === 'itemStatus' ||
    groupKey === 'currency' ||
    groupKey === 'brandAmount' ||
    groupKey === 'salesUser' ||
    groupKey === 'purchaseProgress' ||
    groupKey === 'stockInProgress' ||
    groupKey === 'stockOutNotifyProgress' ||
    groupKey === 'receiptProgress' ||
    groupKey === 'invoiceProgress'
  ) {
    return 'money'
  }
  return 'number'
}

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `${i18nPrefix.value}.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  if (
    maskAmounts.value &&
    group.groupKey !== 'itemStatus' &&
    group.groupKey !== 'brandQty' &&
    group.groupKey !== 'dateCode'
  ) {
    return `${base}（${tt('breakdown.byCount')}）`
  }
  return base
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'itemStatus') {
    const statusNum = Number(item.key)
    if (statusNum === 0) return tt('itemStatus.normal')
    if (statusNum === 1) return tt('itemStatus.cancelled')
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

function reportQueryKey(q: SalesAnalyticsQuery): string {
  const rp = rankingQueryParams()
  return [
    q.viewLevel ?? '',
    q.departmentId ?? '',
    q.salesUserId ?? '',
    q.dateFrom ?? '',
    q.dateTo ?? '',
    q.groupBy ?? groupBy.value,
    rp.rankingSort ?? 'amount',
    rp.rankingLineMetric ?? ''
  ].join('|')
}

async function loadRankings() {
  if (!props.active) return
  try {
    if (isReportMode.value) {
      const q: SalesAnalyticsQuery = {
        ...(props.reportQuery ?? {}),
        groupBy: groupBy.value,
        ...rankingQueryParams()
      }
      rankings.value = await salesAnalyticsApi.getOrderItemsRankings(q)
    } else {
      const q: SalesOrderItemListAnalyticsQuery = {
        ...(props.filters ?? {}),
        dataset: 'listFilter',
        ...rankingQueryParams()
      }
      rankings.value = await salesOrderItemListAnalyticsApi.getRankings(q)
    }
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : tt('loadFailed')
    ElMessage.error(msg)
  }
}

async function loadData(force = false) {
  if (!props.active) return
  if (isReportMode.value) {
    const q = { ...(props.reportQuery ?? {}), groupBy: groupBy.value, ...rankingQueryParams() }
    const key = reportQueryKey(q)
    if (!force && loadedKey.value === key && dashboard.value) return
    loading.value = true
    try {
      const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
        salesAnalyticsApi.getOrderItemsDashboard(q),
        salesAnalyticsApi.getOrderItemsTrends(q),
        salesAnalyticsApi.getOrderItemsBreakdowns(q),
        salesAnalyticsApi.getOrderItemsRankings(q)
      ])
      dashboard.value = dash
      trends.value = trendRows
      breakdowns.value = breakdownRows
      rankings.value = rankingRows
      loadedKey.value = key
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : tt('loadFailed')
      ElMessage.error(msg)
    } finally {
      loading.value = false
    }
    return
  }

  loading.value = true
  try {
    const q: SalesOrderItemListAnalyticsQuery = {
      ...(props.filters ?? {}),
      dataset: 'listFilter',
      ...rankingQueryParams()
    }
    const [dash, breakdownRows, rankingRows] = await Promise.all([
      salesOrderItemListAnalyticsApi.getDashboard(q),
      salesOrderItemListAnalyticsApi.getBreakdowns(q),
      salesOrderItemListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = []
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : tt('loadFailed')
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
}

watch(
  () => ({
    mode: props.mode,
    active: props.active,
    filters: props.filters,
    reportQuery: props.reportQuery,
    groupBy: groupBy.value
  }),
  () => {
    if (isReportMode.value) {
      void loadData(false)
    } else {
      void loadData(true)
    }
  },
  { deep: true }
)
onMounted(() => void loadData(true))

watch(
  () => props.reportQuery?.groupBy,
  (g) => {
    if (g === 'day' || g === 'week' || g === 'month') groupBy.value = g
  },
  { immediate: true }
)

watch(rankingMetricMode, () => {
  if (maskAmounts.value) return
  void loadRankings()
})

defineExpose({ reload: () => loadData(true) })
</script>

<template>
  <div class="so-item-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <el-tag size="small" :type="isReportMode ? 'success' : 'info'" effect="plain">
        {{ tt('datasetTag') }}
      </el-tag>
      <span class="board-hint">{{ tt('hint') }}</span>
      <el-select v-if="showTrends" v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="tt('groupBy.day')" />
        <el-option value="week" :label="tt('groupBy.week')" />
        <el-option value="month" :label="tt('groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData(true)">{{ tt('refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ tt('sections.orderKpi') }}</h3>
      <AnalyticsKpiGrid :items="orderKpiItems" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ tt('sections.profitKpi') }}</h3>
      <AnalyticsKpiGrid :items="profitKpiItems" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ tt('sections.inStockKpi') }}</h3>
      <AnalyticsKpiGrid :items="inStockKpiItems" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ tt('sections.receivableKpi') }}</h3>
      <AnalyticsKpiGrid :items="receivableKpiItems" />
    </section>

    <div v-if="showTrends" class="charts-row">
      <div class="card chart-panel">
        <AnalyticsPanelHeader :title="tt('sections.trendOrders')" v-bind="def('trend.orders')" />
        <AnalyticsTrendChart :points="trendOrderPoints" :value-suffix="tt('trendUnit.orders')" />
      </div>
      <div class="card chart-panel">
        <AnalyticsPanelHeader :title="tt('sections.trendLines')" v-bind="def('trend.lines')" />
        <AnalyticsTrendChart :points="trendLinePoints" :value-suffix="tt('trendUnit.lines')" />
      </div>
      <div class="card chart-panel">
        <AnalyticsPanelHeader
          :title="tt('sections.trendAmount')"
          :unit-caption="tt('trendUnit.moneyCaption')"
          v-bind="def('trend.amount')"
        />
        <AnalyticsTrendChart :points="trendAmountPoints" value-format="money" />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div v-for="group in breakdowns" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          v-if="isPieBreakdown(group.groupKey)"
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          :value-format="breakdownValueFormat(group.groupKey)"
          :unit-caption="
            breakdownValueFormat(group.groupKey) === 'money'
              ? tt('trendUnit.moneyCaption')
              : undefined
          "
          v-bind="def(`breakdown.${group.groupKey}`)"
        />
        <AnalyticsBreakdownChart
          v-else
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          :value-format="breakdownValueFormat(group.groupKey)"
          :unit-caption="
            breakdownValueFormat(group.groupKey) === 'money'
              ? tt('trendUnit.moneyCaption')
              : undefined
          "
          v-bind="def(`breakdown.${group.groupKey}`)"
        />
      </div>
    </div>

    <div class="rankings-section">
      <div v-if="!maskAmounts" class="rankings-toolbar">
        <span class="rankings-toolbar-label">{{ tt('rankings.metricMode') }}</span>
        <el-radio-group v-model="rankingMetricMode" size="small">
          <el-radio-button value="amount">{{ tt('rankings.amount') }}</el-radio-button>
          <el-radio-button value="count">{{ rankingCountModeLabel }}</el-radio-button>
        </el-radio-group>
      </div>

      <div class="rankings-row">
        <div v-for="table in rankingTables" :key="table.key" class="card ranking-panel">
          <AnalyticsPanelHeader
            :title="tt(`rankings.${table.titleKey}`)"
            v-bind="rankingPanelDef(table.titleKey)"
          />
          <el-table :data="rankingRowsFor(table)" size="small" stripe class="ranking-table">
            <el-table-column
              prop="name"
              :label="tt('rankings.name')"
              min-width="200"
              class-name="ranking-name-col"
            >
              <template #default="{ row }">
                <span class="ranking-name-cell">{{ row.name }}</span>
              </template>
            </el-table-column>
            <el-table-column width="168" align="right" class-name="ranking-metric-col">
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
                <span class="ranking-metric-value">{{ formatRankingMetric(row, table.countKind) }}</span>
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
  overflow: visible;
  text-overflow: clip;
}
</style>
