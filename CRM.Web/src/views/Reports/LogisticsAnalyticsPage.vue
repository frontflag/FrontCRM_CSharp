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
import AnalyticsPanelHeader from '@/components/Analytics/AnalyticsPanelHeader.vue'
import { useAnalyticsDefinition } from '@/composables/useAnalyticsDefinition'
import {
  logisticsAnalyticsApi,
  type LogisticsAnalyticsBreakdownGroup,
  type LogisticsAnalyticsCustomerMatrix,
  type LogisticsAnalyticsDashboard,
  type LogisticsAnalyticsTrendPoint,
  type LogisticsAnalyticsViewLevel,
  type LogisticsInventoryType,
  type LogisticsMatrixSubject
} from '@/api/analytics/logistics'
import {
  buildPendingStockInDrillRoute,
  buildStockInFlowDrillRoute,
  buildStockItemListDrillRoute,
  buildStockOutFlowDrillRoute
} from '@/utils/logisticsAnalyticsDrill'

const { t } = useI18n()
const { def } = useAnalyticsDefinition('logisticsAnalytics')
const router = useRouter()
const authStore = useAuthStore()

const loading = ref(false)
const matrixLoading = ref(false)
const viewLevel = ref<LogisticsAnalyticsViewLevel>('company')
const departmentId = ref<string | undefined>()
const inventoryType = ref<LogisticsInventoryType>('all')
const matrixSubject = ref<LogisticsMatrixSubject>('vendor')
const asOfDate = ref(formatDate(new Date()))
const trendDateRange = ref<[string, string]>(defaultTrendRange())
const groupBy = ref<'day' | 'week' | 'month'>('month')

const dashboard = ref<LogisticsAnalyticsDashboard | null>(null)
const trends = ref<LogisticsAnalyticsTrendPoint[]>([])
const breakdowns = ref<LogisticsAnalyticsBreakdownGroup[]>([])
const customerMatrix = ref<LogisticsAnalyticsCustomerMatrix | null>(null)

const scopeContext = computed(() => dashboard.value?.scopeContext)
const maskAmounts = computed(() => scopeContext.value?.maskAmounts === true)
const maskSalesAmounts = computed(() => scopeContext.value?.maskSalesAmounts === true)

const inventoryTypeOptions: { value: LogisticsInventoryType; labelKey: string }[] = [
  { value: 'all', labelKey: 'logisticsAnalytics.inventoryType.all' },
  { value: 'customerOrder', labelKey: 'logisticsAnalytics.inventoryType.customerOrder' },
  { value: 'purchaseStock', labelKey: 'logisticsAnalytics.inventoryType.purchaseStock' }
]

const matrixSubjectOptions: { value: LogisticsMatrixSubject; labelKey: string }[] = [
  { value: 'salesperson', labelKey: 'logisticsAnalytics.matrixSubject.salesperson' },
  { value: 'vendor', labelKey: 'logisticsAnalytics.matrixSubject.vendor' },
  { value: 'purchaser', labelKey: 'logisticsAnalytics.matrixSubject.purchaser' },
  { value: 'brand', labelKey: 'logisticsAnalytics.matrixSubject.brand' }
]

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

function snapshotMoneyKpiFields(
  money: { totalUsd?: number | null; byCurrency?: { currency: number; currencyLabel: string; amount: number }[] } | null | undefined
) {
  const currencyItems = mapMoneyCurrencyItems(money)
  const hasUsd = money?.totalUsd != null
  return {
    value: formatMoney(money?.totalUsd),
    valueFormat: 'money' as const,
    valueSuffix: hasUsd ? t('logisticsAnalytics.kpi.convertedUsdSuffix') : undefined,
    currencyCaption: t('logisticsAnalytics.kpi.originalCaption'),
    currencyItems,
    showCurrencyTip: hasUsd,
    currencyTipLabel: t('logisticsAnalytics.kpi.viewLocalCurrency')
  }
}

function formatAge(v?: number | null): string {
  if (v == null) return '—'
  return `${v.toFixed(1)} ${t('logisticsAnalytics.unit.days')}`
}

