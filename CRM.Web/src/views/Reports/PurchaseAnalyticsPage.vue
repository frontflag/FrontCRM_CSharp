<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import AnalyticsScopeTabs from '@/components/Analytics/AnalyticsScopeTabs.vue'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownChart from '@/components/Analytics/AnalyticsBreakdownChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import PurchaseAnalyticsVendorPanel from '@/components/Analytics/PurchaseAnalyticsVendorPanel.vue'
import QuoteListBoard from '@/views/RFQ/QuoteListBoard.vue'
import PurchaseOrderItemListBoard from '@/views/RFQ/PurchaseOrderItemListBoard.vue'
import {
  purchaseAnalyticsApi,
  type PurchaseAnalyticsBreakdownGroup,
  type PurchaseAnalyticsDashboard,
  type PurchaseAnalyticsTrendPoint,
  type PurchaseAnalyticsViewLevel
} from '@/api/analytics/purchase'
import {
  buildPurchaseUserRankingDrillRoute,
  buildSnapshotDrillRoute,
  buildTodoDrillRoute,
  isSnapshotDrillable,
  isTodoDrillable,
  type PurchaseAnalyticsSnapshotDrillKey,
  type PurchaseAnalyticsTodoDrillKey
} from '@/utils/purchaseAnalyticsDrill'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()

const loading = ref(false)
const contentTab = ref<'overview' | 'vendor' | 'quote' | 'order'>('overview')
const viewLevel = ref<PurchaseAnalyticsViewLevel>('company')
const departmentId = ref<string | undefined>()
const purchaseUserId = ref<string | undefined>()
const dateRange = ref<[string, string]>(defaultDateRange())
const groupBy = ref<'day' | 'week' | 'month'>('month')

const dashboard = ref<PurchaseAnalyticsDashboard | null>(null)
const trends = ref<PurchaseAnalyticsTrendPoint[]>([])
const breakdowns = ref<PurchaseAnalyticsBreakdownGroup[]>([])

const scopeContext = computed(() => dashboard.value?.scopeContext)

const maskAmounts = computed(() => scopeContext.value?.maskAmounts === true)

function drillScope() {
  return {
    dateFrom: dateRange.value[0],
    dateTo: dateRange.value[1],
    purchaseUserId: purchaseUserId.value,
    scopeContext: scopeContext.value
  }
}

function defaultDateRange(): [string, string] {
  const end = new Date()
  const start = new Date(end)
  start.setMonth(start.getMonth() - 5)
  return [formatDate(start), formatDate(end)]
}

function formatDate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function formatMoney(v?: number | null): string {
  if (v == null) return '—'
  // 不换行空格，避免 $ 与数字分两行
  return `$\u00a0${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function formatRate(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(2)}%`
}

const todoKpis = computed(() => {
  const todo = dashboard.value?.todo
  if (!todo) return []
  return [
    {
      key: 'payable',
      label: t('purchaseAnalytics.kpi.payableAmount'),
      value: formatMoney(todo.payableAmount),
      valueFormat: 'money' as const,
      tone: 'todo' as const,
      drillable: isTodoDrillable('payable', maskAmounts.value) && authStore.hasPermission('finance-payment.read')
    },
    {
      key: 'pendingStockIn',
      label: t('purchaseAnalytics.kpi.pendingStockInItemCount'),
      value: String(todo.pendingStockInItemCount ?? 0),
      tone: 'todo' as const,
      drillable: isTodoDrillable('pendingStockIn', maskAmounts.value) && authStore.hasPermission('purchase-order.read')
    }
  ]
})

