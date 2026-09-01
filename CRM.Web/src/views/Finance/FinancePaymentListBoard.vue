<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import AnalyticsPanelHeader from '@/components/Analytics/AnalyticsPanelHeader.vue'
import { useAnalyticsDefinition } from '@/composables/useAnalyticsDefinition'
import {
  financePaymentListAnalyticsApi,
  type FinancePaymentListAnalyticsBreakdownGroup,
  type FinancePaymentListAnalyticsDashboard,
  type FinancePaymentListAnalyticsQuery,
  type FinancePaymentListAnalyticsRankingFacet,
  type FinancePaymentListAnalyticsRankingRow,
  type FinancePaymentListAnalyticsRankings,
  type FinancePaymentListAnalyticsTrendPoint
} from '@/api/financePaymentAnalytics'

const props = defineProps<{
  filters: FinancePaymentListAnalyticsQuery
}>()

const { t } = useI18n()
const { def } = useAnalyticsDefinition('financePaymentList.board')

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const dashboard = ref<FinancePaymentListAnalyticsDashboard | null>(null)
const trends = ref<FinancePaymentListAnalyticsTrendPoint[]>([])
const breakdowns = ref<FinancePaymentListAnalyticsBreakdownGroup[]>([])
const rankings = ref<FinancePaymentListAnalyticsRankings | null>(null)
const rankingMetricMode = ref<'amount' | 'count'>('amount')

const maskAmounts = computed(() => dashboard.value?.context.maskAmounts === true)
const effectiveRankingMetricMode = computed<'amount' | 'count'>(() =>
  maskAmounts.value ? 'count' : rankingMetricMode.value
)

function tt(path: string, values?: Record<string, unknown>): string {
  return values
    ? t(`financePaymentList.board.${path}`, values)
    : t(`financePaymentList.board.${path}`)
}

function formatAmountNumber(amount: number | null | undefined): string {
  if (amount == null) return '—'
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatOriginalMoney(amount: number | null | undefined, currencyLabel: string): string {
  if (amount == null) return '—'
  return `${formatAmountNumber(amount)} ${currencyLabel}`
}

function currencyCode(key?: string | null): number | undefined {
  if (!key) return undefined
  const n = Number(key)
  return Number.isFinite(n) && n > 0 ? n : undefined
}

function trendValueFormat(currencyKey: string): 'money' | 'homeMoney' | 'number' {
  if (currencyKey === '2') return 'money'
  if (currencyKey === '1') return 'homeMoney'
  return 'number'
}

const kpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  const currencyItems = maskAmounts.value
    ? []
    : (s.currencyLines ?? []).map((line) => ({
        currencyLabel: line.currencyLabel,
        originalText: formatOriginalMoney(line.originalAmount, line.currencyLabel),
        amountText: formatAmountNumber(line.originalAmount),
        currency: currencyCode(line.currencyKey),
        usdText: ''
      }))

  return [
    {
      key: 'vendors',
      label: tt('kpi.vendors'),
      value: String(s.vendorCount),
      ...def('kpi.vendors')
    },
    {
      key: 'amount',
      label: tt('kpi.amount'),
      value: maskAmounts.value ? '—' : tt('kpi.originalCaption'),
      valueFormat: 'text' as const,
      layout: 'split' as const,
      forceNewRow: true,
      currencyCaption: currencyItems.length ? tt('kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined,
      ...def('kpi.amount')
    }
  ]
})

const trendHeaderPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.headerCount }))
)

const trendAmountSeries = computed(() => {
  const first = trends.value[0]
  const currencies = first?.amountsByCurrency ?? []
  return currencies.map((ccy) => ({
    currencyKey: ccy.currencyKey,
    currencyLabel: ccy.currencyLabel,
    points: trends.value.map((p) => {
      const row = (p.amountsByCurrency ?? []).find((x) => x.currencyKey === ccy.currencyKey)
      return { period: p.period, value: maskAmounts.value ? 0 : (row?.amount ?? 0) }
    })
  }))
})

const verificationBreakdown = computed(() =>
  breakdowns.value.find((g) => g.groupKey === 'verificationStatus') ?? null
)

const purchaseUserBreakdowns = computed(() =>
  breakdowns.value.filter((g) => g.groupKey === 'purchaseUser')
)

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'verificationStatus') {
    if (item.key === '2') return tt('verification.complete')
    if (item.key === '1') return tt('verification.partial')
    if (item.key === '0') return tt('verification.pending')
  }
  if (item.key === '_unset') {
    return groupKey === 'purchaseUser' ? tt('unsetPurchaseUser') : tt('unsetVendor')
  }
  return item.label
}

