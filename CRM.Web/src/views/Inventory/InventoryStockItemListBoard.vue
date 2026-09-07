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
  inventoryStockItemListAnalyticsApi,
  type InventoryStockItemListAnalyticsDashboard,
  type InventoryStockItemListAnalyticsQuery,
  type InventoryStockItemRankingDrillPayload
} from '@/api/inventoryStockItemAnalytics'
import type {
  InventoryOnHandListAnalyticsBreakdownGroup,
  InventoryOnHandListAnalyticsRankingFacet,
  InventoryOnHandListAnalyticsRankingRow,
  InventoryOnHandListAnalyticsRankings,
  InventoryOnHandListAnalyticsTrendPoint
} from '@/api/inventoryOnHandAnalytics'
import { listAmountCurrencyDockClass } from '@/utils/moneyFormat'
import {
  isInventoryBoardConvertedUsd,
  appendInventoryBoardConvertedUsdOption
} from '@/utils/inventoryBoardConvertedUsd'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{
  filters: InventoryStockItemListAnalyticsQuery
}>()

const emit = defineEmits<{
  drillStagnant: []
  drillRanking: [payload: InventoryStockItemRankingDrillPayload]
}>()

const { t } = useI18n()
const authStore = useAuthStore()
const { def } = useAnalyticsDefinition('inventoryStockItemList.board')

const loading = ref(false)
const groupBy = ref<'day' | 'week' | 'month'>('day')
const dashboard = ref<InventoryStockItemListAnalyticsDashboard | null>(null)
const trends = ref<InventoryOnHandListAnalyticsTrendPoint[]>([])
const breakdowns = ref<InventoryOnHandListAnalyticsBreakdownGroup[]>([])
const rankings = ref<InventoryOnHandListAnalyticsRankings | null>(null)
type BoardMetricMode = 'qty' | 'amount' | 'layers'

const breakdownMetricMode = ref<BoardMetricMode>('qty')
const rankingMetricMode = ref<BoardMetricMode>('qty')
const amountCurrencyKey = ref('1')

const maskAmounts = computed(() => dashboard.value?.context.maskAmounts === true)
const effectiveBreakdownMetricMode = computed<BoardMetricMode>(() =>
  maskAmounts.value && breakdownMetricMode.value === 'amount' ? 'qty' : breakdownMetricMode.value
)
const effectiveRankingMetricMode = computed<BoardMetricMode>(() =>
  maskAmounts.value && rankingMetricMode.value === 'amount' ? 'qty' : rankingMetricMode.value
)

const currencyOptions = computed(() => {
  if (maskAmounts.value || !dashboard.value) return []
  const lines = (dashboard.value.snapshot.currencyLines ?? []).map((line) => ({
    key: line.currencyKey,
    label: line.currencyLabel
  }))
  return appendInventoryBoardConvertedUsdOption(lines, tt('currency.convertedUsd'))
})

watch(
  currencyOptions,
  (opts) => {
    if (opts.length === 0) return
    if (opts.some((o) => o.key === amountCurrencyKey.value)) return
    amountCurrencyKey.value =
      opts.find((o) => !isInventoryBoardConvertedUsd(o.key))?.key ?? opts[0]!.key
  },
  { immediate: true }
)

function tt(path: string, values?: Record<string, unknown>): string {
  return values
    ? t(`inventoryStockItemList.board.${path}`, values)
    : t(`inventoryStockItemList.board.${path}`)
}

