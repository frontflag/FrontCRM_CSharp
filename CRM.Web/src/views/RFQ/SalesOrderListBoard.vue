<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  salesOrderListAnalyticsApi,
  type SalesOrderListAnalyticsDashboard,
  type SalesOrderListAnalyticsQuery,
  type SalesOrderListAnalyticsRankings,
  type SalesOrderListAnalyticsTrendPoint
} from '@/api/salesOrderAnalytics'
import type { SalesAnalyticsBreakdownGroup } from '@/api/analytics/sales'
import { useCustomerDictStore } from '@/stores/customerDict'

const props = defineProps<{
  filters: SalesOrderListAnalyticsQuery
}>()

const { t } = useI18n()
const customerDict = useCustomerDictStore()

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const dashboard = ref<SalesOrderListAnalyticsDashboard | null>(null)
const trends = ref<SalesOrderListAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const rankings = ref<SalesOrderListAnalyticsRankings | null>(null)

const maskAmounts = computed(() => dashboard.value?.context.maskAmounts === true)

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
      key: 'approvedCustomers',
      label: t('salesOrderList.board.kpi.approvedCustomers'),
      value: String(s.approvedCustomerCount)
    },
    {
      key: 'repeatCustomers',
      label: t('salesOrderList.board.kpi.repeatCustomers'),
      value: String(s.repeatCustomerCount)
    },
    {
      key: 'approvedOrders',
      label: t('salesOrderList.board.kpi.approvedOrders'),
      value: String(s.approvedOrderCount)
    },
    {
      key: 'repeatOrders',
      label: t('salesOrderList.board.kpi.repeatOrders'),
      value: String(s.repeatOrderCount)
    },
    {
      key: 'approvedAmount',
      label: t('salesOrderList.board.kpi.approvedAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.approvedAmountUsd),
      valueFormat: 'money' as const,
      layout: 'split' as const,
      valueCaption: maskAmounts.value ? undefined : t('salesOrderList.board.kpi.usdCaption'),
      currencyCaption: currencyItems.length ? t('salesOrderList.board.kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined
    }
  ]
})

const trendOrderPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.approvedOrderCount }))
)

const trendAmountPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.approvedAmountUsd ?? 0 }))
)

const pieBreakdownKeys = new Set([
  'currency',
  'customerType',
  'customerLevel',
  'customerIndustry',
  'salesUser'
])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

function breakdownValueFormat(groupKey: string): 'money' | 'number' {
  if (groupKey === 'currency' && !maskAmounts.value) return 'money'
  if (
    (groupKey === 'customerType' ||
      groupKey === 'customerLevel' ||
      groupKey === 'customerIndustry' ||
      groupKey === 'salesUser') &&
    !maskAmounts.value
  ) {
    return 'money'
  }
  return 'number'
}

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `salesOrderList.board.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  if (group.groupKey === 'currency' && maskAmounts.value) {
    return `${base}（${t('salesOrderList.board.breakdown.byCount')}）`
  }
  if (group.groupKey === 'salesUser' && maskAmounts.value) {
    return `${base}（${t('salesOrderList.board.breakdown.byCount')}）`
  }
  return base
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'customerType' && item.key !== '_unset') {
    const typeNum = Number(item.key)
    if (Number.isFinite(typeNum)) {
      return customerDict.typeLabel(typeNum)
    }
  }
  if (groupKey === 'customerLevel' && item.key !== '_unset') {
    return customerDict.levelLabel(item.key)
  }
  if (groupKey === 'customerIndustry' && item.key !== '_unset') {
    return customerDict.industryLabel(item.key)
  }
  if (groupKey === 'orderStatus') {
    const statusNum = Number(item.key)
    if (Number.isFinite(statusNum)) {
      const statusKey = `salesOrderList.status.${statusLabelKey(statusNum)}`
      const label = t(statusKey)
      return label !== statusKey ? label : item.label
    }
  }
  return item.label
}

function statusLabelKey(status: number): string {
  const map: Record<number, string> = {
    1: 'new',
    2: 'pendingReview',
    10: 'approved',
    20: 'inProgress',
    100: 'completed',
    [-1]: 'reviewFailed',
    [-2]: 'cancelled'
  }
  return map[status] ?? 'unknown'
}

function localizedBreakdownItems(group: SalesAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function buildQuery(): SalesOrderListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    await customerDict.ensureLoaded()
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      salesOrderListAnalyticsApi.getDashboard(q),
      salesOrderListAnalyticsApi.getTrends(q),
      salesOrderListAnalyticsApi.getBreakdowns(q),
      salesOrderListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('salesOrderList.board.loadFailed')
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
  <div class="so-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <span class="board-hint">{{ t('salesOrderList.board.hint') }}</span>
      <el-select v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="t('salesOrderList.board.groupBy.day')" />
        <el-option value="week" :label="t('salesOrderList.board.groupBy.week')" />
        <el-option value="month" :label="t('salesOrderList.board.groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData">{{ t('salesOrderList.board.refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('salesOrderList.board.sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('salesOrderList.board.sections.trendOrders') }}</h3>
        <AnalyticsTrendChart
          :points="trendOrderPoints"
          :value-suffix="t('salesOrderList.board.trendUnit.orders')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('salesOrderList.board.sections.trendAmount') }}</h3>
        <AnalyticsTrendChart
          :points="trendAmountPoints"
          value-format="money"
          :unit-caption="t('salesOrderList.board.trendUnit.moneyCaption')"
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

    <div class="rankings-row">
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('salesOrderList.board.rankings.customerByAmount') }}</h3>
        <el-table :data="rankings?.customerByAmount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('salesOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('salesOrderList.board.rankings.orderCount')" width="90" />
          <el-table-column :label="t('salesOrderList.board.rankings.amount')" width="140">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('salesOrderList.board.rankings.customerByOrderCount') }}</h3>
        <el-table :data="rankings?.customerByOrderCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('salesOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('salesOrderList.board.rankings.orderCount')" width="90" />
          <el-table-column :label="t('salesOrderList.board.rankings.amount')" width="140">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('salesOrderList.board.rankings.customerByRepeat') }}</h3>
        <el-table :data="rankings?.customerByRepeatOrderCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('salesOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('salesOrderList.board.rankings.repeatOrders')" width="100" />
          <el-table-column :label="t('salesOrderList.board.rankings.amount')" width="140">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('salesOrderList.board.rankings.salesUserByAmount') }}</h3>
        <el-table :data="rankings?.salesUserByAmount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('salesOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('salesOrderList.board.rankings.orderCount')" width="90" />
          <el-table-column :label="t('salesOrderList.board.rankings.amount')" width="140">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.so-list-board {
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
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.breakdown-row {
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
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