function localizedBreakdownItems(group: FinancePaymentListAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function purchaseUserPieFormat(currencyKey?: string | null): 'money' | 'number' {
  if (maskAmounts.value) return 'number'
  return currencyKey === '2' ? 'money' : 'number'
}

function rankingMetricHeaderLabel(): string {
  return effectiveRankingMetricMode.value === 'amount' ? tt('rankings.amount') : tt('rankings.headerCount')
}

function formatRankingMetric(row: FinancePaymentListAnalyticsRankingRow, currencyLabel: string): string {
  if (effectiveRankingMetricMode.value === 'amount')
    return formatOriginalMoney(row.amount, currencyLabel)
  return String(row.orderCount ?? 0)
}

function toggleRankingMetricMode() {
  if (maskAmounts.value) return
  rankingMetricMode.value = rankingMetricMode.value === 'amount' ? 'count' : 'amount'
}

function rankingTitle(
  kind: 'vendorByAmount' | 'purchaseUserByAmount',
  facet: FinancePaymentListAnalyticsRankingFacet
): string {
  return `${tt(`rankings.${kind}`)} · ${facet.currencyLabel}`
}

function buildQuery(): FinancePaymentListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      financePaymentListAnalyticsApi.getDashboard(q),
      financePaymentListAnalyticsApi.getTrends(q),
      financePaymentListAnalyticsApi.getBreakdowns(q),
      financePaymentListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : tt('loadFailed')
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
  <div class="fp-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <el-tag size="small" type="info" effect="plain">{{ tt('datasetTag') }}</el-tag>
      <span class="board-hint">{{ tt('hint') }}</span>
      <el-select v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="tt('groupBy.day')" />
        <el-option value="week" :label="tt('groupBy.week')" />
        <el-option value="month" :label="tt('groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData">{{ tt('refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ tt('sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <AnalyticsPanelHeader :title="tt('sections.trendHeaders')" v-bind="def('trend.headers')" />
        <AnalyticsTrendChart :points="trendHeaderPoints" :value-suffix="tt('trendUnit.headers')" />
      </div>
      <div
        v-for="series in trendAmountSeries"
        v-show="!maskAmounts"
        :key="series.currencyKey"
        class="card chart-panel"
      >
        <AnalyticsPanelHeader
          :title="`${tt('sections.trendAmount')} · ${series.currencyLabel}`"
          :unit-caption="tt('trendUnit.originalCaption', { currency: series.currencyLabel })"
          v-bind="def('trend.amount')"
        />
        <AnalyticsTrendChart
          :points="series.points"
          :value-format="maskAmounts ? 'number' : trendValueFormat(series.currencyKey)"
        />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div v-if="verificationBreakdown" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          :title="tt('breakdown.verificationStatus')"
          :items="localizedBreakdownItems(verificationBreakdown)"
          value-format="number"
          :unit-caption="tt('trendUnit.headers')"
          v-bind="def('breakdown.verificationStatus')"
        />
      </div>
      <div
        v-for="group in purchaseUserBreakdowns"
        :key="`${group.groupKey}-${group.currencyKey}`"
        class="card chart-panel"
      >
        <AnalyticsBreakdownPieChart
          :title="`${tt('breakdown.purchaseUser')} · ${group.currencyLabel}`"
          :items="localizedBreakdownItems(group)"
          :value-format="purchaseUserPieFormat(group.currencyKey)"
          :unit-caption="
            maskAmounts
              ? tt('trendUnit.headers')
              : tt('trendUnit.originalCaption', { currency: group.currencyLabel || '' })
          "
          v-bind="def('breakdown.purchaseUser')"
        />
      </div>
    </div>

    <div class="rankings-section">
      <div v-if="!maskAmounts" class="rankings-toolbar">
        <span class="rankings-toolbar-label">{{ tt('rankings.metricMode') }}</span>
        <el-radio-group v-model="rankingMetricMode" size="small">
          <el-radio-button value="amount">{{ tt('rankings.amount') }}</el-radio-button>
          <el-radio-button value="count">{{ tt('rankings.headerCount') }}</el-radio-button>
        </el-radio-group>
      </div>

      <div class="rankings-row">
        <div
          v-for="facet in rankings?.vendorByAmount ?? []"
          :key="`vendor-${facet.currencyKey}`"
          class="card ranking-panel"
        >
          <AnalyticsPanelHeader
            :title="rankingTitle('vendorByAmount', facet)"
            v-bind="def('rankings.vendorByAmount')"
          />
          <el-table :data="facet.rows" size="small" stripe class="ranking-table">
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
                  <span>{{ rankingMetricHeaderLabel() }}</span>
                  <span v-if="!maskAmounts" class="ranking-metric-switch" aria-hidden="true">⇄</span>
                </button>
              </template>
              <template #default="{ row }">
                <span class="ranking-metric-value">{{
                  formatRankingMetric(row, facet.currencyLabel)
                }}</span>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <div
          v-for="facet in rankings?.purchaseUserByAmount ?? []"
          :key="`buyer-${facet.currencyKey}`"
          class="card ranking-panel"
        >
          <AnalyticsPanelHeader
            :title="rankingTitle('purchaseUserByAmount', facet)"
            v-bind="def('rankings.purchaseUserByAmount')"
          />
          <el-table :data="facet.rows" size="small" stripe class="ranking-table">
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
                  <span>{{ rankingMetricHeaderLabel() }}</span>
                  <span v-if="!maskAmounts" class="ranking-metric-switch" aria-hidden="true">⇄</span>
                </button>
              </template>
              <template #default="{ row }">
                <span class="ranking-metric-value">{{
                  formatRankingMetric(row, facet.currencyLabel)
                }}</span>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.fp-list-board {
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
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 16px;
}

.ranking-table {
  width: 100%;
}

.ranking-name-cell {
  word-break: break-word;
}

.ranking-metric-header {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  padding: 0;
}

.ranking-metric-header--toggle {
  cursor: pointer;
}

.ranking-metric-switch {
  opacity: 0.65;
}
</style>
