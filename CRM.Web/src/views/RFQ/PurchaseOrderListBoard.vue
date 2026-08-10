<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  purchaseOrderListAnalyticsApi,
  type PurchaseOrderListAnalyticsDashboard,
  type PurchaseOrderListAnalyticsQuery,
  type PurchaseOrderListAnalyticsRankings,
  type PurchaseOrderListAnalyticsTrendPoint
} from '@/api/purchaseOrderAnalytics'
import type { SalesAnalyticsBreakdownGroup } from '@/api/analytics/sales'
import { useVendorDictStore } from '@/stores/vendorDict'

const props = defineProps<{
  filters: PurchaseOrderListAnalyticsQuery
}>()

const { t } = useI18n()
const vendorDict = useVendorDictStore()

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
const dashboard = ref<PurchaseOrderListAnalyticsDashboard | null>(null)
const trends = ref<PurchaseOrderListAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const rankings = ref<PurchaseOrderListAnalyticsRankings | null>(null)

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
      key: 'approvedVendors',
      label: t('purchaseOrderList.board.kpi.approvedVendors'),
      value: String(s.approvedVendorCount)
    },
    {
      key: 'repeatVendors',
      label: t('purchaseOrderList.board.kpi.repeatVendors'),
      value: String(s.repeatVendorCount)
    },
    {
      key: 'approvedOrders',
      label: t('purchaseOrderList.board.kpi.approvedOrders'),
      value: String(s.approvedOrderCount)
    },
    {
      key: 'repeatOrders',
      label: t('purchaseOrderList.board.kpi.repeatOrders'),
      value: String(s.repeatOrderCount)
    },
    {
      key: 'approvedAmount',
      label: t('purchaseOrderList.board.kpi.approvedAmount'),
      value: maskAmounts.value ? '—' : formatMoney(s.approvedAmountUsd),
      valueFormat: 'money' as const,
      layout: 'split' as const,
      valueCaption: maskAmounts.value ? undefined : t('purchaseOrderList.board.kpi.usdCaption'),
      currencyCaption: currencyItems.length ? t('purchaseOrderList.board.kpi.originalCaption') : undefined,
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
  'vendorIdentity',
  'vendorLevel',
  'vendorIndustry',
  'purchaseUser'
])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

function breakdownValueFormat(groupKey: string): 'money' | 'number' {
  if (maskAmounts.value) return 'number'
  if (
    groupKey === 'orderStatus' ||
    groupKey === 'currency' ||
    groupKey === 'vendorIdentity' ||
    groupKey === 'vendorLevel' ||
    groupKey === 'vendorIndustry' ||
    groupKey === 'purchaseUser'
  ) {
    return 'money'
  }
  return 'number'
}

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `purchaseOrderList.board.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  if (group.groupKey === 'currency' && maskAmounts.value) {
    return `${base}（${t('purchaseOrderList.board.breakdown.byCount')}）`
  }
  if (group.groupKey === 'purchaseUser' && maskAmounts.value) {
    return `${base}（${t('purchaseOrderList.board.breakdown.byCount')}）`
  }
  return base
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'vendorIdentity' && item.key !== '_unset') {
    const idNum = Number(item.key)
    if (Number.isFinite(idNum)) {
      return vendorDict.identityLabel(idNum)
    }
  }
  if (groupKey === 'vendorLevel' && item.key !== '_unset') {
    const levelNum = Number(item.key)
    if (Number.isFinite(levelNum)) {
      return vendorDict.levelLabel(levelNum)
    }
  }
  if (groupKey === 'vendorIndustry' && item.key !== '_unset') {
    return vendorDict.industryLabel(item.key)
  }
  if (groupKey === 'orderStatus') {
    const statusNum = Number(item.key)
    if (Number.isFinite(statusNum)) {
      const statusKey = `purchaseOrderList.status.${statusLabelKey(statusNum)}`
      const label = t(statusKey)
      return label !== statusKey ? label : item.label
    }
  }
  return item.label
}

function statusLabelKey(status: number): string {
  const map: Record<number, string> = {
    0: 'draft',
    1: 'new',
    2: 'pendingReview',
    10: 'approved',
    20: 'pendingConfirm',
    30: 'confirmed',
    50: 'inProgress',
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

function buildQuery(): PurchaseOrderListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    await vendorDict.ensureLoaded()
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      purchaseOrderListAnalyticsApi.getDashboard(q),
      purchaseOrderListAnalyticsApi.getTrends(q),
      purchaseOrderListAnalyticsApi.getBreakdowns(q),
      purchaseOrderListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('purchaseOrderList.board.loadFailed')
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
  <div class="po-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <span class="board-hint">{{ t('purchaseOrderList.board.hint') }}</span>
      <el-select v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="t('purchaseOrderList.board.groupBy.day')" />
        <el-option value="week" :label="t('purchaseOrderList.board.groupBy.week')" />
        <el-option value="month" :label="t('purchaseOrderList.board.groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData">{{ t('purchaseOrderList.board.refresh') }}</el-button>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('purchaseOrderList.board.sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('purchaseOrderList.board.sections.trendOrders') }}</h3>
        <AnalyticsTrendChart
          :points="trendOrderPoints"
          :value-suffix="t('purchaseOrderList.board.trendUnit.orders')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('purchaseOrderList.board.sections.trendAmount') }}</h3>
        <AnalyticsTrendChart
          :points="trendAmountPoints"
          value-format="money"
          :unit-caption="t('purchaseOrderList.board.trendUnit.moneyCaption')"
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
          :unit-caption="
            breakdownValueFormat(group.groupKey) === 'money'
              ? t('purchaseOrderList.board.trendUnit.moneyCaption')
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
              ? t('purchaseOrderList.board.trendUnit.moneyCaption')
              : undefined
          "
        />
      </div>
    </div>

    <div class="rankings-row">
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('purchaseOrderList.board.rankings.vendorByAmount') }}</h3>
        <el-table :data="rankings?.vendorByAmount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('purchaseOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('purchaseOrderList.board.rankings.orderCount')" width="90" />
          <el-table-column :label="t('purchaseOrderList.board.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('purchaseOrderList.board.rankings.vendorByOrderCount') }}</h3>
        <el-table :data="rankings?.vendorByOrderCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('purchaseOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('purchaseOrderList.board.rankings.orderCount')" width="90" />
          <el-table-column :label="t('purchaseOrderList.board.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('purchaseOrderList.board.rankings.vendorByRepeat') }}</h3>
        <el-table :data="rankings?.vendorByRepeatOrderCount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('purchaseOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('purchaseOrderList.board.rankings.repeatOrders')" width="100" />
          <el-table-column :label="t('purchaseOrderList.board.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <h3 class="section-title">{{ t('purchaseOrderList.board.rankings.purchaseUserByAmount') }}</h3>
        <el-table :data="rankings?.purchaseUserByAmount ?? []" size="small" stripe>
          <el-table-column prop="name" :label="t('purchaseOrderList.board.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('purchaseOrderList.board.rankings.orderCount')" width="90" />
          <el-table-column :label="t('purchaseOrderList.board.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.po-list-board {
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
