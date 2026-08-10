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
import StockOutProgressDetailDialog from '@/components/Analytics/StockOutProgressDetailDialog.vue'
import SalesAnalyticsCustomerPanel from '@/components/Analytics/SalesAnalyticsCustomerPanel.vue'
import SalesOrderItemListBoard from '@/views/RFQ/SalesOrderItemListBoard.vue'
import RfqItemListBoard from '@/views/RFQ/RfqItemListBoard.vue'
import {
  salesAnalyticsApi,
  type SalesAnalyticsBreakdownGroup,
  type SalesAnalyticsDashboard,
  type SalesAnalyticsTrendPoint,
  type SalesAnalyticsViewLevel
} from '@/api/analytics/sales'
import {
  buildSalesUserRankingDrillRoute,
  buildSnapshotDrillRoute,
  buildTodoDrillRoute,
  isSnapshotDrillable,
  isTodoDrillable,
  type SalesAnalyticsSnapshotDrillKey,
  type SalesAnalyticsTodoDrillKey
} from '@/utils/salesAnalyticsDrill'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()

const loading = ref(false)
const contentTab = ref<'overview' | 'customer' | 'rfq' | 'order'>('overview')
const viewLevel = ref<SalesAnalyticsViewLevel>('company')
const departmentId = ref<string | undefined>()
const salesUserId = ref<string | undefined>()
const dateRange = ref<[string, string]>(defaultDateRange())
const groupBy = ref<'day' | 'week' | 'month'>('month')

const dashboard = ref<SalesAnalyticsDashboard | null>(null)
const trends = ref<SalesAnalyticsTrendPoint[]>([])
const breakdowns = ref<SalesAnalyticsBreakdownGroup[]>([])
const stockOutDetailVisible = ref(false)

const scopeContext = computed(() => dashboard.value?.scopeContext ?? null)

const maskAmounts = computed(() => scopeContext.value?.maskAmounts === true)

