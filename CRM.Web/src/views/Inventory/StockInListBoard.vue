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
  stockInListAnalyticsApi,
  type StockInListAnalyticsDashboard,
  type StockInListAnalyticsQuery,
  type StockInListAnalyticsRankingRow,
  type StockInListAnalyticsRankings,
  type StockInListAnalyticsTrendPoint
} from '@/api/stockInAnalytics'
import type { SalesAnalyticsBreakdownGroup } from '@/api/analytics/sales'
import { resolveStockInTypeLabelKey } from '@/constants/stockInType'

const props = defineProps<{
  filters: StockInListAnalyticsQuery
}>()

const { t } = useI18n()
const { def } = useAnalyticsDefinition('stockInList.board')

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const dashboard = ref<StockInListAnalyticsDashboard | null>(null)
const trends = ref<StockInListAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const rankings = ref<StockInListAnalyticsRankings | null>(null)
const rankingMetricMode = ref<'amount' | 'count'>('amount')

const maskAmounts = computed(() => dashboard.value?.context.maskAmounts === true)
const effectiveRankingMetricMode = computed<'amount' | 'count'>(() =>
  maskAmounts.value ? 'count' : rankingMetricMode.value
)

function tt(path: string): string {
  return t(`stockInList.board.${path}`)
}

interface RankingTableConfig {
  key: string
  titleKey: string
  dataKey: keyof StockInListAnalyticsRankings
}

const rankingTables: RankingTableConfig[] = [
  { key: 'vendor', titleKey: 'vendorByAmount', dataKey: 'vendorByAmount' },
  { key: 'purchaseUser', titleKey: 'purchaseUserByAmount', dataKey: 'purchaseUserByAmount' }
]

function rankingRowsFor(config: RankingTableConfig): StockInListAnalyticsRankingRow[] {
  const data = rankings.value?.[config.dataKey]
  return Array.isArray(data) ? data : []
}

function rankingMetricHeaderLabel(): string {
  return effectiveRankingMetricMode.value === 'amount' ? tt('rankings.amount') : tt('rankings.headerCount')
}

function formatRankingMetric(row: StockInListAnalyticsRankingRow): string {
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

const kpiItems = computed(() => {
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
      key: 'vendors',
      label: tt('kpi.vendors'),
      value: String(s.vendorCount),
      ...def('kpi.vendors')
    },
    {
      key: 'headers',
      label: tt('kpi.headers'),
      value: String(s.headerCount),
      ...def('kpi.headers')
    },
    {
      key: 'amount',
      label: tt('kpi.amount'),
      value: maskAmounts.value ? '—' : formatMoney(s.amountUsd),
      valueFormat: 'money' as const,
      layout: 'split' as const,
      forceNewRow: true,
      valueCaption: maskAmounts.value ? undefined : tt('kpi.usdCaption'),
      currencyCaption: currencyItems.length ? tt('kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined,
      ...def('kpi.amount')
    }
  ]
})

const trendHeaderPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.headerCount }))
)

const trendAmountPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: maskAmounts.value ? 0 : (p.amountUsd ?? 0) }))
)

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `breakdown.${group.groupKey}`
  const i18nKey = `stockInList.board.${key}`
  return t(i18nKey) === i18nKey ? group.groupLabel : t(i18nKey)
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'stockInType') {
    const typeKey = resolveStockInTypeLabelKey(item.key === '10' ? 10 : Number(item.key))
    return t(`stockInList.stockInTypeLabels.${typeKey}`)
  }
  if (item.key === '_unset') {
    return groupKey === 'purchaseUser' ? tt('unsetPurchaseUser') : tt('unsetVendor')
  }
  return item.label
}

function localizedBreakdownItems(group: SalesAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function breakdownValueFormat(): 'money' | 'number' {
  return maskAmounts.value ? 'number' : 'money'
}

function buildQuery(): StockInListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      stockInListAnalyticsApi.getDashboard(q),
      stockInListAnalyticsApi.getTrends(q),
      stockInListAnalyticsApi.getBreakdowns(q),
      stockInListAnalyticsApi.getRankings(q)
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
  <div class="si-list-board" v-loading="loading">
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
      <div class="card chart-panel">
        <AnalyticsPanelHeader
          :title="tt('sections.trendAmount')"
          :unit-caption="tt('trendUnit.moneyCaption')"
          v-bind="def('trend.amount')"
        />
        <AnalyticsTrendChart
          :points="trendAmountPoints"
          value-format="money"
        />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div v-for="group in breakdowns" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          :value-format="breakdownValueFormat()"
          :unit-caption="
            breakdownValueFormat() === 'money' ? tt('trendUnit.moneyCaption') : undefined
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
          <el-radio-button value="count">{{ tt('rankings.headerCount') }}</el-radio-button>
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
                  <span>{{ rankingMetricHeaderLabel() }}</span>
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
.si-list-board {
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
