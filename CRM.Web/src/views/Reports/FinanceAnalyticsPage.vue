<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import AnalyticsScopeTabs from '@/components/Analytics/AnalyticsScopeTabs.vue'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsTrendChart from '@/components/Analytics/AnalyticsTrendChart.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import AnalyticsDefinitionButton from '@/components/Analytics/AnalyticsDefinitionButton.vue'
import AnalyticsPanelHeader from '@/components/Analytics/AnalyticsPanelHeader.vue'
import { useAnalyticsDefinition } from '@/composables/useAnalyticsDefinition'
import {
  financeAnalyticsApi,
  type FinanceAnalyticsBreakdownGroup,
  type FinanceAnalyticsDashboard,
  type FinanceAnalyticsMoney,
  type FinanceAnalyticsTrendPoint,
  type FinanceAnalyticsViewLevel
} from '@/api/analytics/finance'
import {
  buildCompletedDrillRoute,
  buildPaidCurrencyDrillRoute,
  buildReceivedCurrencyDrillRoute,
  buildTodoDrillRoute,
  canShowPaidCurrencyView,
  canShowReceivedCurrencyView,
  isCompletedDrillable,
  isTodoDrillable,
  type FinanceAnalyticsCompletedDrillKey,
  type FinanceAnalyticsTodoDrillKey
} from '@/utils/financeAnalyticsDrill'

const { t } = useI18n()
const { def } = useAnalyticsDefinition('financeAnalytics')
const router = useRouter()
const authStore = useAuthStore()
const currencyBreakdownDef = computed(() => def('breakdown.currency'))

const loading = ref(false)
const viewLevel = ref<FinanceAnalyticsViewLevel>('company')
const departmentId = ref<string | undefined>()
const ownerUserId = ref<string | undefined>()
const asOfDate = ref(formatDate(new Date()))
const trendDateRange = ref<[string, string]>(defaultTrendRange())
const groupBy = ref<'day' | 'week' | 'month'>('month')
const breakdownMetric = ref<'payable' | 'receivable' | 'pendingPurchaseInvoice' | 'pendingSellInvoice'>('payable')

const dashboard = ref<FinanceAnalyticsDashboard | null>(null)
const trends = ref<FinanceAnalyticsTrendPoint[]>([])
const breakdowns = ref<FinanceAnalyticsBreakdownGroup[]>([])

const scopeContext = computed(() => dashboard.value?.scopeContext)
const maskAmounts = computed(() => scopeContext.value?.maskAmounts === true)