function drillScope() {
  return {
    dateFrom: dateRange.value[0],
    dateTo: dateRange.value[1],
    salesUserId: salesUserId.value,
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

function formatAmountNumber(amount: number | null | undefined): string {
  if (amount == null) return '—'
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatOriginalMoney(amount: number | null | undefined, currencyLabel: string): string {
  if (amount == null) return '—'
  return `${formatAmountNumber(amount)} ${currencyLabel}`
}

function formatRate(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(2)}%`
}

function mapMoneyCurrencyItems(
  money: { byCurrency?: { currency: number; currencyLabel: string; amount: number }[] } | null | undefined
) {
  return (money?.byCurrency ?? []).map((line) => ({
    currencyLabel: line.currencyLabel,
    originalText: formatOriginalMoney(line.amount, line.currencyLabel),
    amountText: formatAmountNumber(line.amount),
    currency: line.currency,
    usdText: ''
  }))
}

function todoMoneyKpiFields(money: { totalUsd?: number | null; byCurrency?: { currency: number; currencyLabel: string; amount: number }[] } | null | undefined) {
  const currencyItems = mapMoneyCurrencyItems(money)
  return {
    value: formatMoney(money?.totalUsd),
    valueFormat: 'money' as const,
    layout: 'split' as const,
    valueCaption: t('salesAnalytics.kpi.usdCaption'),
    currencyCaption: currencyItems.length ? t('salesAnalytics.kpi.originalCaption') : undefined,
    currencyItems: currencyItems.length ? currencyItems : undefined
  }
}

/** 概况已出库/已收款：与成单金额同款，原币进「查看本币」Tip */
function snapshotMoneyKpiFields(money: { totalUsd?: number | null; byCurrency?: { currency: number; currencyLabel: string; amount: number }[] } | null | undefined) {
  const currencyItems = mapMoneyCurrencyItems(money)
  const hasUsd = money?.totalUsd != null
  return {
    value: formatMoney(money?.totalUsd),
    valueFormat: 'money' as const,
    valueSuffix: hasUsd ? `（${t('salesAnalytics.kpi.usdCaption')}）` : undefined,
    currencyCaption: t('salesAnalytics.kpi.originalCaption'),
    currencyItems,
    showCurrencyTip: hasUsd,
    currencyTipLabel: t('salesAnalytics.kpi.viewLocalCurrency')
  }
}

function canOpenTodoDetail(key: SalesAnalyticsTodoDrillKey): boolean {
  if (!isTodoDrillable(key, maskAmounts.value)) return false
  if (key === 'receivable') return authStore.hasPermission('finance-receipt.read')
  return authStore.hasPermission('sales-order.read')
}

const todoKpis = computed(() => {
  const todo = dashboard.value?.todo
  if (!todo) return []
  const defLabel = t('salesAnalytics.definitionTip.button')
  const detailLabel = t('salesAnalytics.stockOutProgressDetail.detail')
  return [
    {
      key: 'receivable',
      label: t('salesAnalytics.kpi.receivableAmount'),
      ...todoMoneyKpiFields(todo.receivableAmount),
      tone: 'todo' as const,
      showDefinition: true,
      definitionLabel: defLabel,
      definitionChart: t('salesAnalytics.todoReceivableDefinition.chart'),
      definitionDataSource: t('salesAnalytics.todoReceivableDefinition.dataSource'),
      definitionText: t('salesAnalytics.todoReceivableDefinition.text'),
      showDetail: canOpenTodoDetail('receivable'),
      detailLabel
    },
    {
      key: 'pendingStockOut',
      label: t('salesAnalytics.kpi.pendingStockOutItemCount'),
      value: String(todo.pendingStockOutItemCount ?? 0),
      tone: 'todo' as const,
      showDefinition: true,
      definitionLabel: defLabel,
      definitionChart: t('salesAnalytics.todoPendingStockOutDefinition.chart'),
      definitionDataSource: t('salesAnalytics.todoPendingStockOutDefinition.dataSource'),
      definitionText: t('salesAnalytics.todoPendingStockOutDefinition.text'),
      showDetail: canOpenTodoDetail('pendingStockOut'),
      detailLabel
    },
    {
      key: 'pendingInvoice',
      label: t('salesAnalytics.kpi.pendingInvoiceAmount'),
      ...todoMoneyKpiFields(todo.pendingInvoiceAmount),
      tone: 'todo' as const,
      showDefinition: true,
      definitionLabel: defLabel,
      definitionChart: t('salesAnalytics.todoPendingInvoiceDefinition.chart'),
      definitionDataSource: t('salesAnalytics.todoPendingInvoiceDefinition.dataSource'),
      definitionText: t('salesAnalytics.todoPendingInvoiceDefinition.text'),
      showDetail: canOpenTodoDetail('pendingInvoice'),
      detailLabel
    }
  ]
})

const snapshotKpis = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'rfqItems',
      label: t('salesAnalytics.kpi.rfqItemCount'),
      value: String(s.rfqItemCount),
      drillable: isSnapshotDrillable('rfqItems', maskAmounts.value) && authStore.hasPermission('rfq.read')
    },
    {
      key: 'rfqCustomers',
      label: t('salesAnalytics.kpi.rfqCustomerCount'),
      value: String(s.rfqCustomerCount),
      drillable: isSnapshotDrillable('rfqCustomers', maskAmounts.value) && authStore.hasPermission('rfq.read')
    },
    {
      key: 'conversion',
      label: t('salesAnalytics.kpi.rfqToSalesConversionRate'),
      value: formatRate(s.rfqToSalesConversionRate)
    },
    {
      key: 'soItems',
      label: t('salesAnalytics.kpi.salesOrderItemCount'),
      value: String(s.salesOrderItemCount),
      drillable: isSnapshotDrillable('soItems', maskAmounts.value) && authStore.hasPermission('sales-order.read')
    },
    {
      key: 'soCustomers',
      label: t('salesAnalytics.kpi.salesOrderCustomerCount'),
      value: String(s.salesOrderCustomerCount),
      drillable: isSnapshotDrillable('soCustomers', maskAmounts.value) && authStore.hasPermission('sales-order.read')
    },
    {
      key: 'amount',
      label: t('salesAnalytics.kpi.salesAmountApproved'),
      value: formatMoney(s.salesAmountApproved),
      valueSuffix:
        s.salesAmountApproved == null ? undefined : `（${t('salesAnalytics.kpi.usdCaption')}）`,
      valueFormat: 'money' as const,
      forceNewRow: true,
      drillable: isSnapshotDrillable('amount', maskAmounts.value) && authStore.hasPermission('sales-order.read')
    },
    {
      key: 'stockOut',
      label: t('salesAnalytics.kpi.salesAmountStockOut'),
      ...snapshotMoneyKpiFields(s.salesAmountStockOut),
      drillable: isSnapshotDrillable('stockOut', maskAmounts.value) && authStore.hasPermission('sales-order.read')
    },
    {
      key: 'received',
      label: t('salesAnalytics.kpi.salesAmountReceived'),
      ...snapshotMoneyKpiFields(s.salesAmountReceived),
      drillable: isSnapshotDrillable('received', maskAmounts.value) && authStore.hasPermission('sales-order.read')
    }
  ]
})

const trendAmountPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.salesAmountApproved ?? 0
  }))
)

const trendItemPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.salesOrderItemCount
  }))
)

const trendRfqPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.rfqItemCount
  }))
)

const trendRfqCustomerPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.rfqCustomerCount
  }))
)

const trendSalesCustomerPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.salesOrderCustomerCount
  }))
)

const trendConversionPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.rfqToSalesConversionRate ?? 0
  }))
)

const trendStockOutPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.salesAmountStockOut ?? 0
  }))
)

const trendReceivedPoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.salesAmountReceived ?? 0
  }))
)

const trendReceivablePoints = computed(() =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.receivableAmount ?? 0
  }))
)

const primaryRankingTitle = computed(() => {
  const lvl = viewLevel.value
  if (lvl === 'company') return t('salesAnalytics.rankings.departmentTop')
  if (lvl === 'department') return t('salesAnalytics.rankings.salesUserTop')
  return ''
})

/** 方案 A：客户排行仅在「客户」Tab；个人层概况无排行 */
const showOverviewRankings = computed(() => viewLevel.value === 'company' || viewLevel.value === 'department')

const analyticsQuery = computed(() => ({
  viewLevel: viewLevel.value,
  departmentId: departmentId.value,
  salesUserId: salesUserId.value,
  dateFrom: dateRange.value[0],
  dateTo: dateRange.value[1],
  groupBy: groupBy.value
}))

const showDepartmentSelect = computed(() => {
  const ctx = scopeContext.value
  if (!ctx) return false
  return viewLevel.value === 'department' && (ctx.saleDataScope === 0 || ctx.saleDataScope === 3)
})

const showSalesUserSelect = computed(() => {
  const ctx = scopeContext.value
  if (!ctx) return false
  return viewLevel.value === 'personal' && (ctx.allowedSalesUsers?.length ?? 0) > 1
})

const pieBreakdownKeys = new Set(['pipelineStage', 'currency'])

function isPieBreakdown(groupKey: string): boolean {
  return pieBreakdownKeys.has(groupKey)
}

function breakdownValueFormat(groupKey: string): 'money' | 'number' {
  if ((groupKey === 'currency' || groupKey === 'orderStatus') && !maskAmounts.value) return 'money'
  return 'number'
}

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  if (group.groupKey === 'currency' && maskAmounts.value) {
    return `${group.groupLabel}（${t('salesAnalytics.breakdown.currencyByCount')}）`
  }
  return group.groupLabel
}

function buildQuery() {
  return {
    viewLevel: viewLevel.value,
    departmentId: departmentId.value,
    salesUserId: salesUserId.value,
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
      salesAnalyticsApi.getDashboard(q),
      salesAnalyticsApi.getTrends(q),
      salesAnalyticsApi.getBreakdowns(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows

    // 后端可能将非法 company 回落到默认层；同步 Tab，避免 UI 仍显示「公司」
    const resolvedLevel = dash.scopeContext.viewLevel as SalesAnalyticsViewLevel | undefined
    if (resolvedLevel && resolvedLevel !== viewLevel.value) {
      viewLevel.value = resolvedLevel
    } else if (
      dash.scopeContext.allowedViewLevels.length &&
      !dash.scopeContext.allowedViewLevels.includes(viewLevel.value)
    ) {
      viewLevel.value = dash.scopeContext.viewLevel
    }
    if (!departmentId.value && dash.scopeContext.resolvedDepartmentId) {
      departmentId.value = dash.scopeContext.resolvedDepartmentId ?? undefined
    }
    if (!salesUserId.value && dash.scopeContext.resolvedSalesUserId) {
      salesUserId.value = dash.scopeContext.resolvedSalesUserId ?? undefined
    }
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('salesAnalytics.loadFailed')
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
    salesUserId.value = row.id
  }
}

function onRankingRowDblClick(row: { id: string; name: string }) {
  if (!authStore.hasPermission('sales-order.read')) {
    ElMessage.warning(t('salesAnalytics.drill.noPermission'))
    return
  }
  if (viewLevel.value === 'department') {
    void router.push(buildSalesUserRankingDrillRoute(row.id, row.name, drillScope()))
  }
}

function onTodoKpiDetail(key: string) {
  const drillKey = key as SalesAnalyticsTodoDrillKey
  if (!canOpenTodoDetail(drillKey)) {
    ElMessage.warning(t('salesAnalytics.drill.noPermission'))
    return
  }
  const route = buildTodoDrillRoute(drillKey, drillScope())
  if (!route) return
  const href = router.resolve(route).href
  window.open(href, '_blank', 'noopener,noreferrer')
}

function onSnapshotKpiClick(key: string) {
  if (key === 'conversion') return
  const route = buildSnapshotDrillRoute(key as SalesAnalyticsSnapshotDrillKey, drillScope())
  if (!route) return
  void router.push(route)
}

watch([viewLevel, departmentId, salesUserId, dateRange, groupBy], () => void loadData(), { immediate: true })
</script>

<template>
  <div class="sales-analytics-page" v-loading="loading">
    <div class="page-header">
      <h2 class="page-title">{{ t('salesAnalytics.title') }}</h2>
      <p class="page-subtitle">{{ t('salesAnalytics.subtitle') }}</p>
    </div>

    <div class="toolbar card">
      <AnalyticsScopeTabs
        v-if="scopeContext"
        v-model="viewLevel"
        :allowed-levels="scopeContext.allowedViewLevels"
        :sale-data-scope="scopeContext.saleDataScope"
      />
      <div class="toolbar-filters">
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          :start-placeholder="t('salesAnalytics.dateFrom')"
          :end-placeholder="t('salesAnalytics.dateTo')"
        />
        <el-select v-if="showDepartmentSelect" v-model="departmentId" clearable style="width: 200px">
          <el-option
            v-for="d in scopeContext?.allowedDepartments ?? []"
            :key="d.id"
            :label="d.name"
            :value="d.id"
          />
        </el-select>
        <el-select v-if="showSalesUserSelect" v-model="salesUserId" clearable filterable style="width: 200px">
          <el-option
            v-for="u in scopeContext?.allowedSalesUsers ?? []"
            :key="u.id"
            :label="u.name"
            :value="u.id"
          />
        </el-select>
        <el-select v-model="groupBy" style="width: 120px">
          <el-option value="day" :label="t('salesAnalytics.groupBy.day')" />
          <el-option value="week" :label="t('salesAnalytics.groupBy.week')" />
          <el-option value="month" :label="t('salesAnalytics.groupBy.month')" />
        </el-select>
        <el-button type="primary" @click="loadData">{{ t('salesAnalytics.refresh') }}</el-button>
      </div>
    </div>

    <div v-if="scopeContext" class="scope-banner card">
      <span>{{ t('salesAnalytics.scopeBanner', { label: scopeContext.scopeLabel }) }}</span>
      <span class="muted">{{ t('salesAnalytics.metricHint') }}</span>
      <span class="muted">{{ t('salesAnalytics.drill.hint') }}</span>
    </div>

    <el-tabs v-model="contentTab" class="content-tabs card">
      <el-tab-pane :label="t('salesAnalytics.contentTabs.overview')" name="overview">
        <section class="section">
          <h3 class="section-title">{{ t('salesAnalytics.sections.todo') }}</h3>
          <AnalyticsKpiGrid :items="todoKpis" @detail="onTodoKpiDetail" />
        </section>

        <section class="section">
          <h3 class="section-title">{{ t('salesAnalytics.sections.snapshot') }}</h3>
          <AnalyticsKpiGrid :items="snapshotKpis" @item-click="onSnapshotKpiClick" />
        </section>

        <div class="charts-row">
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendAmount') }}</h3>
            <AnalyticsTrendChart
              :points="trendAmountPoints"
              value-format="money"
              :unit-caption="t('salesAnalytics.trendUnit.moneyCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendStockOut') }}</h3>
            <AnalyticsTrendChart
              :points="trendStockOutPoints"
              value-format="money"
              :unit-caption="t('salesAnalytics.trendUnit.moneyCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendReceived') }}</h3>
            <AnalyticsTrendChart
              :points="trendReceivedPoints"
              value-format="money"
              :unit-caption="t('salesAnalytics.trendUnit.moneyCaption')"
            />
          </div>
        </div>

        <div class="charts-row">
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendRfqCustomers') }}</h3>
            <AnalyticsTrendChart
              :points="trendRfqCustomerPoints"
              :value-suffix="t('salesAnalytics.trendUnit.customer')"
              :unit-caption="t('salesAnalytics.trendUnit.customerCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendSalesCustomers') }}</h3>
            <AnalyticsTrendChart
              :points="trendSalesCustomerPoints"
              :value-suffix="t('salesAnalytics.trendUnit.customer')"
              :unit-caption="t('salesAnalytics.trendUnit.customerCaption')"
            />
          </div>
        </div>

        <div class="charts-row">
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendRfq') }}</h3>
            <AnalyticsTrendChart :points="trendRfqPoints" />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendItems') }}</h3>
            <AnalyticsTrendChart :points="trendItemPoints" />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendConversion') }}</h3>
            <AnalyticsTrendChart
              :points="trendConversionPoints"
              value-format="percent"
              :unit-caption="t('salesAnalytics.trendUnit.percentCaption')"
            />
          </div>
          <div class="card chart-panel">
            <h3 class="section-title">{{ t('salesAnalytics.sections.trendReceivable') }}</h3>
            <AnalyticsTrendChart
              :points="trendReceivablePoints"
              value-format="money"
              :unit-caption="t('salesAnalytics.trendUnit.moneyCaption')"
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
                  ? t('salesAnalytics.trendUnit.moneyCaption')
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
                  ? t('salesAnalytics.trendUnit.moneyCaption')
                  : undefined
              "
              :show-definition="group.groupKey === 'stockOutProgress'"
              :definition-label="t('salesAnalytics.definitionTip.button')"
              :definition-chart="t('salesAnalytics.stockOutProgressDefinition.chart')"
              :definition-data-source="t('salesAnalytics.stockOutProgressDefinition.dataSource')"
              :definition-text="t('salesAnalytics.stockOutProgressDefinition.text')"
              :show-detail="group.groupKey === 'stockOutProgress'"
              :detail-label="t('salesAnalytics.stockOutProgressDetail.detail')"
              @detail="stockOutDetailVisible = true"
            />
          </div>
        </div>

        <StockOutProgressDetailDialog v-model="stockOutDetailVisible" :query="buildQuery()" />

        <div v-if="showOverviewRankings" class="rankings-row">
          <div class="card ranking-panel">
            <div class="section-title-row">
              <h3 class="section-title">{{ primaryRankingTitle }}</h3>
              <span v-if="!maskAmounts" class="unit-caption">{{ t('salesAnalytics.trendUnit.moneyCaption') }}</span>
            </div>
            <el-table
              :data="dashboard?.rankings.primary ?? []"
              size="small"
              stripe
              :class="'ranking-table--drill'"
              @row-click="onRankingRowClick"
              @row-dblclick="onRankingRowDblClick"
            >
              <el-table-column prop="name" :label="t('salesAnalytics.rankings.name')" />
              <el-table-column prop="orderCount" :label="t('salesAnalytics.rankings.orderCount')" width="90" />
              <el-table-column :label="t('salesAnalytics.rankings.amount')" width="140">
                <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
              </el-table-column>
            </el-table>
          </div>
        </div>
      </el-tab-pane>

      <el-tab-pane :label="t('salesAnalytics.contentTabs.customer')" name="customer" lazy>
        <SalesAnalyticsCustomerPanel
          :query="analyticsQuery"
          :scope-context="scopeContext"
          :active="contentTab === 'customer'"
        />
      </el-tab-pane>
      <el-tab-pane :label="t('salesAnalytics.contentTabs.rfq')" name="rfq" lazy>
        <RfqItemListBoard
          mode="report"
          :report-query="analyticsQuery"
          :active="contentTab === 'rfq'"
        />
      </el-tab-pane>
      <el-tab-pane :label="t('salesAnalytics.contentTabs.order')" name="order" lazy>
        <SalesOrderItemListBoard
          mode="report"
          :report-query="analyticsQuery"
          :active="contentTab === 'order'"
        />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<style scoped lang="scss">
.sales-analytics-page {
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
