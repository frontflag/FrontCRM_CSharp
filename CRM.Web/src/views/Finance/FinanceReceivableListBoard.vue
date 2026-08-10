<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  financeReceivableListAnalyticsApi,
  type FinanceReceivableListAnalyticsBreakdownGroup,
  type FinanceReceivableListAnalyticsDashboard,
  type FinanceReceivableListAnalyticsQuery,
  type FinanceReceivableListAnalyticsRankingRow,
  type FinanceReceivableListAnalyticsRankings,
  type FinanceReceivableListAnalyticsTrendPoint
} from '@/api/financeReceivableAnalytics'

const props = defineProps<{
  filters: FinanceReceivableListAnalyticsQuery
}>()

const { t } = useI18n()

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('month')
/** 分布/排名金额模式：待核销 | 总额（账期饼始终待核销） */
const amountMode = ref<'pending' | 'total'>('pending')
const dashboard = ref<FinanceReceivableListAnalyticsDashboard | null>(null)
const trends = ref<FinanceReceivableListAnalyticsTrendPoint[]>([])
const breakdowns = ref<FinanceReceivableListAnalyticsBreakdownGroup[]>([])
const rankings = ref<FinanceReceivableListAnalyticsRankings | null>(null)

interface RankingTableConfig {
  key: string
  titleKey: string
  dataKey: keyof FinanceReceivableListAnalyticsRankings
  /** 单笔榜固定总额 */
  forceTotal?: boolean
  showStatus?: boolean
}

const rankingTables: RankingTableConfig[] = [
  {
    key: 'receivable',
    titleKey: 'receivableByTotalAmount',
    dataKey: 'receivableByTotalAmount',
    forceTotal: true,
    showStatus: true
  },
  { key: 'customer', titleKey: 'customerByAmount', dataKey: 'customerByAmount' },
  { key: 'salesUser', titleKey: 'salesUserByAmount', dataKey: 'salesUserByAmount' }
]