function formatAmountNumber(amount: number | null | undefined): string {
  if (amount == null) return '—'
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatOriginalMoney(amount: number | null | undefined, currencyLabel: string): string {
  if (amount == null) return '—'
  return `${formatAmountNumber(amount)} ${currencyLabel}`
}

function formatTurnover(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(1)} ${tt('unit.days')}`
}

function formatQty(v?: number | null): string {
  if (v == null) return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

function onKpiClick(key: string) {
  if (key !== 'stagnantQty') return
  if (!authStore.hasPermission('inventory.read')) {
    ElMessage.warning(tt('drill.noPermission'))
    return
  }
  emit('drillStagnant')
}

function onRankingRowClick(
  panel: RankingPanelConfig,
  row: InventoryOnHandListAnalyticsRankingRow
) {
  if (!authStore.hasPermission('inventory.read')) {
    ElMessage.warning(tt('drill.noPermission'))
    return
  }
  emit('drillRanking', {
    dimension: panel.key as InventoryStockItemRankingDrillPayload['dimension'],
    row,
    metricMode: effectiveRankingMetricMode.value,
    currencyKey: effectiveRankingMetricMode.value === 'amount' ? amountCurrencyKey.value : undefined,
    panelTitle: rankingTitle(panel)
  })
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
        currency: Number(line.currencyKey) || undefined,
        usdText: ''
      }))

  return [
    {
      key: 'onHandQty',
      label: tt('kpi.onHandQty'),
      value: formatQty(s.onHandQty),
      ...def('kpi.onHandQty')
    },
    {
      key: 'amount',
      label: tt('kpi.amount'),
      value: maskAmounts.value ? '—' : tt('kpi.originalCaption'),
      valueFormat: 'text' as const,
      layout: 'split' as const,
      currencyCaption: currencyItems.length ? tt('kpi.originalCaption') : undefined,
      currencyItems: currencyItems.length ? currencyItems : undefined,
      ...def('kpi.amount')
    },
    {
      key: 'turnoverDays',
      label: tt('kpi.turnoverDays'),
      value: formatTurnover(s.turnoverDays),
      ...def('kpi.turnoverDays')
    },
    {
      key: 'stagnantQty',
      label: tt('kpi.stagnantQty'),
      value: formatQty(s.stagnantQty),
      drillable: true,
      ...def('kpi.stagnantQty')
    }
  ]
})

const trendQtyPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.onHandQty }))
)

const trendAmountSeries = computed(() => {
  const labels = new Map<string, string>()
  for (const p of trends.value) {
    for (const row of p.amountsByCurrency ?? []) {
      if (!labels.has(row.currencyKey)) labels.set(row.currencyKey, row.currencyLabel)
    }
  }
  return [...labels.entries()].map(([currencyKey, currencyLabel]) => ({
    currencyKey,
    currencyLabel,
    points: trends.value.map((p) => {
      const row = (p.amountsByCurrency ?? []).find((x) => x.currencyKey === currencyKey)
      return { period: p.period, value: maskAmounts.value ? 0 : (row?.amount ?? 0) }
    })
  }))
})

type BreakdownPanelKey = 'stockType' | 'warehouse' | 'salesUser' | 'ageBucket'

const breakdownPanels: { key: BreakdownPanelKey; titleKey: string }[] = [
  { key: 'stockType', titleKey: 'stockType' },
  { key: 'warehouse', titleKey: 'warehouse' },
  { key: 'salesUser', titleKey: 'salesUser' },
  { key: 'ageBucket', titleKey: 'ageBucket' }
]

function breakdownGroupFor(panel: BreakdownPanelKey): InventoryOnHandListAnalyticsBreakdownGroup | null {
  const mode = effectiveBreakdownMetricMode.value
  if (mode === 'layers') {
    return breakdowns.value.find((g) => g.groupKey === panel && g.metric === 'layers') ?? null
  }
  if (mode === 'qty') {
    return (
      breakdowns.value.find(
        (g) => g.groupKey === panel && !g.currencyKey && (g.metric === 'qty' || !g.metric)
      ) ?? null
    )
  }
  return (
    breakdowns.value.find(
      (g) => g.groupKey === panel && g.currencyKey === amountCurrencyKey.value
    ) ?? null
  )
}

function breakdownTitle(panel: BreakdownPanelKey): string {
  const base = tt(`breakdown.${panel}`)
  if (effectiveBreakdownMetricMode.value !== 'amount') return base
  const label =
    currencyOptions.value.find((o) => o.key === amountCurrencyKey.value)?.label ?? ''
  return `${base} · ${label}`
}

function breakdownItemLabel(
  groupKey: string,
  item: { key: string; label: string }
): string {
  if (groupKey === 'stockType') return item.label
  if (groupKey === 'ageBucket') return item.label
  if (item.key === '_unset') {
    if (groupKey === 'salesUser') return tt('unsetSalesUser')
    if (groupKey === 'warehouse') return tt('unsetWarehouse')
  }
  return item.label
}

function localizedBreakdownItems(group: InventoryOnHandListAnalyticsBreakdownGroup) {
  return group.items.map((item) => ({
    ...item,
    label: breakdownItemLabel(group.groupKey, item)
  }))
}

function breakdownValueFormat(): 'money' | 'number' | 'originalCurrency' | 'amount' {
  if (effectiveBreakdownMetricMode.value === 'amount') return 'amount'
  return 'number'
}

function breakdownUnitCaption(_panel: BreakdownPanelKey): string | undefined {
  if (effectiveBreakdownMetricMode.value === 'layers') return tt('trendUnit.layers')
  if (effectiveBreakdownMetricMode.value === 'qty') return tt('trendUnit.qty')
  if (isInventoryBoardConvertedUsd(amountCurrencyKey.value)) return tt('trendUnit.convertedUsdCaption')
  const label =
    currencyOptions.value.find((o) => o.key === amountCurrencyKey.value)?.label ?? ''
  return tt('trendUnit.originalCaption', { currency: label })
}

interface RankingPanelConfig {
  key: string
  qtyKey: keyof InventoryOnHandListAnalyticsRankings
  layerKey: keyof InventoryOnHandListAnalyticsRankings
  amountKey: keyof InventoryOnHandListAnalyticsRankings
  titleQty: string
  titleLayer: string
  titleAmount: string
  defQty: string
  defLayer: string
  defAmount: string
}

const rankingPanels: RankingPanelConfig[] = [
  {
    key: 'customer',
    qtyKey: 'customerByQty',
    layerKey: 'customerByLayer',
    amountKey: 'customerByAmount',
    titleQty: 'customerByQty',
    titleLayer: 'customerByLayer',
    titleAmount: 'customerByAmount',
    defQty: 'rankings.customerByQty',
    defLayer: 'rankings.customerByLayer',
    defAmount: 'rankings.customerByAmount'
  },
  {
    key: 'salesUser',
    qtyKey: 'salesUserByQty',
    layerKey: 'salesUserByLayer',
    amountKey: 'salesUserByAmount',
    titleQty: 'salesUserByQty',
    titleLayer: 'salesUserByLayer',
    titleAmount: 'salesUserByAmount',
    defQty: 'rankings.salesUserByQty',
    defLayer: 'rankings.salesUserByLayer',
    defAmount: 'rankings.salesUserByAmount'
  },
  {
    key: 'material',
    qtyKey: 'materialByQty',
    layerKey: 'materialByLayer',
    amountKey: 'materialByAmount',
    titleQty: 'materialByQty',
    titleLayer: 'materialByLayer',
    titleAmount: 'materialByAmount',
    defQty: 'rankings.materialByQty',
    defLayer: 'rankings.materialByLayer',
    defAmount: 'rankings.materialByAmount'
  },
  {
    key: 'brand',
    qtyKey: 'brandByQty',
    layerKey: 'brandByLayer',
    amountKey: 'brandByAmount',
    titleQty: 'brandByQty',
    titleLayer: 'brandByLayer',
    titleAmount: 'brandByAmount',
    defQty: 'rankings.brandByQty',
    defLayer: 'rankings.brandByLayer',
    defAmount: 'rankings.brandByAmount'
  }
]

function rankingRowsFor(panel: RankingPanelConfig): InventoryOnHandListAnalyticsRankingRow[] {
  if (!rankings.value) return []
  if (effectiveRankingMetricMode.value === 'qty') {
    const data = rankings.value[panel.qtyKey] as InventoryOnHandListAnalyticsRankingRow[] | undefined
    return Array.isArray(data) ? data : []
  }
  if (effectiveRankingMetricMode.value === 'layers') {
    const data = rankings.value[panel.layerKey] as InventoryOnHandListAnalyticsRankingRow[] | undefined
    return Array.isArray(data) ? data : []
  }
  const facets = rankings.value[panel.amountKey] as InventoryOnHandListAnalyticsRankingFacet[]
  const facet = facets?.find((f) => f.currencyKey === amountCurrencyKey.value)
  return facet?.rows ?? []
}

function rankingTitle(panel: RankingPanelConfig): string {
  const mode = effectiveRankingMetricMode.value
  const titleKey =
    mode === 'qty' ? panel.titleQty : mode === 'layers' ? panel.titleLayer : panel.titleAmount
  const base = tt(`rankings.${titleKey}`)
  if (mode !== 'amount') return base
  const label =
    currencyOptions.value.find((o) => o.key === amountCurrencyKey.value)?.label ?? ''
  return `${base} · ${label}`
}

function rankingDef(panel: RankingPanelConfig) {
  const mode = effectiveRankingMetricMode.value
  if (mode === 'qty') return def(panel.defQty)
  if (mode === 'layers') return def(panel.defLayer)
  return def(panel.defAmount)
}

function rankingMetricHeaderLabel(): string {
  if (effectiveRankingMetricMode.value === 'amount') return tt('rankings.amount')
  if (effectiveRankingMetricMode.value === 'layers') return tt('rankings.layers')
  return tt('rankings.qty')
}

function formatRankingMetric(row: InventoryOnHandListAnalyticsRankingRow): string {
  return String(row.orderCount ?? 0)
}

function rankingAmountCurrencyLabel(): string {
  return currencyOptions.value.find((o) => o.key === amountCurrencyKey.value)?.label ?? ''
}

function rankingAmountDockClass(): string {
  if (isInventoryBoardConvertedUsd(amountCurrencyKey.value)) return listAmountCurrencyDockClass(2)
  return listAmountCurrencyDockClass(Number(amountCurrencyKey.value))
}

function buildQuery(): InventoryStockItemListAnalyticsQuery {
  return { ...props.filters, groupBy: groupBy.value }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows, rankingRows] = await Promise.all([
      inventoryStockItemListAnalyticsApi.getDashboard(q),
      inventoryStockItemListAnalyticsApi.getTrends(q),
      inventoryStockItemListAnalyticsApi.getBreakdowns(q),
      inventoryStockItemListAnalyticsApi.getRankings(q)
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
  <div class="isi-list-board" v-loading="loading">
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
      <AnalyticsKpiGrid :items="kpiItems" @item-click="onKpiClick" />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <AnalyticsPanelHeader
          :title="tt('sections.trendQty')"
          v-bind="def('trend.qty')"
        />
        <AnalyticsTrendChart :points="trendQtyPoints" :value-suffix="tt('trendUnit.qty')" />
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
          :value-format="trendValueFormat(series.currencyKey)"
        />
      </div>
    </div>

    <div class="breakdown-section">
      <div class="rankings-toolbar">
        <span class="rankings-toolbar-label">{{ tt('breakdown.metricMode') }}</span>
        <el-radio-group v-model="breakdownMetricMode" size="small">
          <el-radio-button v-if="!maskAmounts" value="amount">{{ tt('breakdown.amount') }}</el-radio-button>
          <el-radio-button value="layers">{{ tt('breakdown.layers') }}</el-radio-button>
          <el-radio-button value="qty">{{ tt('breakdown.qty') }}</el-radio-button>
        </el-radio-group>
        <el-select
          v-if="effectiveBreakdownMetricMode === 'amount' && currencyOptions.length"
          v-model="amountCurrencyKey"
          style="width: 120px"
        >
          <el-option
            v-for="opt in currencyOptions"
            :key="opt.key"
            :label="opt.label"
            :value="opt.key"
          />
        </el-select>
      </div>

      <div class="charts-row breakdown-row">
        <div v-for="panel in breakdownPanels" :key="panel.key" class="card chart-panel">
          <AnalyticsBreakdownPieChart
            v-if="breakdownGroupFor(panel.key)"
            :title="breakdownTitle(panel.key)"
            :items="localizedBreakdownItems(breakdownGroupFor(panel.key)!)"
            :value-format="breakdownValueFormat()"
            :unit-caption="breakdownUnitCaption(panel.key)"
            v-bind="def(`breakdown.${panel.titleKey}`)"
          />
        </div>
      </div>
    </div>

    <div class="rankings-section">
      <div class="rankings-toolbar">
        <span class="rankings-toolbar-label">{{ tt('rankings.metricMode') }}</span>
        <el-radio-group v-model="rankingMetricMode" size="small">
          <el-radio-button v-if="!maskAmounts" value="amount">{{ tt('rankings.amount') }}</el-radio-button>
          <el-radio-button value="layers">{{ tt('rankings.layers') }}</el-radio-button>
          <el-radio-button value="qty">{{ tt('rankings.qty') }}</el-radio-button>
        </el-radio-group>
        <el-select
          v-if="effectiveRankingMetricMode === 'amount' && currencyOptions.length"
          v-model="amountCurrencyKey"
          style="width: 120px"
        >
          <el-option
            v-for="opt in currencyOptions"
            :key="opt.key"
            :label="opt.label"
            :value="opt.key"
          />
        </el-select>
      </div>

      <div class="rankings-row">
        <div v-for="panel in rankingPanels" :key="panel.key" class="card ranking-panel">
          <AnalyticsPanelHeader :title="rankingTitle(panel)" v-bind="rankingDef(panel)" />
          <el-table
            :data="rankingRowsFor(panel)"
            size="small"
            stripe
            class="ranking-table ranking-table--drill"
            @row-click="(row: InventoryOnHandListAnalyticsRankingRow) => onRankingRowClick(panel, row)"
          >
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
            <el-table-column width="188" align="right" class-name="ranking-metric-col">
              <template #header>
                <span>{{ rankingMetricHeaderLabel() }}</span>
              </template>
              <template #default="{ row }">
                <span
                  v-if="effectiveRankingMetricMode === 'amount'"
                  class="dock-tier-price-line ranking-metric-value"
                >
                  <span class="dock-tier-amt-int">{{ formatAmountNumber(row.amount) }}</span>
                  <template v-if="row.amount != null">
                    <span class="dock-tier-ccy-gap">&nbsp;</span>
                    <span :class="['dock-tier-ccy', rankingAmountDockClass()]">{{
                      rankingAmountCurrencyLabel()
                    }}</span>
                  </template>
                </span>
                <span v-else class="ranking-metric-value">{{ formatRankingMetric(row) }}</span>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.isi-list-board {
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

.breakdown-section,
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

.ranking-table--drill :deep(.el-table__row) {
  cursor: pointer;
}

.ranking-table--drill :deep(.el-table__row:hover) {
  background: var(--el-fill-color-light);
}

.ranking-name-cell {
  word-break: break-word;
}

.ranking-metric-value {
  font-variant-numeric: tabular-nums;
}
</style>
