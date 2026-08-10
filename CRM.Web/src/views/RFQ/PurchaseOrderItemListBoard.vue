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
  purchaseOrderItemListAnalyticsApi,
  type PurchaseOrderItemListAnalyticsDashboard,
  type PurchaseOrderItemListAnalyticsQuery,
  type PurchaseOrderItemListAnalyticsRankingRow,
  type PurchaseOrderItemListAnalyticsRankings,
  type PurchaseOrderItemListAnalyticsTrendPoint
} from '@/api/purchaseOrderItemAnalytics'
import {
  purchaseAnalyticsApi,
  type PurchaseAnalyticsBreakdownGroup,
  type PurchaseAnalyticsQuery
} from '@/api/analytics/purchase'

const props = withDefaults(
  defineProps<{
    filters?: PurchaseOrderItemListAnalyticsQuery
    reportQuery?: PurchaseAnalyticsQuery
    mode?: 'list' | 'report'
    active?: boolean
  }>(),
  {
    mode: 'list',
    active: true
  }
)

const { t } = useI18n()
const { def } = useAnalyticsDefinition('purchaseOrderItemList.board')

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const loadedKey = ref('')
const dashboard = ref<PurchaseOrderItemListAnalyticsDashboard | null>(null)
const trends = ref<PurchaseOrderItemListAnalyticsTrendPoint[]>([])
const breakdowns = ref<PurchaseAnalyticsBreakdownGroup[]>([])
const rankings = ref<PurchaseOrderItemListAnalyticsRankings | null>(null)
const rankingMetricMode = ref<'amount' | 'count'>('amount')

const isReportMode = computed(() => props.mode === 'report')
const showTrends = computed(() => isReportMode.value)
const i18nPrefix = computed(() =>
  isReportMode.value ? 'purchaseAnalytics.orderTab' : 'purchaseOrderItemList.board'
)

type RankingCountKind = 'line' | 'qty'

interface RankingTableConfig {
  key: string
  titleKey: string
  dataKey: keyof PurchaseOrderItemListAnalyticsRankings
  countKind: RankingCountKind
}

const rankingTables: RankingTableConfig[] = [
  { key: 'vendor', titleKey: 'vendorByAmount', dataKey: 'vendorByAmount', countKind: 'line' },
  { key: 'pnAmount', titleKey: 'pnByAmount', dataKey: 'pnByAmount', countKind: 'line' },
  { key: 'pnQty', titleKey: 'pnByQty', dataKey: 'pnByQty', countKind: 'qty' },
  { key: 'brandAmount', titleKey: 'brandByAmount', dataKey: 'brandByAmount', countKind: 'line' },
  { key: 'brandQty', titleKey: 'brandByQty', dataKey: 'brandByQty', countKind: 'qty' },
  { key: 'purchaseUser', titleKey: 'purchaseUserByAmount', dataKey: 'purchaseUserByAmount', countKind: 'line' }
]

const PO_ITEM_STATUS_I18N_KEY: Record<number, string> = {
  1: 'new',
  2: 'pendingReview',
  10: 'approved',
  20: 'pendingConfirm',
  30: 'confirmed',
  40: 'paid',
  50: 'shipped',
  60: 'stockedIn',
  100: 'completed',
  [-1]: 'reviewFailed',
  [-2]: 'cancelled'
}

const maskAmounts = computed(() => dashboard.value?.context.maskAmounts === true)

const effectiveRankingMetricMode = computed<'amount' | 'count'>(() =>
  maskAmounts.value ? 'count' : rankingMetricMode.value
)

function tt(path: string): string {
  return t(`${i18nPrefix.value}.${path}`)
}

function rankingRowsFor(config: RankingTableConfig): PurchaseOrderItemListAnalyticsRankingRow[] {
  const data = rankings.value?.[config.dataKey]
  return Array.isArray(data) ? data : []
}

function rankingCountLabel(kind: RankingCountKind): string {
  return kind === 'qty' ? tt('rankings.qty') : tt('rankings.lineCount')
}

function rankingMetricHeaderLabel(kind: RankingCountKind): string {
  return effectiveRankingMetricMode.value === 'amount' ? tt('rankings.amount') : rankingCountLabel(kind)
}

