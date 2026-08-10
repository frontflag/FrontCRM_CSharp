<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  quoteListAnalyticsApi,
  type QuoteListAnalyticsDashboard,
  type QuoteListAnalyticsQuery,
  type QuoteListAnalyticsRankings,
  type QuoteListAnalyticsTrendPoint
} from '@/api/quoteListAnalytics'
import {
  purchaseAnalyticsApi,
  type PurchaseAnalyticsBreakdownGroup,
  type PurchaseAnalyticsQuery
} from '@/api/analytics/purchase'

const props = withDefaults(
  defineProps<{
    filters?: QuoteListAnalyticsQuery
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

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const loadedKey = ref('')
const dashboard = ref<QuoteListAnalyticsDashboard | null>(null)
const trends = ref<QuoteListAnalyticsTrendPoint[]>([])
const breakdowns = ref<PurchaseAnalyticsBreakdownGroup[]>([])
const rankings = ref<QuoteListAnalyticsRankings | null>(null)

const isReportMode = computed(() => props.mode === 'report')
const showTrends = computed(() => isReportMode.value)
const i18nPrefix = computed(() =>
  isReportMode.value ? 'purchaseAnalytics.quoteTab' : 'quoteList.board'
)

function tt(path: string): string {
  return t(`${i18nPrefix.value}.${path}`)
}

function formatRate(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(2)}%`
}

function formatMinutes(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(1)} ${tt('kpi.minutesUnit')}`
}

function formatAvgQuotes(v?: number | null): string {
  if (v == null) return '—'
  return v.toFixed(1)
}

const kpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []

  return [
    {
      key: 'quoteVendors',
      label: tt('kpi.quoteVendors'),
      value: String(s.quoteVendorCount)
    },
    {
      key: 'validQuotes',
      label: tt('kpi.validQuotes'),
      value: String(s.validQuoteCount)
    },
    {
      key: 'noQuoteFound',
      label: tt('kpi.noQuoteFound'),
      value: String(s.noQuoteFoundItemCount)
    },
    {
      key: 'rfqQuoteRate',
      label: tt('kpi.rfqQuoteRate'),
      value: formatRate(s.rfqQuoteRate)
    },
    {
      key: 'avgResponse',
      label: tt('kpi.avgResponse'),
      value: formatMinutes(s.avgResponseMinutes)
    },
    {
      key: 'avgQuotesPerItem',
      label: tt('kpi.avgQuotesPerItem'),
      value: formatAvgQuotes(s.avgQuotesPerRfqItem)
    },
    {
      key: 'convertedLines',
      label: tt('kpi.convertedLines'),
      value: String(s.convertedLineCount)
    },
    {
      key: 'quoteConversionRate',
      label: tt('kpi.quoteConversionRate'),
      value: formatRate(s.quoteConversionRate)
    }
  ]
})

const trendVendorPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.quoteVendorCount }))
)
const trendItemPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.rfqItemCount }))
)
const trendValidQuotePoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.validQuoteCount }))
)

const pieBreakdownKeys = new Set([
  'quoteDistribution',
  'labelType',
  'waferOrigin',
  'packageOrigin',
  'freeShipping',
  'brand',
  'assignedPurchaser',
  'quotePurchaser'
])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

/** 报价主状态等按报价主单计数；报价分布/分配采购员按需求明细计数 */
function breakdownUnitCaption(groupKey: string): string {
  if (groupKey === 'quoteDistribution' || groupKey === 'assignedPurchaser') {
    return tt('trendUnit.itemsCaption')
  }
  return tt('trendUnit.quotesCaption')
}

function breakdownTitle(group: PurchaseAnalyticsBreakdownGroup): string {
  const key = `${i18nPrefix.value}.breakdown.${group.groupKey}`
  const translated = t(key)
  return translated !== key ? translated : group.groupLabel
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'quoteStatus') {
    const statusNum = Number(item.key)
    if (statusNum === 0) return t('quoteList.status.new')
    if (statusNum === 1) return t('quoteList.status.won')
    if (statusNum === 2) return t('quoteList.status.closed')
  }
  if (groupKey === 'quoteDistribution') {
    const quoteKey = `quoteList.board.quoteDistribution.${item.key}`
    const label = t(quoteKey)
    return label !== quoteKey ? label : item.label
  }
  if (groupKey === 'labelType') {
    const labelKey = `quoteList.board.labelType.${item.key}`
    const label = t(labelKey)
    return label !== labelKey ? label : item.label
  }
  if (groupKey === 'waferOrigin' || groupKey === 'packageOrigin') {
    const originKey = `quoteList.board.origin.${item.key}`
    const label = t(originKey)
    return label !== originKey ? label : item.label
  }
  if (groupKey === 'freeShipping') {
    const shipKey = `quoteList.board.freeShipping.${item.key}`
    const label = t(shipKey)
    return label !== shipKey ? label : item.label
  }
  if ((groupKey === 'assignedPurchaser' || groupKey === 'quotePurchaser') && item.key === '_unset') {
    return tt('breakdown.unassignedPurchaser')
  }
  if (groupKey === 'brand' && item.key === '_unset') {
    return tt('breakdown.unset')
  }
  if (groupKey === 'brand' && item.key === '_other') {
    return tt('breakdown.other')
  }
  return item.label
}