const snapshotKpis = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'quoteItems',
      label: t('purchaseAnalytics.kpi.quoteItemCount'),
      value: String(s.quoteItemCount),
      drillable: isSnapshotDrillable('quoteItems', maskAmounts.value) && authStore.hasPermission('quote.read')
    },
    {
      key: 'quoteVendors',
      label: t('purchaseAnalytics.kpi.quoteVendorCount'),
      value: String(s.quoteVendorCount),
      drillable: isSnapshotDrillable('quoteVendors', maskAmounts.value) && authStore.hasPermission('quote.read')
    },
    {
      key: 'conversion',
      label: t('purchaseAnalytics.kpi.quoteToPurchaseConversionRate'),
      value: formatRate(s.quoteToPurchaseConversionRate)
    },
    {
      key: 'poItems',
      label: t('purchaseAnalytics.kpi.purchaseOrderItemCount'),
      value: String(s.purchaseOrderItemCount),
      drillable: isSnapshotDrillable('poItems', maskAmounts.value) && authStore.hasPermission('purchase-order.read')
    },
    {
      key: 'poVendors',
      label: t('purchaseAnalytics.kpi.purchaseOrderVendorCount'),
      value: String(s.purchaseOrderVendorCount),
      drillable: isSnapshotDrillable('poVendors', maskAmounts.value) && authStore.hasPermission('purchase-order.read')
    },
    {
      key: 'amount',
      label: t('purchaseAnalytics.kpi.purchaseAmountApproved'),
      value: formatMoney(s.purchaseAmountApproved),
      valueFormat: 'money' as const,
      drillable: isSnapshotDrillable('amount', maskAmounts.value) && authStore.hasPermission('purchase-order.read')
    },
    {
      key: 'stockIn',
      label: t('purchaseAnalytics.kpi.purchaseAmountStockIn'),
      value: formatMoney(s.purchaseAmountStockIn),
      valueFormat: 'money' as const,
      drillable: isSnapshotDrillable('stockIn', maskAmounts.value) && authStore.hasPermission('purchase-order.read')
    },
    {
      key: 'paid',
      label: t('purchaseAnalytics.kpi.purchaseAmountPaid'),
      value: formatMoney(s.purchaseAmountPaid),
      valueFormat: 'money' as const,
      drillable: isSnapshotDrillable('paid', maskAmounts.value) && authStore.hasPermission('purchase-order.read')
    }
  ]
})

const trendAmountPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.purchaseAmountApproved ?? 0
  }))
)

const trendItemPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.purchaseOrderItemCount
  }))
)

const trendQuotePoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.quoteItemCount
  }))
)

const trendQuoteVendorPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.quoteVendorCount
  }))
)

const trendPoVendorPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.purchaseOrderVendorCount
  }))
)

const trendConversionPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.quoteToPurchaseConversionRate ?? 0
  }))
)

const trendStockInPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.purchaseAmountStockIn ?? 0
  }))
)

const trendPaidPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.purchaseAmountPaid ?? 0
  }))
)

const trendPayablePoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.payableAmount ?? 0
  }))
)

const primaryRankingTitle = computed(() => {
  const lvl = viewLevel.value
  if (lvl === 'company') return t('purchaseAnalytics.rankings.departmentTop')
  if (lvl === 'department') return t('purchaseAnalytics.rankings.purchaseUserTop')
  return ''
})

/** 方案 A：供应商排行仅在「供应商」Tab；个人层概况无排行 */
const showOverviewRankings = computed(() => viewLevel.value === 'company' || viewLevel.value === 'department')

const analyticsQuery = computed(() => ({
  viewLevel: viewLevel.value,
  departmentId: departmentId.value,
  purchaseUserId: purchaseUserId.value,
  dateFrom: dateRange.value[0],
  dateTo: dateRange.value[1],
  groupBy: groupBy.value
}))

const showDepartmentSelect = computed(() => {
  const ctx = scopeContext.value
  if (!ctx) return false
  return viewLevel.value === 'department' && (ctx.purchaseDataScope === 0 || ctx.purchaseDataScope === 3)
})

const showPurchaseUserSelect = computed(() => {
  if (viewLevel.value !== 'personal') return false
  const ctx = scopeContext.value
  if (!ctx) return false
  if (ctx.canSelectPurchaseUser === true) return true
  if (ctx.canSelectPurchaseUser === false) return false
  const scope = Number(ctx.purchaseDataScope)
  if (scope === 1 || scope === 4) return false
  return (ctx.allowedViewLevels ?? []).some((l) => String(l).toLowerCase() === 'personal')
})

const pieBreakdownKeys = new Set(['pipelineStage', 'currency'])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