function formatMoney(v?: number | null): string {
  if (v == null) return '—'
  return `$\u00a0${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function formatAmountNumber(amount: number | null | undefined): string {
  if (amount == null) return '—'
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatOriginalMoney(amount: number | null | undefined, currencyLabel: string): string {
  if (amount == null) return '—'
  return `${formatAmountNumber(amount)} ${currencyLabel}`
}

function formatDays(v?: number | null): string {
  if (v == null || v === undefined) return '—'
  return `${v}`
}

function verificationLabel(status?: number | null): string {
  if (status === 2) return t('financeReceivableList.verification.complete')
  if (status === 1) return t('financeReceivableList.verification.partial')
  if (status === 0) return t('financeReceivableList.verification.pending')
  return '—'
}

const kpiItems = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  const pendingCurrency = (s.pendingCurrencyLines ?? []).map((line) => ({
    currencyLabel: line.currencyLabel,
    originalText: formatOriginalMoney(line.originalAmount, line.currencyLabel),
    amountText: formatAmountNumber(line.originalAmount),
    currency: Number(line.currencyKey),
    usdText: ''
  }))
  const totalCurrency = (s.totalCurrencyLines ?? []).map((line) => ({
    currencyLabel: line.currencyLabel,
    originalText: formatOriginalMoney(line.originalAmount, line.currencyLabel),
    amountText: formatAmountNumber(line.originalAmount),
    currency: Number(line.currencyKey),
    usdText: ''
  }))

  return [
    {
      key: 'customers',
      label: t('financeReceivableList.board.kpi.customers'),
      value: String(s.customerCount)
    },
    {
      key: 'lines',
      label: t('financeReceivableList.board.kpi.lines'),
      value: String(s.lineCount)
    },
    {
      key: 'totalAmount',
      label: t('financeReceivableList.board.kpi.totalAmount'),
      value: formatMoney(s.totalAmountUsd),
      valueFormat: 'money' as const,
      layout: 'split' as const,
      gridColumnSpan: 2,
      valueCaption: t('financeReceivableList.board.kpi.usdCaption'),
      currencyCaption: totalCurrency.length
        ? t('financeReceivableList.board.kpi.originalCaption')
        : undefined,
      currencyItems: totalCurrency.length ? totalCurrency : undefined,
      showDefinition: true,
      definitionLabel: t('salesAnalytics.definitionTip.button'),
      definitionChart: t('financeReceivableList.board.stockOutReceivableDefinition.chart'),
      definitionDataSource: t('financeReceivableList.board.stockOutReceivableDefinition.dataSource'),
      definitionText: t('financeReceivableList.board.stockOutReceivableDefinition.text')
    },
    {
      key: 'pendingAmount',
      label: t('financeReceivableList.board.kpi.pendingAmount'),
      value: formatMoney(s.pendingAmountUsd),
      valueFormat: 'money' as const,
      layout: 'split' as const,
      gridColumnSpan: 2,
      valueCaption: t('financeReceivableList.board.kpi.usdCaption'),
      currencyCaption: pendingCurrency.length
        ? t('financeReceivableList.board.kpi.originalCaption')
        : undefined,
      currencyItems: pendingCurrency.length ? pendingCurrency : undefined,
      showDefinition: true,
      definitionLabel: t('salesAnalytics.definitionTip.button'),
      definitionChart: t('financeReceivableList.board.pendingReceivableDefinition.chart'),
      definitionDataSource: t('financeReceivableList.board.pendingReceivableDefinition.dataSource'),
      definitionText: t('financeReceivableList.board.pendingReceivableDefinition.text')
    },
    {
      key: 'maxAge',
      label: t('financeReceivableList.board.kpi.maxAge'),
      value: formatDays(s.maxReceivableAgeDays)
    }
  ]
})

const trendCustomerPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.customerCount }))
)
const trendLinePoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.lineCount }))
)
const trendPendingAmountPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: Number(p.pendingAmountUsd ?? 0) }))
)
const trendTotalAmountPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: Number(p.totalAmountUsd ?? 0) }))
)

function mapBreakdownItems(group: FinanceReceivableListAnalyticsBreakdownGroup) {
  const usePending = amountMode.value === 'pending'
  return group.items.map((item) => ({
    key: item.key,
    label: breakdownItemLabel(group.groupKey, item),
    value: usePending ? item.pendingValue : item.totalValue,
    ratio: usePending ? item.pendingRatio : item.totalRatio
  }))
}

function breakdownItemLabel(groupKey: string, item: { key: string; label: string }): string {
  if (groupKey === 'verificationStatus') {
    const n = Number(item.key)
    if (n === 0) return t('financeReceivableList.verification.pending')
    if (n === 1) return t('financeReceivableList.verification.partial')
    if (n === 2) return t('financeReceivableList.verification.complete')
  }
  if (groupKey === 'aging') {
    const agingKey = `financeReceivableList.board.aging.${item.key}`
    const translated = t(agingKey)
    if (translated !== agingKey) return translated
  }
  return item.label
}

function breakdownTitle(group: FinanceReceivableListAnalyticsBreakdownGroup): string {
  const key = `financeReceivableList.board.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  const modeLabel =
    amountMode.value === 'pending'
      ? t('financeReceivableList.board.amountMode.pending')
      : t('financeReceivableList.board.amountMode.total')
  return `${base}（${modeLabel}）`
}

function rankingRowsFor(config: RankingTableConfig): FinanceReceivableListAnalyticsRankingRow[] {
  const data = rankings.value?.[config.dataKey]
  return Array.isArray(data) ? data : []
}

function rankingAmount(row: FinanceReceivableListAnalyticsRankingRow, forceTotal?: boolean): string {
  const useTotal = forceTotal || amountMode.value === 'total'
  return formatMoney(useTotal ? row.totalAmountUsd : row.pendingAmountUsd)
}