function defaultTrendRange(): [string, string] {
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

function formatAmountNumber(amount: number): string {
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatFinanceMoneyDisplay(m?: FinanceAnalyticsMoney | null) {
  const usdCaption = t('financeAnalytics.kpi.usdEquivalent')
  const localCaption = t('financeAnalytics.kpi.localCurrency')

  if (!m || m.totalUsd == null) {
    return {
      value: '—',
      valueCaption: usdCaption,
      currencyCaption: undefined as string | undefined,
      currencyItems: undefined as
        | { currencyLabel: string; originalText: string; amountText: string; currency: number; usdText: string }[]
        | undefined
    }
  }

  const value = `$\u00a0${formatAmountNumber(m.totalUsd)}`

  const currencyItems = (m.byCurrency ?? [])
    .filter((c) => Math.abs(c.amount) >= 0.005)
    .sort((a, b) => a.currency - b.currency)
    .map((c) => {
      const amountText = formatAmountNumber(c.amount)
      return {
        currencyLabel: c.currencyLabel,
        originalText: `${amountText} ${c.currencyLabel}`,
        amountText,
        currency: c.currency,
        usdText: ''
      }
    })

  return {
    value,
    valueCaption: usdCaption,
    currencyCaption: currencyItems.length > 0 ? localCaption : undefined,
    currencyItems: currencyItems.length ? currencyItems : undefined
  }
}

function moneyKpiFields(m?: FinanceAnalyticsMoney | null) {
  const { value, valueCaption, currencyCaption, currencyItems } = formatFinanceMoneyDisplay(m)
  return { value, valueCaption, currencyCaption, currencyItems }
}

const showPaidCurrencyView = computed(() =>
  canShowPaidCurrencyView({
    viewLevel: viewLevel.value,
    accessMode: scopeContext.value?.accessMode,
    maskAmounts: maskAmounts.value,
    hasPaymentRead: authStore.hasPermission('finance-payment.read')
  })
)

const showReceivedCurrencyView = computed(() =>
  canShowReceivedCurrencyView({
    viewLevel: viewLevel.value,
    accessMode: scopeContext.value?.accessMode,
    maskAmounts: maskAmounts.value,
    hasReceiptRead: authStore.hasPermission('finance-receipt.read')
  })
)

function withCurrencyViewButtons(show: boolean, fields: ReturnType<typeof moneyKpiFields>) {
  if (!show || !fields.currencyItems?.length) return fields
  const viewLabel = t('financeAnalytics.kpi.viewRecords')
  return {
    ...fields,
    currencyItems: fields.currencyItems.map((cur) => ({
      ...cur,
      showView: cur.currency != null,
      viewLabel
    }))
  }
}

function buildBaseQuery() {
  return {
    viewLevel: viewLevel.value,
    departmentId: departmentId.value,
    ownerUserId: ownerUserId.value,
    dateTo: asOfDate.value,
    dateFrom: trendDateRange.value[0],
    groupBy: groupBy.value
  }
}

function drillScope() {
  return {
    dateFrom: trendDateRange.value[0],
    dateTo: trendDateRange.value[1],
    ownerUserId: ownerUserId.value,
    scopeContext: scopeContext.value
  }
}

const scopeDataForTabs = computed(() => {
  const ctx = scopeContext.value
  if (!ctx) return 0
  if (ctx.accessMode === 'salesPurchaseOnly') {
    if (ctx.saleDataScope === 0 || ctx.purchaseDataScope === 0) return 0
    return ctx.saleDataScope <= ctx.purchaseDataScope ? ctx.saleDataScope : ctx.purchaseDataScope
  }
  return ctx.financeDataScope
})

const showDepartmentSelect = computed(() => {
  const ctx = scopeContext.value
  if (!ctx || viewLevel.value !== 'department') return false
  if (ctx.accessMode === 'salesPurchaseOnly') return true
  return ctx.financeDataScope === 0 || ctx.financeDataScope === 3
})

const todoKpis = computed(() => {
  const todo = dashboard.value?.todo
  if (!todo) return []
  return [
    {
      key: 'payable',
      label: t('financeAnalytics.kpi.payableAmount'),
      ...moneyKpiFields(todo.payableAmount),
      valueFormat: 'money' as const,
      tone: 'todo' as const,
      drillable: isTodoDrillable('payable', maskAmounts.value) && authStore.hasPermission('finance-payment.read'),
      ...def('todo.payable')
    },
    {
      key: 'receivable',
      label: t('financeAnalytics.kpi.receivableAmount'),
      ...moneyKpiFields(todo.receivableAmount),
      valueFormat: 'money' as const,
      tone: 'todo' as const,
      drillable: isTodoDrillable('receivable', maskAmounts.value) && authStore.hasPermission('finance-receipt.read'),
      ...def('todo.receivable')
    },
    {
      key: 'pendingPurchaseInvoice',
      label: t('financeAnalytics.kpi.pendingPurchaseInvoiceAmount'),
      ...moneyKpiFields(todo.pendingPurchaseInvoiceAmount),
      valueFormat: 'money' as const,
      tone: 'todo' as const,
      drillable:
        isTodoDrillable('pendingPurchaseInvoice', maskAmounts.value) &&
        authStore.hasPermission('finance-purchase-invoice.read'),
      ...def('todo.pendingPurchaseInvoice')
    },
    {
      key: 'pendingSellInvoice',
      label: t('financeAnalytics.kpi.pendingSellInvoiceAmount'),
      ...moneyKpiFields(todo.pendingSellInvoiceAmount),
      valueFormat: 'money' as const,
      tone: 'todo' as const,
      drillable:
        isTodoDrillable('pendingSellInvoice', maskAmounts.value) &&
        authStore.hasPermission('finance-sell-invoice.read'),
      ...def('todo.pendingSellInvoice')
    }
  ]
})

const completedKpis = computed(() => {
  const c = dashboard.value?.completed
  if (!c) return []
  return [
    {
      key: 'paid',
      label: t('financeAnalytics.kpi.paidAmount'),
      ...withCurrencyViewButtons(showPaidCurrencyView.value, moneyKpiFields(c.paidAmount)),
      valueFormat: 'money' as const,
      ...def('completed.paid')
    },
    {
      key: 'received',
      label: t('financeAnalytics.kpi.receivedAmount'),
      ...withCurrencyViewButtons(showReceivedCurrencyView.value, moneyKpiFields(c.receivedAmount)),
      valueFormat: 'money' as const,
      ...def('completed.received')
    },
    {
      key: 'issuedPurchaseInvoice',
      label: t('financeAnalytics.kpi.issuedPurchaseInvoiceAmount'),
      ...moneyKpiFields(c.issuedPurchaseInvoiceAmount),
      valueFormat: 'money' as const,
      drillable:
        isCompletedDrillable('issuedPurchaseInvoice', maskAmounts.value) &&
        authStore.hasPermission('finance-purchase-invoice.read'),
      ...def('completed.issuedPurchaseInvoice')
    },
    {
      key: 'issuedSellInvoice',
      label: t('financeAnalytics.kpi.issuedSellInvoiceAmount'),
      ...moneyKpiFields(c.issuedSellInvoiceAmount),
      valueFormat: 'money' as const,
      drillable:
        isCompletedDrillable('issuedSellInvoice', maskAmounts.value) &&
        authStore.hasPermission('finance-sell-invoice.read'),
      ...def('completed.issuedSellInvoice')
    }
  ]
})

const trendPaidPoints = computed((): { period: string; value: number }[] =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.paidAmount?.totalUsd ?? 0
  }))
)

