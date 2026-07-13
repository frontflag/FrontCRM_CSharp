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
  type QuoteListAnalyticsQuery,
  type QuoteListAnalyticsRankings
} from '@/api/quoteListAnalytics'
import type { QuoteListAnalyticsDashboard, QuoteListAnalyticsTrendPoint } from '@/api/quoteListAnalytics'
import type { SalesAnalyticsBreakdownGroup } from '@/api/analytics/sales'

const props = defineProps<{
  filters: QuoteListAnalyticsQuery
}>()

const { t } = useI18n()

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const dashboard = ref<QuoteListAnalyticsDashboard | null>(null)
const trends = ref<QuoteListAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const rankings = ref<QuoteListAnalyticsRankings | null>(null)

function formatRate(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(2)}%`
}

function formatMinutes(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(1)} ${t('quoteList.board.kpi.minutesUnit')}`
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
      label: t('quoteList.board.kpi.quoteVendors'),
      value: String(s.quoteVendorCount)
    },
    {
      key: 'validQuotes',
      label: t('quoteList.board.kpi.validQuotes'),
      value: String(s.validQuoteCount)
    },
    {
      key: 'noQuoteFound',
      label: t('quoteList.board.kpi.noQuoteFound'),
      value: String(s.noQuoteFoundItemCount)
    },
    {
      key: 'rfqQuoteRate',
      label: t('quoteList.board.kpi.rfqQuoteRate'),
      value: formatRate(s.rfqQuoteRate)
    },
    {
      key: 'avgResponse',
      label: t('quoteList.board.kpi.avgResponse'),
      value: formatMinutes(s.avgResponseMinutes)
    },
    {
      key: 'avgQuotesPerItem',
      label: t('quoteList.board.kpi.avgQuotesPerItem'),
      value: formatAvgQuotes(s.avgQuotesPerRfqItem)
    },
    {
      key: 'convertedLines',
      label: t('quoteList.board.kpi.convertedLines'),
      value: String(s.convertedLineCount)
    },
    {
      key: 'quoteConversionRate',
      label: t('quoteList.board.kpi.quoteConversionRate'),
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

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `quoteList.board.breakdown.${group.groupKey}`
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
    return t('quoteList.board.breakdown.unassignedPurchaser')
  }
  if (groupKey === 'brand' && item.key === '_unset') {
    return t('quoteList.board.breakdown.unset')
  }
  if (groupKey === 'brand' && item.key === '_other') {
    return t('quoteList.board.breakdown.other')
  }
  return item.label
}

function localizedBreakdownItems(group: SalesAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function formatQuoteRate(row: { amount?: number | null; orderCount: number }): string {
  if (row.amount != null) return `${row.amount.toFixed(2)}%`
  return '—'
}

function buildQuery(): QuoteListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      quoteListAnalyticsApi.getDashboard(q),
      quoteListAnalyticsApi.getTrends(q),
      quoteListAnalyticsApi.getBreakdowns(q),
      quoteListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('quoteList.board.loadFailed')
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
  <div class="quote-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <span class="board-hint">{{ t('quoteList.board.hint') }}</span>
      <el-select v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="t('quoteList.board.groupBy.day')" />
        <el-option value="week" :label="t('quoteList.board.groupBy.week')" />
        <el-option value="month" :label="t('quoteList.board.groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData">{{ t('quoteList.board.refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('quoteList.board.sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('quoteList.board.sections.trendVendors') }}</h3>
        <AnalyticsTrendChart
          :points="trendVendorPoints"
          :value-suffix="t('quoteList.board.trendUnit.vendors')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('quoteList.board.sections.trendItems') }}</h3>
        <AnalyticsTrendChart
          :points="trendItemPoints"
          :value-suffix="t('quoteList.board.trendUnit.items')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('quoteList.board.sections.trendValidQuotes') }}</h3>
        <AnalyticsTrendChart
          :points="trendValidQuotePoints"
          :value-suffix="t('quoteList.board.trendUnit.quotes')"
        />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div v-for="group in breakdowns" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          v-if="isPieBreakdown(group.groupKey)"
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          value-format="number"
        />
        <AnalyticsBreakdownChart
          v-else
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
        />
      </div>
    </div>

    <div class="rankings-row">
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('quoteList.board.rankings.vendorByRfqItemCount') }}</h3>
        <el-table :data="rankings?.vendorByRfqItemCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('quoteList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('quoteList.board.rankings.rfqItemCount')" width="110" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('quoteList.board.rankings.purchaserByQuoteCount') }}</h3>
        <el-table :data="rankings?.purchaserByQuoteCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('quoteList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('quoteList.board.rankings.quoteCount')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('quoteList.board.rankings.purchaserByQuoteRate') }}</h3>
        <el-table :data="rankings?.purchaserByQuoteRate ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('quoteList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('quoteList.board.rankings.quoteCount')" width="90" />
          <el-table-column :label="t('quoteList.board.rankings.quoteRate')" width="90">
            <template #default="{ row }">{{ formatQuoteRate(row) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('quoteList.board.rankings.mpnByQuoteCount') }}</h3>
        <el-table :data="rankings?.mpnByQuoteCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('quoteList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('quoteList.board.rankings.quoteCount')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('quoteList.board.rankings.mpnByQty') }}</h3>
        <el-table :data="rankings?.mpnByQty ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('quoteList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('quoteList.board.rankings.qty')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('quoteList.board.rankings.brandByQuoteCount') }}</h3>
        <el-table :data="rankings?.brandByQuoteCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('quoteList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('quoteList.board.rankings.quoteCount')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('quoteList.board.rankings.brandByQty') }}</h3>
        <el-table :data="rankings?.brandByQty ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('quoteList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('quoteList.board.rankings.qty')" width="100" />
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