function formatRankingMetric(row: PurchaseOrderItemListAnalyticsRankingRow): string {
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
      key: 'approvedVendors',
      label: tt('kpi.approvedVendors'),
      value: String(s.approvedVendorCount),
      ...def('kpi.approvedVendors')
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

const inStockKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'inStockVendors',
      label: tt('kpi.inStockVendors'),
      value: String(s.inStockVendorCount),
      ...def('kpi.inStockVendors')
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

const payableKpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  const currencyItems = maskAmounts.value
    ? []
    : (s.payableCurrencyLines ?? []).map((line) => ({
        currencyLabel: line.currencyLabel,
        originalText: formatOriginalMoney(line.originalAmount, line.currencyLabel),
        usdText: ''
      }))

  return [
    {
      key: 'payableVendors',
      label: tt('kpi.payableVendors'),
      value: String(s.payableVendorCount),
      ...def('kpi.payableVendors')
    },
    {
      key: 'payableLines',
      label: tt('kpi.payableLines'),
      value: String(s.payableLineCount),
      ...def('kpi.payableLines')
    },
    {
      key: 'payableAmount',
      label: tt('kpi.payableAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.payableAmountUsd),
      valueFormat: 'money' as const,
      layout: currencyItems.length ? ('split' as const) : undefined,
      valueCaption: maskAmounts.value ? undefined : tt('kpi.usdCaption'),
      currencyCaption: currencyItems.length ? tt('kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined,
      ...def('kpi.payableAmount')
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
  'paymentRequestProgress',
  'paymentProgress',
  'purchaseProgress',
  'stockInProgress',
  'invoiceProgress',
  'brandQty',
  'brandAmount',
  'dateCode',
  'purchaseUser'
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
    groupKey === 'purchaseUser' ||
    groupKey === 'paymentRequestProgress' ||
    groupKey === 'paymentProgress' ||
    groupKey === 'purchaseProgress' ||
    groupKey === 'stockInProgress' ||
    groupKey === 'invoiceProgress'
  ) {
    return 'money'
  }
  return 'number'
}

function breakdownTitle(group: PurchaseAnalyticsBreakdownGroup): string {
  const key = `${i18nPrefix.value}.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  if (maskAmounts.value && group.groupKey !== 'itemStatus' && group.groupKey !== 'brandQty' && group.groupKey !== 'dateCode') {
    return `${base}（${tt('breakdown.byCount')}）`
  }
  return base
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'itemStatus') {
    const statusNum = Number(item.key)
    const slug = PO_ITEM_STATUS_I18N_KEY[statusNum]
    if (slug) return t(`purchaseOrderItemList.itemStatus.${slug}`)
  }
  if (groupKey === 'paymentRequestProgress') {
    return Number(item.key) >= 1
      ? t('purchaseOrderItemList.extendProgress.paymentRequestApplied')
      : t('purchaseOrderItemList.extendProgress.paymentRequestPending')
  }
  const progressKeyMap: Record<string, Record<number, string>> = {
    paymentProgress: { 0: 'paymentPending', 1: 'paymentPartial', 2: 'paymentDone' },
    purchaseProgress: { 0: 'purchasePending', 1: 'purchasePartial', 2: 'purchaseDone' },
    stockInProgress: { 0: 'stockInPending', 1: 'stockInPartial', 2: 'stockInDone' },
    invoiceProgress: { 0: 'invoicePending', 1: 'invoicePartial', 2: 'invoiceDone' }
  }
  const map = progressKeyMap[groupKey]
  if (map) {
    const statusNum = Number(item.key)
    const slot = map[statusNum]
    if (slot) return t(`purchaseOrderItemList.extendProgress.${slot}`)
  }
  return item.label
}

function localizedBreakdownItems(group: PurchaseAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function reportQueryKey(q: PurchaseAnalyticsQuery): string {
  return [
    q.viewLevel ?? '',
    q.departmentId ?? '',
    q.purchaseUserId ?? '',
    q.dateFrom ?? '',
    q.dateTo ?? '',
    q.groupBy ?? groupBy.value
  ].join('|')
}

async function loadData(force = false) {
  if (!props.active) return
  if (isReportMode.value) {
    const q = { ...(props.reportQuery ?? {}), groupBy: groupBy.value }
    const key = reportQueryKey(q)
    if (!force && loadedKey.value === key && dashboard.value) return
    loading.value = true
    try {
      const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
        purchaseAnalyticsApi.getOrderItemsDashboard(q),
        purchaseAnalyticsApi.getOrderItemsTrends(q),
        purchaseAnalyticsApi.getOrderItemsBreakdowns(q),
        purchaseAnalyticsApi.getOrderItemsRankings(q)
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
    const q: PurchaseOrderItemListAnalyticsQuery = {
      ...(props.filters ?? {}),
      dataset: 'listFilter'
    }
    const [dash, breakdownRows, rankingRows] = await Promise.all([
      purchaseOrderItemListAnalyticsApi.getDashboard(q),
      purchaseOrderItemListAnalyticsApi.getBreakdowns(q),
      purchaseOrderItemListAnalyticsApi.getRankings(q)
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
    if (isReportMode.value) void loadData(false)
    else void loadData(true)
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

defineExpose({ reload: () => loadData(true) })
</script>

<template>
  <div class="po-item-list-board" v-loading="loading">
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
      <h3 class="section-title">{{ tt('sections.inStockKpi') }}</h3>
      <AnalyticsKpiGrid :items="inStockKpiItems" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ tt('sections.payableKpi') }}</h3>
      <AnalyticsKpiGrid :items="payableKpiItems" />
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
          <el-radio-button value="count">{{ tt('rankings.lineCount') }}</el-radio-button>
        </el-radio-group>
      </div>

      <div class="rankings-row">
        <div v-for="table in rankingTables" :key="table.key" class="card ranking-panel">
          <AnalyticsPanelHeader
            :title="tt(`rankings.${table.titleKey}`)"
            v-bind="def(`rankings.${table.titleKey}`)"
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
.po-item-list-board {
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