function buildBaseQuery() {
  return {
    viewLevel: viewLevel.value,
    departmentId: departmentId.value,
    inventoryType: inventoryType.value,
    dateTo: asOfDate.value,
    dateFrom: trendDateRange.value[0],
    trendDateTo: trendDateRange.value[1],
    groupBy: groupBy.value
  }
}

const todoKpis = computed(() => {
  const todo = dashboard.value?.todo
  if (!todo) return []
  return [
    {
      key: 'pendingStockIn',
      label: t('logisticsAnalytics.kpi.pendingStockInQty'),
      value: String(todo.pendingStockInQty ?? 0),
      tone: 'todo' as const,
      drillable: authStore.hasPermission('purchase-order.read'),
      ...def('todo.pendingStockInQty')
    }
  ]
})

const flowKpis = computed(() => {
  const flow = dashboard.value?.flow
  if (!flow) return []
  return [
    {
      key: 'stockInAmount',
      label: t('logisticsAnalytics.kpi.stockInAmount'),
      ...snapshotMoneyKpiFields(flow.stockInAmount),
      drillable: !maskAmounts.value && authStore.hasPermission('inventory.read'),
      ...def('flow.stockInAmount')
    },
    {
      key: 'stockOutAmount',
      label: t('logisticsAnalytics.kpi.stockOutAmount'),
      ...snapshotMoneyKpiFields(flow.stockOutAmount),
      drillable: !maskSalesAmounts.value && authStore.hasPermission('inventory.read'),
      ...def('flow.stockOutAmount')
    }
  ]
})

const snapshotKpis = computed(() => {
  const s = dashboard.value?.snapshot
  if (!s) return []
  const c = s.subjectCounts
  return [
    {
      key: 'onHandQty',
      label: t('logisticsAnalytics.kpi.onHandQty'),
      value: String(s.onHandQty),
      drillable: authStore.hasPermission('inventory.read'),
      ...def('snapshot.onHandQty')
    },
    {
      key: 'onHandAmount',
      label: t('logisticsAnalytics.kpi.onHandAmountUsd'),
      value: formatMoney(s.onHandAmountUsd),
      valueFormat: 'money' as const,
      valueSuffix: t('logisticsAnalytics.kpi.convertedUsdSuffix'),
      drillable: !maskAmounts.value && authStore.hasPermission('inventory.read'),
      ...def('snapshot.onHandAmountUsd')
    },
    {
      key: 'avgAge',
      label: t('logisticsAnalytics.kpi.weightedAvgAgeDays'),
      value: formatAge(s.weightedAvgAgeDays),
      ...def('snapshot.weightedAvgAgeDays')
    },
    {
      key: 'customerCount',
      label: t('logisticsAnalytics.kpi.customerCount'),
      value: String(c.customer),
      ...def('snapshot.customerCount')
    },
    {
      key: 'salespersonCount',
      label: t('logisticsAnalytics.kpi.salespersonCount'),
      value: String(c.salesperson),
      ...def('snapshot.salespersonCount')
    },
    {
      key: 'vendorCount',
      label: t('logisticsAnalytics.kpi.vendorCount'),
      value: String(c.vendor),
      ...def('snapshot.vendorCount')
    },
    {
      key: 'purchaserCount',
      label: t('logisticsAnalytics.kpi.purchaserCount'),
      value: String(c.purchaser),
      ...def('snapshot.purchaserCount')
    },
    {
      key: 'brandCount',
      label: t('logisticsAnalytics.kpi.brandCount'),
      value: String(c.brand),
      ...def('snapshot.brandCount')
    }
  ]
})

const trendStockInPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.stockInQty }))
)

const trendStockOutPoints = computed(() =>
  trends.value.map((p) => ({ period: p.period, value: p.stockOutQty }))
)

const scopeDataForTabs = computed(() => {
  const ctx = scopeContext.value
  if (!ctx) return 0
  if (ctx.accessMode === 'salesPurchaseOnly') {
    if (ctx.saleDataScope === 0 || ctx.purchaseDataScope === 0) return 0
    return ctx.saleDataScope <= ctx.purchaseDataScope ? ctx.saleDataScope : ctx.purchaseDataScope
  }
  return ctx.logisticsDataScope
})

