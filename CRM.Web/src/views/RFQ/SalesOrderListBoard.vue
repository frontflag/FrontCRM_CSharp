<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  salesOrderListAnalyticsApi,
  type SalesOrderListAnalyticsDashboard,
  type SalesOrderListAnalyticsQuery,
  type SalesOrderListAnalyticsRankings
} from '@/api/salesOrderAnalytics'
import type { SalesAnalyticsBreakdownGroup } from '@/api/analytics/sales'

const props = defineProps<{
  filters: SalesOrderListAnalyticsQuery
}>()

const { t } = useI18n()

const loading = ref(false)
const dashboard = ref<SalesOrderListAnalyticsDashboard | null>(null)
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
      key: 'approvedOrders',
      label: t('salesOrderList.board.kpi.approvedOrders'),
      value: String(s.approvedOrderCount)
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

/** 客户维指标已迁至 /reports/sales「客户」Tab；列表看板仅保留订单向分解 */
const orderFacingBreakdowns = computed(() =>
  breakdowns.value.filter((g) => !g.groupKey.startsWith('customer'))
)

const pieBreakdownKeys = new Set([
  'currency',
  'salesUser'
])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

function breakdownValueFormat(groupKey: string): 'money' | 'number' {
  if (maskAmounts.value) return 'number'
  if (groupKey === 'orderStatus' || groupKey === 'currency' || groupKey === 'salesUser') {
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
  return { ...props.filters }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, breakdownRows, rankingRows] = await Promise.all([
      salesOrderListAnalyticsApi.getDashboard(q),
      salesOrderListAnalyticsApi.getBreakdowns(q),
      salesOrderListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('salesOrderList.board.loadFailed')
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
}

watch(() => ({ ...props.filters }), () => void loadData(), { deep: true })
onMounted(() => void loadData())

defineExpose({ reload: loadData })
</script>

<template>
  <div class="so-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <span class="board-hint">{{ t('salesOrderList.board.hint') }}</span>
      <el-button type="primary" @click="loadData">{{ t('salesOrderList.board.refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('salesOrderList.board.sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row breakdown-row">
      <div v-for="group in orderFacingBreakdowns" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          v-if="isPieBreakdown(group.groupKey)"
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          :value-format="breakdownValueFormat(group.groupKey)"
          :unit-caption="
            breakdownValueFormat(group.groupKey) === 'money'
              ? t('salesOrderList.board.trendUnit.moneyCaption')
              : undefined
          "
        />
        <AnalyticsBreakdownChart
          v-else
          :title="breakdownTitle(group)"
          :items="localizedBreakdownItems(group)"
          :value-format="breakdownValueFormat(group.groupKey)"
          :unit-caption="
            breakdownValueFormat(group.groupKey) === 'money'
              ? t('salesOrderList.board.trendUnit.moneyCaption')
              : undefined
          "
        />
      </div>
    </div>

    <div class="rankings-row">
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('salesOrderList.board.rankings.salesUserByAmount') }}</h3>
        <el-table :data="rankings?.salesUserByAmount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('salesOrderList.board.rankings.name')" />
          <el-table-column
            prop="orderCount"
            :label="t('salesOrderList.board.rankings.orderCount')"
            width="90"
            align="right"
            header-align="right"
          />
          <el-table-column
            :label="t('salesOrderList.board.rankings.amount')"
            width="160"
            align="right"
            header-align="right"
          >
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
