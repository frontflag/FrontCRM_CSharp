<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  rfqListAnalyticsApi,
  type RfqListAnalyticsDashboard,
  type RfqListAnalyticsQuery,
  type RfqListAnalyticsRankings,
  type RfqListAnalyticsTrendPoint
} from '@/api/rfqAnalytics'
import type { SalesAnalyticsBreakdownGroup } from '@/api/analytics/sales'
import { formatRfqTypeLabel } from '@/constants/rfqFormEnums'

const props = defineProps<{
  filters: RfqListAnalyticsQuery
}>()

const { t } = useI18n()

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const dashboard = ref<RfqListAnalyticsDashboard | null>(null)
const trends = ref<RfqListAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const rankings = ref<RfqListAnalyticsRankings | null>(null)

function formatRate(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(2)}%`
}

const kpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []

  return [
    {
      key: 'publishedCustomers',
      label: t('rfqList.board.kpi.publishedCustomers'),
      value: String(s.publishedCustomerCount)
    },
    {
      key: 'repeatCustomers',
      label: t('rfqList.board.kpi.repeatCustomers'),
      value: String(s.repeatInquiryCustomerCount)
    },
    {
      key: 'repeatRfqs',
      label: t('rfqList.board.kpi.repeatRfqs'),
      value: String(s.repeatInquiryRfqCount)
    },
    {
      key: 'rfqCount',
      label: t('rfqList.board.kpi.rfqCount'),
      value: String(s.rfqCount)
    },
    {
      key: 'rfqItemCount',
      label: t('rfqList.board.kpi.rfqItemCount'),
      value: String(s.rfqItemCount)
    },
    {
      key: 'convertedLines',
      label: t('rfqList.board.kpi.convertedLines'),
      value: String(s.convertedLineCount)
    },
    {
      key: 'conversionRate',
      label: t('rfqList.board.kpi.conversionRate'),
      value: formatRate(s.conversionRate)
    }
  ]
})

const trendCustomerPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.customerCount }))
)
const trendRfqPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.rfqCount }))
)
const trendItemPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.rfqItemCount }))
)

const pieBreakdownKeys = new Set([
  'rfqType',
  'targetType',
  'industry',
  'assignedPurchaser',
  'quoteDistribution'
])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `rfqList.board.breakdown.${group.groupKey}`
  const translated = t(key)
  return translated !== key ? translated : group.groupLabel
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'rfqStatus') {
    const statusNum = Number(item.key)
    if (Number.isFinite(statusNum)) {
      const statusKey = `rfqList.status.${rfqStatusLabelKey(statusNum)}`
      const label = t(statusKey)
      return label !== statusKey ? label : item.label
    }
  }
  if (groupKey === 'rfqType') {
    const typeNum = Number(item.key)
    if (Number.isFinite(typeNum)) {
      const label = formatRfqTypeLabel(typeNum)
      return label !== '—' ? label : item.label
    }
  }
  if (groupKey === 'targetType') {
    const typeNum = Number(item.key)
    if (Number.isFinite(typeNum)) {
      const targetKey = `rfqDetail.targetType.${targetTypeLabelKey(typeNum)}`
      const label = t(targetKey)
      return label !== targetKey ? label : item.label
    }
  }
  if (groupKey === 'quoteDistribution') {
    const quoteKey = `rfqList.board.quoteDistribution.${item.key}`
    const label = t(quoteKey)
    return label !== quoteKey ? label : item.label
  }
  if (groupKey === 'assignedPurchaser' && item.key === '_unset') {
    return t('rfqList.board.breakdown.unassignedPurchaser')
  }
  if (groupKey === 'industry' && item.key === '_unset') {
    return t('rfqList.board.breakdown.unsetIndustry')
  }
  return item.label
}

function rfqStatusLabelKey(status: number): string {
  const map: Record<number, string> = {
    0: 'pending',
    1: 'assigned',
    2: 'processing',
    3: 'quoted',
    4: 'selected',
    5: 'converted',
    7: 'closed',
    8: 'cancelled'
  }
  return map[status] ?? 'unknown'
}

function targetTypeLabelKey(type: number): string {
  const map: Record<number, string> = {
    1: 'priceCompare',
    2: 'exclusive',
    3: 'urgent',
    4: 'normal'
  }
  return map[type] ?? 'unknown'
}

function localizedBreakdownItems(group: SalesAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function buildQuery(): RfqListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      rfqListAnalyticsApi.getDashboard(q),
      rfqListAnalyticsApi.getTrends(q),
      rfqListAnalyticsApi.getBreakdowns(q),
      rfqListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('rfqList.board.loadFailed')
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
  <div class="rfq-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <span class="board-hint">{{ t('rfqList.board.hint') }}</span>
      <el-select v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="t('rfqList.board.groupBy.day')" />
        <el-option value="week" :label="t('rfqList.board.groupBy.week')" />
        <el-option value="month" :label="t('rfqList.board.groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData">{{ t('rfqList.board.refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('rfqList.board.sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('rfqList.board.sections.trendCustomers') }}</h3>
        <AnalyticsTrendChart
          :points="trendCustomerPoints"
          :value-suffix="t('rfqList.board.trendUnit.customers')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('rfqList.board.sections.trendRfqs') }}</h3>
        <AnalyticsTrendChart
          :points="trendRfqPoints"
          :value-suffix="t('rfqList.board.trendUnit.rfqs')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('rfqList.board.sections.trendItems') }}</h3>
        <AnalyticsTrendChart
          :points="trendItemPoints"
          :value-suffix="t('rfqList.board.trendUnit.items')"
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
        <h3 class="section-title">{{ t('rfqList.board.rankings.customerByLineCount') }}</h3>
        <el-table :data="rankings?.customerByLineCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('rfqList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('rfqList.board.rankings.lineCount')" width="100" />
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('rfqList.board.rankings.salesUserByLineCount') }}</h3>
        <el-table :data="rankings?.salesUserByLineCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('rfqList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('rfqList.board.rankings.lineCount')" width="100" />
        </el-table>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.rfq-list-board {
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