const showDepartmentSelect = computed(() => {
  const ctx = scopeContext.value
  if (!ctx || viewLevel.value !== 'department') return false
  if (ctx.accessMode === 'salesPurchaseOnly') return true
  return ctx.logisticsDataScope === 0 || ctx.logisticsDataScope === 3
})

const primaryRankingTitle = computed(() => {
  if (viewLevel.value === 'company') return t('logisticsAnalytics.rankings.customerTop')
  if (viewLevel.value === 'department') return t('logisticsAnalytics.rankings.salespersonTop')
  return t('logisticsAnalytics.rankings.vendorTop')
})

const matrixSubjectColumnLabel = computed(() =>
  t(`logisticsAnalytics.matrix.subjectLabel.${matrixSubject.value}`)
)

const ageBreakdown = computed(() => breakdowns.value.find((g) => g.groupKey === 'ageBucket'))

async function loadDashboardBundle() {
  loading.value = true
  try {
    const q = buildBaseQuery()
    const [dash, trendRows, breakdownRows] = await Promise.all([
      logisticsAnalyticsApi.getDashboard(q),
      logisticsAnalyticsApi.getTrends(q),
      logisticsAnalyticsApi.getBreakdowns(q)
    ])
    dashboard.value = dash
    trends.value = trendRows
    breakdowns.value = breakdownRows

    const resolvedLevel = dash.scopeContext.viewLevel as LogisticsAnalyticsViewLevel | undefined
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
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('logisticsAnalytics.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function loadMatrix() {
  matrixLoading.value = true
  try {
    customerMatrix.value = await logisticsAnalyticsApi.getCustomerMatrix({
      ...buildBaseQuery(),
      matrixSubject: matrixSubject.value
    })
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('logisticsAnalytics.matrixLoadFailed'))
  } finally {
    matrixLoading.value = false
  }
}

async function loadAll() {
  await loadDashboardBundle()
  await loadMatrix()
}

function onTodoKpiClick(key: string) {
  if (key !== 'pendingStockIn') return
  void router.push(
    buildPendingStockInDrillRoute({
      inventoryType: inventoryType.value,
      scopeContext: scopeContext.value
    })
  )
}

function onFlowKpiClick(key: string) {
  const range = {
    dateFrom: trendDateRange.value[0],
    dateTo: trendDateRange.value[1]
  }
  if (key === 'stockInAmount') {
    if (maskAmounts.value || !authStore.hasPermission('inventory.read')) return
    void router.push(buildStockInFlowDrillRoute(range))
    return
  }
  if (key === 'stockOutAmount') {
    if (maskSalesAmounts.value || !authStore.hasPermission('inventory.read')) return
    void router.push(buildStockOutFlowDrillRoute(range))
  }
}

function onSnapshotKpiClick(key: string) {
  if (key === 'avgAge' || key.endsWith('Count')) return
  void router.push(
    buildStockItemListDrillRoute({
      inventoryType: inventoryType.value,
      scopeContext: scopeContext.value
    })
  )
}

function onRankingRowClick(row: { id: string; name: string }) {
  onMatrixRowDrill({ anchorCustomerName: row.name, anchorCustomerId: row.id })
}

function onMatrixRowDrill(row: { anchorCustomerName: string; anchorCustomerId?: string | null }) {
  if (!authStore.hasPermission('inventory.read')) return
  const isUnassigned = !row.anchorCustomerId
  void router.push(
    buildStockItemListDrillRoute({
      inventoryType: inventoryType.value,
      customerName: isUnassigned ? undefined : row.anchorCustomerName,
      scopeContext: scopeContext.value
    })
  )
}

function onMatrixChildDrill(
  anchor: { anchorCustomerName: string; anchorCustomerId?: string | null },
  child: { subjectKey: string; subjectLabel: string }
) {
  if (!authStore.hasPermission('inventory.read') || child.subjectKey === '__none__') return
  const drill: Parameters<typeof buildStockItemListDrillRoute>[0] = {
    inventoryType: inventoryType.value,
    scopeContext: scopeContext.value
  }
  if (anchor.anchorCustomerId) drill.customerName = anchor.anchorCustomerName
  if (matrixSubject.value === 'vendor') drill.vendorName = child.subjectLabel
  if (matrixSubject.value === 'brand') drill.purchaseBrand = child.subjectLabel
  if (matrixSubject.value === 'salesperson') drill.salespersonUserId = child.subjectKey
  if (matrixSubject.value === 'purchaser') drill.purchaserUserId = child.subjectKey
  void router.push(buildStockItemListDrillRoute(drill))
}

function matrixChildRowClickFactory(anchor: { anchorCustomerName: string; anchorCustomerId?: string | null }) {
  return (child: { subjectKey: string; subjectLabel: string }) => onMatrixChildDrill(anchor, child)
}

watch([viewLevel, departmentId, inventoryType, asOfDate, trendDateRange, groupBy], () => void loadAll(), {
  immediate: true
})
watch(matrixSubject, () => void loadMatrix())
</script>

<template>
  <div class="logistics-analytics-page" v-loading="loading">
    <div class="page-header">
      <h2 class="page-title">{{ t('logisticsAnalytics.title') }}</h2>
      <p class="page-subtitle">{{ t('logisticsAnalytics.subtitle') }}</p>
    </div>

    <div class="toolbar card">
      <AnalyticsScopeTabs
        v-if="scopeContext"
        v-model="viewLevel"
        :allowed-levels="scopeContext.allowedViewLevels"
        :data-scope="scopeDataForTabs"
        i18n-prefix="logisticsAnalytics"
      />
      <div class="toolbar-filters">
        <el-radio-group v-model="inventoryType" size="default">
          <el-radio-button v-for="opt in inventoryTypeOptions" :key="opt.value" :value="opt.value">
            {{ t(opt.labelKey) }}
          </el-radio-button>
        </el-radio-group>
        <el-date-picker
          v-model="asOfDate"
          type="date"
          value-format="YYYY-MM-DD"
          :placeholder="t('logisticsAnalytics.asOfDate')"
        />
        <el-date-picker
          v-model="trendDateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          :start-placeholder="t('logisticsAnalytics.dateFrom')"
          :end-placeholder="t('logisticsAnalytics.dateTo')"
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
          <el-option value="day" :label="t('logisticsAnalytics.groupBy.day')" />
          <el-option value="week" :label="t('logisticsAnalytics.groupBy.week')" />
          <el-option value="month" :label="t('logisticsAnalytics.groupBy.month')" />
        </el-select>
        <el-button type="primary" @click="loadAll">{{ t('logisticsAnalytics.refresh') }}</el-button>
      </div>
    </div>

    <div v-if="scopeContext" class="scope-banner card">
      <span>{{ t('logisticsAnalytics.scopeBanner', { label: scopeContext.scopeLabel }) }}</span>
      <span class="muted">{{ t('logisticsAnalytics.metricHint') }}</span>
      <span v-if="scopeContext.accessMode === 'salesPurchaseOnly'" class="muted">
        {{ t('logisticsAnalytics.salesPurchaseOnlyHint') }}
      </span>
    </div>

    <section class="section">
      <h3 class="section-title">{{ t('logisticsAnalytics.sections.todo') }}</h3>
      <AnalyticsKpiGrid :items="todoKpis" @item-click="onTodoKpiClick" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ t('logisticsAnalytics.sections.flow') }}</h3>
      <AnalyticsKpiGrid :items="flowKpis" @item-click="onFlowKpiClick" />
    </section>

    <section class="section">
      <h3 class="section-title">{{ t('logisticsAnalytics.sections.snapshot') }}</h3>
      <AnalyticsKpiGrid :items="snapshotKpis" @item-click="onSnapshotKpiClick" />
    </section>

    <div class="charts-row">
      <div v-if="ageBreakdown" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          :title="ageBreakdown.groupLabel"
          :items="ageBreakdown.items"
          value-format="number"
          v-bind="def('breakdown.ageBucket')"
        />
      </div>
      <div class="card chart-panel">
        <AnalyticsPanelHeader
          :title="t('logisticsAnalytics.sections.trendStockIn')"
          v-bind="def('trend.stockInQty')"
        />
        <AnalyticsTrendChart :points="trendStockInPoints" />
      </div>
      <div class="card chart-panel">
        <AnalyticsPanelHeader
          :title="t('logisticsAnalytics.sections.trendStockOut')"
          v-bind="def('trend.stockOutQty')"
        />
        <AnalyticsTrendChart :points="trendStockOutPoints" />
      </div>
    </div>

    <section class="section card matrix-section" v-loading="matrixLoading">
      <div class="matrix-header">
        <AnalyticsPanelHeader
          :title="t('logisticsAnalytics.sections.customerMatrix')"
          v-bind="def('matrix.customer')"
        />
        <el-radio-group v-model="matrixSubject" size="small">
          <el-radio-button v-for="opt in matrixSubjectOptions" :key="opt.value" :value="opt.value">
            {{ t(opt.labelKey) }}
          </el-radio-button>
        </el-radio-group>
      </div>
      <el-table
        :data="customerMatrix?.rows ?? []"
        size="small"
        stripe
        row-key="anchorCustomerName"
        default-expand-all
      >
        <el-table-column type="expand">
          <template #default="{ row }">
            <el-table
              :data="row.children"
              size="small"
              @row-click="matrixChildRowClickFactory(row)"
            >
              <el-table-column prop="subjectLabel" :label="matrixSubjectColumnLabel" min-width="160" />
              <el-table-column
                prop="onHandQty"
                :label="t('logisticsAnalytics.kpi.onHandQty')"
                min-width="132"
                class-name="matrix-col-qty"
                label-class-name="matrix-col-qty-header"
              />
              <el-table-column :label="t('logisticsAnalytics.kpi.onHandAmountUsd')" width="140">
                <template #default="{ row: child }">{{ formatMoney(child.onHandAmountUsd) }}</template>
              </el-table-column>
              <el-table-column :label="t('logisticsAnalytics.kpi.weightedAvgAgeDays')" width="120">
                <template #default="{ row: child }">{{ formatAge(child.weightedAvgAgeDays) }}</template>
              </el-table-column>
            </el-table>
          </template>
        </el-table-column>
        <el-table-column prop="anchorCustomerName" :label="t('logisticsAnalytics.matrix.customer')" min-width="160" />
        <el-table-column
          prop="onHandQty"
          :label="t('logisticsAnalytics.kpi.onHandQty')"
          min-width="132"
          class-name="matrix-col-qty"
          label-class-name="matrix-col-qty-header"
        />
        <el-table-column :label="t('logisticsAnalytics.kpi.onHandAmountUsd')" width="140">
          <template #default="{ row }">{{ formatMoney(row.onHandAmountUsd) }}</template>
        </el-table-column>
        <el-table-column :label="t('logisticsAnalytics.kpi.weightedAvgAgeDays')" width="120">
          <template #default="{ row }">{{ formatAge(row.weightedAvgAgeDays) }}</template>
        </el-table-column>
      </el-table>
    </section>

    <div class="rankings-row">
      <div class="card ranking-panel">
        <AnalyticsPanelHeader
          :title="primaryRankingTitle"
          :unit-caption="maskAmounts ? undefined : t('logisticsAnalytics.unit.moneyCaption')"
          v-bind="def('rankings.primary')"
        />
        <el-table
          :data="dashboard?.rankings.primary ?? []"
          size="small"
          stripe
          class="ranking-table--drill"
          @row-click="onRankingRowClick"
        >
          <el-table-column prop="name" :label="t('logisticsAnalytics.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('logisticsAnalytics.rankings.qty')" width="100" />
          <el-table-column :label="t('logisticsAnalytics.rankings.amount')" width="140">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.logistics-analytics-page {
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

.chart-panel,
.ranking-panel,
.matrix-section {
  margin-bottom: 0;
}

.matrix-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;

  :deep(.panel-header) {
    margin-bottom: 0;
    flex: 1;
    min-width: 200px;
  }
}

.matrix-section :deep(th.matrix-col-qty-header .cell) {
  white-space: nowrap;
}

.ranking-table--drill :deep(.el-table__row) {
  cursor: pointer;
}
</style>