function buildQuery(): FinanceReceivableListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      financeReceivableListAnalyticsApi.getDashboard(q),
      financeReceivableListAnalyticsApi.getTrends(q),
      financeReceivableListAnalyticsApi.getBreakdowns(q),
      financeReceivableListAnalyticsApi.getRankings(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows
    rankings.value = rankingRows
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('financeReceivableList.board.loadFailed')
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
  <div class="receivable-list-board" v-loading="loading">
    <div class="board-toolbar card">
      <span class="board-hint">{{ t('financeReceivableList.board.hint') }}</span>
      <el-radio-group v-model="amountMode" size="small">
        <el-radio-button value="pending">{{ t('financeReceivableList.board.amountMode.pending') }}</el-radio-button>
        <el-radio-button value="total">{{ t('financeReceivableList.board.amountMode.total') }}</el-radio-button>
      </el-radio-group>
      <el-select v-model="groupBy" style="width: 120px">
        <el-option value="day" :label="t('financeReceivableList.board.groupBy.day')" />
        <el-option value="week" :label="t('financeReceivableList.board.groupBy.week')" />
        <el-option value="month" :label="t('financeReceivableList.board.groupBy.month')" />
      </el-select>
      <el-button type="primary" @click="loadData">{{ t('financeReceivableList.board.refresh') }}</el-button>
    </div>

    <p v-if="dashboard?.context.exchangeRateHint" class="fx-hint">
      {{ dashboard.context.exchangeRateHint }}
    </p>

    <section class="section">
      <h3 class="section-title">{{ t('financeReceivableList.board.sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('financeReceivableList.board.sections.trendCustomers') }}</h3>
        <AnalyticsTrendChart
          :points="trendCustomerPoints"
          :value-suffix="t('financeReceivableList.board.trendUnit.customers')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('financeReceivableList.board.sections.trendLines') }}</h3>
        <AnalyticsTrendChart
          :points="trendLinePoints"
          :value-suffix="t('financeReceivableList.board.trendUnit.lines')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('financeReceivableList.board.sections.trendPendingAmount') }}</h3>
        <AnalyticsTrendChart
          :points="trendPendingAmountPoints"
          value-format="money"
          :unit-caption="t('financeReceivableList.board.trendUnit.moneyCaption')"
        />
      </div>
      <div class="card chart-panel">
        <h3 class="section-title">{{ t('financeReceivableList.board.sections.trendTotalAmount') }}</h3>
        <AnalyticsTrendChart
          :points="trendTotalAmountPoints"
          value-format="money"
          :unit-caption="t('financeReceivableList.board.trendUnit.moneyCaption')"
        />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div v-for="group in breakdowns" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          :title="breakdownTitle(group)"
          :items="mapBreakdownItems(group)"
          value-format="money"
          :unit-caption="t('financeReceivableList.board.trendUnit.moneyCaption')"
        />
      </div>
    </div>

    <div class="rankings-section">
      <div class="rankings-toolbar">
        <span class="rankings-toolbar-label">{{ t('financeReceivableList.board.rankings.amountMode') }}</span>
        <el-radio-group v-model="amountMode" size="small">
          <el-radio-button value="pending">{{ t('financeReceivableList.board.amountMode.pending') }}</el-radio-button>
          <el-radio-button value="total">{{ t('financeReceivableList.board.amountMode.total') }}</el-radio-button>
        </el-radio-group>
      </div>

      <div class="rankings-row">
        <div v-for="table in rankingTables" :key="table.key" class="card ranking-panel">
          <h3 class="section-title">{{ t(`financeReceivableList.board.rankings.${table.titleKey}`) }}</h3>
          <el-table :data="rankingRowsFor(table)" size="small" stripe class="ranking-table">
            <el-table-column
              prop="name"
              :label="t('financeReceivableList.board.rankings.name')"
              min-width="160"
            />
            <el-table-column
              v-if="table.showStatus"
              :label="t('financeReceivableList.columns.verificationStatus')"
              width="100"
              align="center"
            >
              <template #default="{ row }">
                {{ verificationLabel(row.verificationStatus) }}
              </template>
            </el-table-column>
            <el-table-column
              :label="t('financeReceivableList.board.rankings.amount')"
              width="160"
              align="right"
            >
              <template #default="{ row }">
                {{ rankingAmount(row, table.forceTotal) }}
              </template>
            </el-table-column>
            <el-table-column
              :label="t('financeReceivableList.board.rankings.lineCount')"
              width="88"
              align="right"
              prop="orderCount"
            />
          </el-table>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.receivable-list-board {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 12px 16px;
}

.board-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.board-hint {
  flex: 1;
  min-width: 200px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.fx-hint {
  margin: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
}

.charts-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 12px;
}

.breakdown-row {
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
}

.chart-panel {
  min-height: 220px;
}

.rankings-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.rankings-toolbar-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.rankings-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 12px;
}

.ranking-table {
  width: 100%;
}
</style>