function breakdownValueFormat(groupKey: string): 'money' | 'number' {
  if ((groupKey === 'currency' || groupKey === 'orderStatus') && !maskAmounts.value) return 'money'
  return 'number'
}

function breakdownTitle(group: PurchaseAnalyticsBreakdownGroup): string {
  if (group.groupKey === 'currency' && maskAmounts.value) {
    return `${group.groupLabel}（${t('purchaseAnalytics.breakdown.currencyByCount')}）`
  }
  return group.groupLabel
}

function buildQuery() {
  return {
    viewLevel: viewLevel.value,
    departmentId: departmentId.value,
    purchaseUserId: purchaseUserId.value,
    dateFrom: dateRange.value[0],
    dateTo: dateRange.value[1],
    groupBy: groupBy.value
  }
}

async function loadData() {
  loading.value = true
  try {
    const q = buildQuery()
    const [dash, trendRows, breakdownRows] = await Promise.all([
      purchaseAnalyticsApi.getDashboard(q),
      purchaseAnalyticsApi.getTrends(q),
      purchaseAnalyticsApi.getBreakdowns(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows

    // 后端可能将非法 company 回落到默认层；同步 Tab，避免 UI 仍显示「公司」
    const resolvedLevel = dash.scopeContext.viewLevel as PurchaseAnalyticsViewLevel | undefined
    if (resolvedLevel && resolvedLevel !== viewLevel.value) {
      viewLevel.value = resolvedLevel
    } else if (
      dash.scopeContext.allowedViewLevels.length &&
      !dash.scopeContext.allowedViewLevels.some(
        (l) => String(l).toLowerCase() === String(viewLevel.value).toLowerCase()
      )
    ) {
      viewLevel.value = dash.scopeContext.viewLevel as PurchaseAnalyticsViewLevel
    }
    if (!departmentId.value && dash.scopeContext.resolvedDepartmentId) {
      departmentId.value = dash.scopeContext.resolvedDepartmentId ?? undefined
    }
    if (!purchaseUserId.value && dash.scopeContext.resolvedPurchaseUserId) {
      purchaseUserId.value = dash.scopeContext.resolvedPurchaseUserId ?? undefined
    }
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('purchaseAnalytics.loadFailed')
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
}

function onRankingRowClick(row: { id: string; name: string }) {
  if (viewLevel.value === 'company') {
    viewLevel.value = 'department'
    departmentId.value = row.id
    return
  }
  if (viewLevel.value === 'department') {
    viewLevel.value = 'personal'
    purchaseUserId.value = row.id
  }
}

function onRankingRowDblClick(row: { id: string; name: string }) {
  if (!authStore.hasPermission('purchase-order.read')) {
    ElMessage.warning(t('purchaseAnalytics.drill.noPermission'))
    return
  }
  if (viewLevel.value === 'department') {
    void router.push(buildPurchaseUserRankingDrillRoute(row.id, row.name, drillScope()))
  }
}

function onTodoKpiClick(key: string) {
  const route = buildTodoDrillRoute(key as PurchaseAnalyticsTodoDrillKey, drillScope())
  if (!route) return
  void router.push(route)
}

function onSnapshotKpiClick(key: string) {
  if (key === 'conversion') return
  const route = buildSnapshotDrillRoute(key as PurchaseAnalyticsSnapshotDrillKey, drillScope())
  if (!route) return
  void router.push(route)
}

watch([viewLevel, departmentId, purchaseUserId, dateRange, groupBy], () => void loadData(), { immediate: true })
</script>

<template>
  <div class="purchase-analytics-page" v-loading="loading">
    <div class="page-header">
      <h2 class="page-title">{{ t('purchaseAnalytics.title') }}</h2>
      <p class="page-subtitle">{{ t('purchaseAnalytics.subtitle') }}</p>
    </div>

    <div class="toolbar card">
      <AnalyticsScopeTabs
        v-if="scopeContext"
        v-model="viewLevel"
        :allowed-levels="scopeContext.allowedViewLevels"
        :data-scope="scopeContext.purchaseDataScope"
        i18n-prefix="purchaseAnalytics"
      />
      <div class="toolbar-filters">
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          :start-placeholder="t('purchaseAnalytics.dateFrom')"
          :end-placeholder="t('purchaseAnalytics.dateTo')"
        />
        <el-select v-if="showDepartmentSelect" v-model="departmentId" clearable style="width: 200px">
          <el-option
            v-for="d in scopeContext?.allowedDepartments ?? []"
            :key="d.id"
            :label="d.name"
            :value="d.id"
          />
        </el-select>
        <el-select
          v-if="showPurchaseUserSelect"
          v-model="purchaseUserId"
          clearable
          filterable
          style="width: 200px"
          :placeholder="t('purchaseAnalytics.selectPurchaseUser')"
        >
          <el-option
            v-for="u in scopeContext?.allowedPurchaseUsers ?? []"
            :key="u.id"
            :label="u.name"
            :value="u.id"
          />
        </el-select>
        <el-select v-model="groupBy" style="width: 120px">
          <el-option value="day" :label="t('purchaseAnalytics.groupBy.day')" />
          <el-option value="week" :label="t('purchaseAnalytics.groupBy.week')" />
          <el-option value="month" :label="t('purchaseAnalytics.groupBy.month')" />
        </el-select>
        <el-button type="primary" @click="loadData">{{ t('purchaseAnalytics.refresh') }}</el-button>
      </div>
    </div>

    <div v-if="scopeContext" class="scope-banner card">
      <span>{{ t('purchaseAnalytics.scopeBanner', { label: scopeContext.scopeLabel }) }}</span>
      <span class="muted">{{ t('purchaseAnalytics.metricHint') }}</span>
      <span class="muted">{{ t('purchaseAnalytics.drill.hint') }}</span>
    </div>

    <el-tabs v-model="contentTab" class="content-tabs card">
      <el-tab-pane :label="t('purchaseAnalytics.contentTabs.overview')" name="overview">
        <section class="section">
          <h3 class="section-title">{{ t('purchaseAnalytics.sections.todo') }}</h3>
          <AnalyticsKpiGrid :items="todoKpis" @item-click="onTodoKpiClick" />
        </section>

        <section class="section">
          <h3 class="section-title">{{ t('purchaseAnalytics.sections.snapshot') }}</h3>
          <AnalyticsKpiGrid :items="snapshotKpis" @item-click="onSnapshotKpiClick" />
        </section>

        <div class="charts-row">
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendAmount') }}</h3>
            <AnalyticsTrendChart
              :points="trendAmountPoints"
              value-format="money"
              :unit-caption="t('purchaseAnalytics.trendUnit.moneyCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendStockIn') }}</h3>
            <AnalyticsTrendChart
              :points="trendStockInPoints"
              value-format="money"
              :unit-caption="t('purchaseAnalytics.trendUnit.moneyCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendPaid') }}</h3>
            <AnalyticsTrendChart
              :points="trendPaidPoints"
              value-format="money"
              :unit-caption="t('purchaseAnalytics.trendUnit.moneyCaption')"
            />
          </div>
        </div>

        <div class="charts-row">
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendQuoteVendors') }}</h3>
            <AnalyticsTrendChart
              :points="trendQuoteVendorPoints"
              :value-suffix="t('purchaseAnalytics.trendUnit.vendor')"
              :unit-caption="t('purchaseAnalytics.trendUnit.vendorCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendPoVendors') }}</h3>
            <AnalyticsTrendChart
              :points="trendPoVendorPoints"
              :value-suffix="t('purchaseAnalytics.trendUnit.vendor')"
              :unit-caption="t('purchaseAnalytics.trendUnit.vendorCaption')"
            />
          </div>
        </div>

        <div class="charts-row">
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendQuote') }}</h3>
            <AnalyticsTrendChart :points="trendQuotePoints" />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendItems') }}</h3>
            <AnalyticsTrendChart :points="trendItemPoints" />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendConversion') }}</h3>
            <AnalyticsTrendChart
              :points="trendConversionPoints"
              value-format="percent"
              :unit-caption="t('purchaseAnalytics.trendUnit.percentCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('purchaseAnalytics.sections.trendPayable') }}</h3>
            <AnalyticsTrendChart
              :points="trendPayablePoints"
              value-format="money"
              :unit-caption="t('purchaseAnalytics.trendUnit.moneyCaption')"
            />
          </div>
        </div>

        <div class="charts-row breakdown-row">
          <div v-for="group in breakdowns" :key="group.groupKey" class="card chart-panel">
            <AnalyticsBreakdownPieChart
              v-if="isPieBreakdown(group.groupKey)"
              :title="breakdownTitle(group)"
              :items="group.items"
              :value-format="breakdownValueFormat(group.groupKey)"
              :unit-caption="
                breakdownValueFormat(group.groupKey) === 'money'
                  ? t('purchaseAnalytics.trendUnit.moneyCaption')
                  : undefined
              "
            />
            <AnalyticsBreakdownChart
              v-else
              :title="breakdownTitle(group)"
              :items="group.items"
              :value-format="breakdownValueFormat(group.groupKey)"
              :unit-caption="
                breakdownValueFormat(group.groupKey) === 'money'
                  ? t('purchaseAnalytics.trendUnit.moneyCaption')
                  : undefined
              "
            />
          </div>
        </div>

        <div v-if="showOverviewRankings" class="rankings-row">
          <div class="card ranking-panel">
            <div class="section-title-row">
              <h3 class="section-title">{{ primaryRankingTitle }}</h3>
              <span v-if="!maskAmounts" class="unit-caption">{{ t('purchaseAnalytics.trendUnit.moneyCaption') }}</span>
            </div>
            <el-table
              :data="dashboard?.rankings.primary ?? []"
              size="small"
              stripe
              :class="viewLevel === 'department' ? 'ranking-table--drill' : ''"
              @row-click="onRankingRowClick"
              @row-dblclick="onRankingRowDblClick"
            >
              <el-table-column prop="name" :label="t('purchaseAnalytics.rankings.name')" />
              <el-table-column prop="orderCount" :label="t('purchaseAnalytics.rankings.orderCount')" width="90" />
              <el-table-column :label="t('purchaseAnalytics.rankings.amount')" width="160">
                <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
              </el-table-column>
            </el-table>
          </div>
        </div>
      </el-tab-pane>

      <el-tab-pane :label="t('purchaseAnalytics.contentTabs.vendor')" name="vendor" lazy>
        <PurchaseAnalyticsVendorPanel
          :query="analyticsQuery"
          :scope-context="scopeContext ?? null"
          :active="contentTab === 'vendor'"
        />
      </el-tab-pane>
      <el-tab-pane :label="t('purchaseAnalytics.contentTabs.quote')" name="quote" lazy>
        <QuoteListBoard
          mode="report"
          :report-query="analyticsQuery"
          :active="contentTab === 'quote'"
        />
      </el-tab-pane>
      <el-tab-pane :label="t('purchaseAnalytics.contentTabs.order')" name="order" lazy>
        <PurchaseOrderItemListBoard
          mode="report"
          :report-query="analyticsQuery"
          :active="contentTab === 'order'"
        />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<style scoped lang="scss">
.purchase-analytics-page {
  padding: 0 4px 24px;
}

.page-header {
  margin-bottom: 16px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.page-subtitle {
  margin: 6px 0 0;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 16px;
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.toolbar-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.scope-banner {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  font-size: 13px;
}

.scope-banner .muted {
  color: var(--el-text-color-secondary);
}

.content-tabs {
  padding-top: 8px;

  :deep(.el-tabs__header) {
    margin-bottom: 16px;
  }

  :deep(.el-tabs__content) {
    overflow: visible;
  }

  :deep(.el-tab-pane) {
    outline: none;
  }
}

.section {
  margin-bottom: 16px;
}

.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
}

.section-title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 12px;

  .section-title {
    margin: 0;
  }
}

.unit-caption {
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  flex-shrink: 0;
}

.charts-row,
.rankings-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.breakdown-row {
  grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
}

.chart-panel,
.ranking-panel {
  margin-bottom: 0;
}

.ranking-table--drill :deep(.el-table__row) {
  cursor: pointer;
}
</style>