const trendReceivedPoints = computed((): { period: string; value: number }[] =>
  trends.value.map((p) => ({
    period: p.period,
    value: p.receivedAmount?.totalUsd ?? 0
  }))
)

const activeBreakdown = computed(() =>
  breakdowns.value.find((g) => g.groupKey === `currency:${breakdownMetric.value}`)
)

const breakdownMetricOptions = computed(() => [
  { value: 'payable', label: t('financeAnalytics.kpi.payableAmount') },
  { value: 'receivable', label: t('financeAnalytics.kpi.receivableAmount') },
  { value: 'pendingPurchaseInvoice', label: t('financeAnalytics.kpi.pendingPurchaseInvoiceAmount') },
  { value: 'pendingSellInvoice', label: t('financeAnalytics.kpi.pendingSellInvoiceAmount') }
])

async function loadData() {
  loading.value = true
  try {
    const q = buildBaseQuery()
    const [dash, trendRows, breakdownRows] = await Promise.all([
      financeAnalyticsApi.getDashboard(q),
      financeAnalyticsApi.getTrends(q),
      financeAnalyticsApi.getBreakdowns(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows

    const resolvedLevel = dash.scopeContext.viewLevel as FinanceAnalyticsViewLevel | undefined
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
    if (!ownerUserId.value && dash.scopeContext.resolvedOwnerUserId) {
      ownerUserId.value = dash.scopeContext.resolvedOwnerUserId ?? undefined
    }
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('financeAnalytics.loadFailed'))
  } finally {
    loading.value = false
  }
}

function onTodoKpiClick(key: string) {
  const route = buildTodoDrillRoute(key as FinanceAnalyticsTodoDrillKey, drillScope())
  if (!route) return
  void router.push(route)
}

function onCompletedKpiClick(key: string) {
  if (key === 'paid' || key === 'received') return
  const route = buildCompletedDrillRoute(key as FinanceAnalyticsCompletedDrillKey, drillScope())
  if (!route) return
  void router.push(route)
}

function onCompletedCurrencyView(itemKey: string, currency: number) {
  const scope = {
    dateFrom: trendDateRange.value[0],
    dateTo: asOfDate.value
  }
  const route =
    itemKey === 'paid'
      ? buildPaidCurrencyDrillRoute(scope, currency)
      : itemKey === 'received'
        ? buildReceivedCurrencyDrillRoute(scope, currency)
        : null
  if (!route) return
  void router.push(route)
}

watch([viewLevel, departmentId, ownerUserId, asOfDate, trendDateRange, groupBy], () => void loadData(), {
  immediate: true
})
</script>

<template>
  <div class="finance-analytics-page" v-loading="loading">
    <div class="page-header">
      <h2 class="page-title">{{ t('financeAnalytics.title') }}</h2>
      <p class="page-subtitle">{{ t('financeAnalytics.subtitle') }}</p>
    </div>

    <div class="toolbar card">
      <AnalyticsScopeTabs
        v-if="scopeContext"
        v-model="viewLevel"
        :allowed-levels="scopeContext.allowedViewLevels"
        :data-scope="scopeDataForTabs"
        i18n-prefix="financeAnalytics"
      />
      <div class="toolbar-filters">
        <span class="filter-label">{{ t('financeAnalytics.asOfDate') }}</span>
        <el-date-picker v-model="asOfDate" type="date" value-format="YYYY-MM-DD" style="width: 150px" />
        <el-date-picker
          v-model="trendDateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          :start-placeholder="t('financeAnalytics.dateFrom')"
          :end-placeholder="t('financeAnalytics.dateTo')"
        />
        <el-select v-if="showDepartmentSelect" v-model="departmentId" clearable style="width: 200px">
          <el-option
            v-for="d in scopeContext?.allowedDepartments ?? []"
            :key="d.id"
            :label="d.name"
            :value="d.id"
          />
        </el-select>
        <el-select v-model="groupBy" style="width: 120px">
          <el-option value="day" :label="t('financeAnalytics.groupBy.day')" />
          <el-option value="week" :label="t('financeAnalytics.groupBy.week')" />
          <el-option value="month" :label="t('financeAnalytics.groupBy.month')" />
        </el-select>
        <el-button type="primary" @click="loadData">{{ t('financeAnalytics.refresh') }}</el-button>
      </div>
    </div>

    <div v-if="scopeContext" class="scope-banner card">
      <span>{{ t('financeAnalytics.scopeBanner', { label: scopeContext.scopeLabel }) }}</span>
      <span v-if="scopeContext.exchangeRateHint" class="muted">{{ scopeContext.exchangeRateHint }}</span>
      <span class="muted">{{ t('financeAnalytics.receivableHint') }}</span>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('financeAnalytics.sections.todo') }}</h3>
      <AnalyticsKpiGrid :items="todoKpis" @item-click="onTodoKpiClick" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ t('financeAnalytics.sections.completed') }}</h3>
      <p class="section-hint">{{ t('financeAnalytics.completedHint') }}</p>
      <AnalyticsKpiGrid
        :items="completedKpis"
        @item-click="onCompletedKpiClick"
        @currency-view="onCompletedCurrencyView"
      />
    </section>

    <div class="charts-row">
      <div class="card chart-panel">
        <AnalyticsPanelHeader
          :title="t('financeAnalytics.sections.trendPaid')"
          :unit-caption="t('financeAnalytics.trendUnit.moneyCaption')"
          v-bind="def('trend.paid')"
        />
        <AnalyticsTrendChart :points="trendPaidPoints" value-format="money" />
      </div>
      <div class="card chart-panel">
        <AnalyticsPanelHeader
          :title="t('financeAnalytics.sections.trendReceived')"
          :unit-caption="t('financeAnalytics.trendUnit.moneyCaption')"
          v-bind="def('trend.received')"
        />
        <AnalyticsTrendChart :points="trendReceivedPoints" value-format="money" />
      </div>
    </div>

    <div class="charts-row breakdown-row">
      <div class="card chart-panel breakdown-panel">
        <div class="breakdown-header">
          <h3 class="section-title">{{ t('financeAnalytics.sections.currencyBreakdown') }}</h3>
          <div class="breakdown-header-right">
            <el-select v-model="breakdownMetric" style="width: 220px">
              <el-option
                v-for="opt in breakdownMetricOptions"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>
            <AnalyticsDefinitionButton
              tip-kind="chart"
              :chart="currencyBreakdownDef.definitionChart"
              :data-source="currencyBreakdownDef.definitionDataSource"
              :text="currencyBreakdownDef.definitionText"
            />
          </div>
        </div>
        <AnalyticsBreakdownPieChart
          v-if="activeBreakdown && activeBreakdown.items.length"
          :title="activeBreakdown.groupLabel"
          :items="activeBreakdown.items"
          value-format="originalCurrency"
          :unit-caption="t('financeAnalytics.trendUnit.originalCaption')"
        />
        <p v-else class="empty-hint">{{ t('financeAnalytics.noBreakdownData') }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.finance-analytics-page {
  padding: 0 4px 24px;
}

.page-header {
  margin-bottom: 20px;
}

.page-title {
  margin: 0 0 6px;
  font-size: 22px;
  font-weight: 600;
}

.page-subtitle {
  margin: 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
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
  gap: 16px;
  align-items: center;
  justify-content: space-between;
}

.toolbar-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.filter-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.scope-banner {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  font-size: 13px;

  .muted {
    color: var(--el-text-color-secondary);
  }
}

.section {
  margin-bottom: 20px;
}

.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
}

.section-hint {
  margin: -6px 0 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.charts-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.chart-panel {
  margin-bottom: 0;
}

.breakdown-header {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;

  .section-title {
    margin-bottom: 0;
  }
}

.breakdown-header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.empty-hint {
  margin: 24px 0;
  text-align: center;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>