function localizedBreakdownItems(group: PurchaseAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function formatQuoteRate(row: { amount?: number | null; orderCount: number }): string {
  if (row.amount != null) return `${row.amount.toFixed(2)}%`
  return '—'
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
        purchaseAnalyticsApi.getQuotesDashboard(q),
        purchaseAnalyticsApi.getQuotesTrends(q),
        purchaseAnalyticsApi.getQuotesBreakdowns(q),
        purchaseAnalyticsApi.getQuotesRankings(q)
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
    const q: QuoteListAnalyticsQuery = {
      ...(props.filters ?? {}),
      dataset: 'listFilter'
    }
    const [dash, breakdownRows, rankingRows] = await Promise.all([
      quoteListAnalyticsApi.getDashboard(q),
      quoteListAnalyticsApi.getBreakdowns(q),
      quoteListAnalyticsApi.getRankings(q)
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
  <div class="quote-list-board" v-loading="loading">
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
      <h3 class="section-title">{{ tt('sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div v-if="showTrends" class="charts-row">
      <div class="card chart-panel">
        <h3 class="section-title">{{ tt('sections.trendVendors') }}</h3>
        <AnalyticsTrendChart :points="trendVendorPoints" :value-suffix="tt('trendUnit.vendors')" />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ tt('sections.trendItems') }}</h3>
        <AnalyticsTrendChart :points="trendItemPoints" :value-suffix="tt('trendUnit.items')" />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ tt('sections.trendValidQuotes') }}</h3>
        <AnalyticsTrendChart :points="trendValidQuotePoints" :value-suffix="tt('trendUnit.quotes')" />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div v-for="group in breakdowns" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          v-if="isPieBreakdown(group.groupKey)"
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          value-format="number"
          :unit-caption="breakdownUnitCaption(group.groupKey)"
        />
        <AnalyticsBreakdownChart
          v-else
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          :unit-caption="breakdownUnitCaption(group.groupKey)"
        />
      </div>
    </div>

    <div class="rankings-row">
      <div class="card ranking-panel">
        <h3 class="section-title">{{ tt('rankings.vendorByRfqItemCount') }}</h3>
        <el-table :data="rankings?.vendorByRfqItemCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="tt('rankings.name')" />
          <el-table-column prop="orderCount" :label="tt('rankings.rfqItemCount')" width="110" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ tt('rankings.purchaserByQuoteCount') }}</h3>
        <el-table :data="rankings?.purchaserByQuoteCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="tt('rankings.name')" />
          <el-table-column prop="orderCount" :label="tt('rankings.quoteCount')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ tt('rankings.purchaserByQuoteRate') }}</h3>
        <el-table :data="rankings?.purchaserByQuoteRate ?? []" size="small" stripe>
          <el-table-column prop="name" :label="tt('rankings.name')" />
          <el-table-column prop="orderCount" :label="tt('rankings.quoteCount')" width="90" />
          <el-table-column :label="tt('rankings.quoteRate')" width="90">
            <template #default="{ row }">{{ formatQuoteRate(row) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ tt('rankings.mpnByQuoteCount') }}</h3>
        <el-table :data="rankings?.mpnByQuoteCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="tt('rankings.name')" />
          <el-table-column prop="orderCount" :label="tt('rankings.quoteCount')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ tt('rankings.mpnByQty') }}</h3>
        <el-table :data="rankings?.mpnByQty ?? []" size="small" stripe>
          <el-table-column prop="name" :label="tt('rankings.name')" />
          <el-table-column prop="orderCount" :label="tt('rankings.qty')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ tt('rankings.brandByQuoteCount') }}</h3>
        <el-table :data="rankings?.brandByQuoteCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="tt('rankings.name')" />
          <el-table-column prop="orderCount" :label="tt('rankings.quoteCount')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ tt('rankings.brandByQty') }}</h3>
        <el-table :data="rankings?.brandByQty ?? []" size="small" stripe>
          <el-table-column prop="name" :label="tt('rankings.name')" />
          <el-table-column prop="orderCount" :label="tt('rankings.qty')" width="100" />
        </el-table>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.quote-list-board {
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
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.breakdown-row {
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
}

.rankings-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 16px;
}

.chart-panel,
.ranking-panel {
  min-height: 240px;
}
</style>
