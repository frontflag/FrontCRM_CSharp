<template>
  <section class="overview" aria-labelledby="dashboard-overview-title">
    <header class="overview__head">
      <div>
        <h3 id="dashboard-overview-title" class="overview__title">{{ t('dashboard.overview.title') }}</h3>
        <p class="overview__meta">
          <span>{{ t('dashboard.overview.last30Days') }}</span>
          <template v-if="scopeText">
            <span class="overview__dot">·</span>
            <span>{{ scopeText }}</span>
          </template>
        </p>
      </div>
      <router-link v-if="analysisTo" class="overview__link" :to="analysisTo">
        {{ t('dashboard.overview.openAnalysis') }}
      </router-link>
    </header>

    <div v-loading="loading" class="overview__body">
      <div class="overview__kpis" :class="{ 'is-four': identityKind === 'logistics' }">
        <div v-for="item in kpis" :key="item.key" class="kpi">
          <span class="kpi__label">{{ item.label }}</span>
          <strong class="kpi__value" :class="{ 'is-money': item.money }">{{ item.value }}</strong>
          <span v-if="item.hint" class="kpi__hint">{{ item.hint }}</span>
        </div>
      </div>

      <div class="overview__trends">
        <div class="trend">
          <h4>{{ leftTrendTitle }}</h4>
          <DashboardSparkline
            :points="leftTrend"
            :value-prefix="t('dashboard.overview.tipItemPrefix')"
          />
        </div>
        <div class="trend">
          <h4>{{ rightTrendTitle }}</h4>
          <DashboardSparkline
            :points="rightTrend"
            :value-format="useCountSparkline ? 'number' : 'money'"
            :value-prefix="
              useCountSparkline ? t('dashboard.overview.tipItemPrefix') : t('dashboard.overview.tipUsdPrefix')
            "
            :masked="!useCountSparkline && moneyMasked"
          />
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores'
import { salesAnalyticsApi } from '@/api/analytics/sales'
import { purchaseAnalyticsApi } from '@/api/analytics/purchase'
import { logisticsAnalyticsApi } from '@/api/analytics/logistics'
import { inventoryCenterApi } from '@/api/inventoryCenter'
import { purchaseOrderItemListAnalyticsApi } from '@/api/purchaseOrderItemAnalytics'
import { financePaymentListAnalyticsApi } from '@/api/financePaymentAnalytics'
import { financeReceiptListAnalyticsApi } from '@/api/financeReceiptAnalytics'
import { dashboardOpsApi } from '@/api/dashboardOps'
import DashboardSparkline from './DashboardSparkline.vue'

const { t } = useI18n()
const authStore = useAuthStore()

const identityKind = computed(() => {
  const t0 = authStore.user?.identityType ?? 0
  if (t0 === 2 || t0 === 3) return 'purchase'
  if (t0 === 6) return 'logistics'
  if (t0 === 5) return 'finance'
  return 'sales'
})

const useCountSparkline = computed(
  () => identityKind.value === 'purchase' || identityKind.value === 'logistics' || identityKind.value === 'finance'
)

const canOpenSales = computed(
  () => authStore.hasPermission('analytics-sales.read') || authStore.hasPermission('sales-order.read')
)
const canOpenPurchase = computed(
  () =>
    authStore.hasPermission('analytics-purchase.read') || authStore.hasPermission('purchase-order.read')
)
const canOpenStock = computed(
  () =>
    authStore.hasPermission('analytics-logistics.read') ||
    authStore.hasPermission('inventory.read') ||
    authStore.hasPermission('purchase-order.read') ||
    authStore.hasPermission('sales-order.read')
)
const canOpenLogistics = computed(
  () =>
    authStore.hasPermission('analytics-logistics.read') ||
    authStore.hasPermission('inventory.read') ||
    authStore.hasPermission('purchase-order.read') ||
    authStore.hasPermission('sales-order.read')
)
const canOpenFinance = computed(
  () =>
    authStore.hasPermission('analytics-finance.read') ||
    authStore.hasPermission('finance-payment.read') ||
    authStore.hasPermission('finance-receipt.read') ||
    authStore.hasPermission('finance-purchase-invoice.read') ||
    authStore.hasPermission('finance-sell-invoice.read')
)
const canOpenPayment = computed(
  () => authStore.hasPermission('finance-payment.read') || authStore.hasPermission('purchase-order.read')
)
const canOpenReceipt = computed(() => authStore.hasPermission('finance-receipt.read'))

const analysisTo = computed(() => {
  const kind = identityKind.value
  if (kind === 'purchase') return canOpenPurchase.value ? { name: 'PurchaseAnalytics' } : null
  if (kind === 'logistics') return canOpenLogistics.value ? { name: 'LogisticsAnalytics' } : null
  if (kind === 'finance') return canOpenFinance.value ? { name: 'FinanceAnalytics' } : null
  return canOpenSales.value ? { name: 'SalesAnalytics' } : null
})

const loading = ref(false)
const scopeLabel = ref('')
const dash = '—'

const values = ref({
  countA: dash,
  countB: dash,
  rate: dash,
  stock: dash,
  money: dash,
  stockIn: dash,
  stockOut: dash,
  customs: dash,
  payment: dash,
  purchaseInvoiceWriteOff: dash,
  receipt: dash,
  receivableWriteOff: dash,
  sellInvoiceWriteOff: dash
})
const leftTrend = ref<{ period: string; value: number }[]>([])
const rightTrend = ref<{ period: string; value: number }[]>([])
const moneyMasked = ref(false)

const scopeText = computed(() => scopeLabel.value.trim())

type OverviewKpi = {
  key: string
  label: string
  value: string
  money?: boolean
  hint?: string
}

const kpis = computed((): OverviewKpi[] => {
  const kind = identityKind.value
  if (kind === 'purchase') {
    return [
      { key: 'quote', label: t('dashboard.overview.quote'), value: values.value.countA },
      { key: 'po', label: t('dashboard.overview.purchaseOrder'), value: values.value.countB },
      { key: 'rate', label: t('dashboard.overview.conversion'), value: values.value.rate },
      {
        key: 'stock',
        label: t('dashboard.overview.onHand'),
        value: values.value.stock,
        money: false,
        hint: t('dashboard.overview.stockItemCountHint')
      },
      {
        key: 'pay',
        label: t('dashboard.overview.payable'),
        value: values.value.money,
        money: false,
        hint: t('dashboard.overview.payableItemCountHint')
      }
    ]
  }
  if (kind === 'logistics') {
    return [
      {
        key: 'inventory',
        label: t('dashboard.overview.inventory'),
        value: values.value.stock,
        hint: t('dashboard.overview.stockItemCountHint')
      },
      {
        key: 'stockIn',
        label: t('dashboard.overview.stockIn'),
        value: values.value.stockIn,
        hint: t('dashboard.overview.last30DaysItemHint')
      },
      {
        key: 'stockOut',
        label: t('dashboard.overview.stockOut'),
        value: values.value.stockOut,
        hint: t('dashboard.overview.last30DaysItemHint')
      },
      {
        key: 'customs',
        label: t('dashboard.overview.customs'),
        value: values.value.customs,
        hint: t('dashboard.overview.last30DaysItemHint')
      }
    ]
  }
  if (kind === 'finance') {
    return [
      { key: 'payment', label: t('dashboard.overview.payment'), value: values.value.payment },
      {
        key: 'piWo',
        label: t('dashboard.overview.purchaseInvoiceWriteOff'),
        value: values.value.purchaseInvoiceWriteOff
      },
      { key: 'receipt', label: t('dashboard.overview.receipt'), value: values.value.receipt },
      {
        key: 'recvWo',
        label: t('dashboard.overview.receivableWriteOff'),
        value: values.value.receivableWriteOff
      },
      {
        key: 'siWo',
        label: t('dashboard.overview.sellInvoiceWriteOff'),
        value: values.value.sellInvoiceWriteOff
      }
    ]
  }
  return [
    { key: 'rfq', label: t('dashboard.overview.rfq'), value: values.value.countA },
    { key: 'so', label: t('dashboard.overview.salesOrder'), value: values.value.countB },
    { key: 'rate', label: t('dashboard.overview.conversion'), value: values.value.rate },
    {
      key: 'stock',
      label: t('dashboard.overview.onHand'),
      value: values.value.stock,
      money: values.value.stock !== dash,
      hint: t('dashboard.overview.asOfToday')
    },
    {
      key: 'recv',
      label: t('dashboard.overview.receivable'),
      value: values.value.money,
      money: values.value.money !== dash,
      hint: t('dashboard.overview.asOfToday')
    }
  ]
})

const leftTrendTitle = computed(() => {
  const kind = identityKind.value
  if (kind === 'purchase') return t('dashboard.overview.quoteTrend')
  if (kind === 'logistics') return t('dashboard.overview.stockInTrend')
  if (kind === 'finance') return t('dashboard.overview.paymentTrend')
  return t('dashboard.overview.rfqTrend')
})

const rightTrendTitle = computed(() => {
  const kind = identityKind.value
  if (kind === 'purchase') return t('dashboard.overview.purchaseTrend')
  if (kind === 'logistics') return t('dashboard.overview.stockOutTrend')
  if (kind === 'finance') return t('dashboard.overview.receiptTrend')
  return t('dashboard.overview.dealTrend')
})

function shanghaiToday() {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(new Date())
}

function addDaysYmd(ymd: string, days: number) {
  const [y, m, d] = ymd.split('-').map(Number)
  const dt = new Date(Date.UTC(y, m - 1, d + days))
  return `${dt.getUTCFullYear()}-${String(dt.getUTCMonth() + 1).padStart(2, '0')}-${String(dt.getUTCDate()).padStart(2, '0')}`
}

function last30Range() {
  const to = shanghaiToday()
  return { dateFrom: addDaysYmd(to, -29), dateTo: to }
}

function formatCount(n: number | null | undefined) {
  if (n == null || !Number.isFinite(n)) return dash
  return n.toLocaleString('zh-CN')
}

function formatRate(n: number | null | undefined) {
  if (n == null || !Number.isFinite(n)) return dash
  return `${n.toFixed(2)}%`
}

function formatUsd(n: number | null | undefined, masked?: boolean) {
  if (masked) return dash
  if (n == null || !Number.isFinite(n)) return dash
  return `$\u00a0${n.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function emptyValues() {
  return {
    countA: dash,
    countB: dash,
    rate: dash,
    stock: dash,
    money: dash,
    stockIn: dash,
    stockOut: dash,
    customs: dash,
    payment: dash,
    purchaseInvoiceWriteOff: dash,
    receipt: dash,
    receivableWriteOff: dash,
    sellInvoiceWriteOff: dash
  }
}

/** 与库存明细列表默认筛选「共 N 条」同一口径（数据范围内全部明细行，含已出清）。 */
async function loadStockItemCount(): Promise<string> {
  try {
    const res = await inventoryCenterApi.searchStockItems({ page: 1, pageSize: 1 })
    return formatCount(res.total)
  } catch {
    return dash
  }
}

async function loadStockUsd(): Promise<string> {
  if (!canOpenStock.value) return dash
  try {
    const logi = await logisticsAnalyticsApi.getDashboard({ inventoryType: 'all' })
    const masked = logi.scopeContext?.maskAmounts === true
    return formatUsd(logi.snapshot?.onHandAmountUsd, masked)
  } catch {
    return dash
  }
}

/** 采购桌面：与采购订单明细看板「应付款明细数」同一口径（未付清行，默认筛选）。 */
async function loadPurchasePayableItemCount(): Promise<string> {
  if (!canOpenPurchase.value) return dash
  try {
    const res = await purchaseOrderItemListAnalyticsApi.getDashboard({})
    return formatCount(res.snapshot?.payableLineCount)
  } catch {
    return dash
  }
}

async function loadSales() {
  if (!canOpenSales.value) return
  const range = last30Range()
  const [dashRes, trends] = await Promise.all([
    salesAnalyticsApi.getDashboard(range),
    salesAnalyticsApi.getTrends({ ...range, groupBy: 'day' })
  ])
  scopeLabel.value = dashRes.scopeContext?.scopeLabel?.trim() || ''
  const mask = dashRes.scopeContext?.maskAmounts === true
  moneyMasked.value = mask
  values.value.countA = formatCount(dashRes.snapshot?.rfqItemCount)
  values.value.countB = formatCount(dashRes.snapshot?.salesOrderItemCount)
  values.value.rate = formatRate(dashRes.snapshot?.rfqToSalesConversionRate)
  values.value.money = formatUsd(dashRes.todo?.receivableAmount?.totalUsd, mask)
  leftTrend.value = (trends ?? []).map((p) => ({ period: p.period, value: p.rfqItemCount ?? 0 }))
  rightTrend.value = (trends ?? []).map((p) => ({
    period: p.period,
    value: mask ? 0 : (p.salesAmountApproved ?? 0)
  }))
}

async function loadPurchase() {
  if (!canOpenPurchase.value) return
  const range = last30Range()
  const [dashRes, trends] = await Promise.all([
    purchaseAnalyticsApi.getDashboard(range),
    purchaseAnalyticsApi.getTrends({ ...range, groupBy: 'day' })
  ])
  scopeLabel.value = dashRes.scopeContext?.scopeLabel?.trim() || ''
  const mask = dashRes.scopeContext?.maskAmounts === true
  moneyMasked.value = mask
  values.value.countA = formatCount(dashRes.snapshot?.quoteItemCount)
  values.value.countB = formatCount(dashRes.snapshot?.purchaseOrderItemCount)
  values.value.rate = formatRate(dashRes.snapshot?.quoteToPurchaseConversionRate)
  leftTrend.value = (trends ?? []).map((p) => ({ period: p.period, value: p.quoteItemCount ?? 0 }))
  rightTrend.value = (trends ?? []).map((p) => ({
    period: p.period,
    value: p.purchaseOrderItemCount ?? 0
  }))
}

async function loadLogistics() {
  if (!canOpenLogistics.value) {
    values.value.stock = await loadStockItemCount()
    return
  }
  const range = last30Range()
  const [ops, stock] = await Promise.all([
    dashboardOpsApi.getLogistics(range).catch(() => null),
    loadStockItemCount()
  ])
  values.value.stock = stock
  if (!ops) return
  values.value.stockIn = formatCount(ops.stockInItemCount)
  values.value.stockOut = formatCount(ops.stockOutItemCount)
  values.value.customs = formatCount(ops.customsDeclarationItemCount)
  leftTrend.value = (ops.stockInTrends ?? []).map((p) => ({ period: p.period, value: p.count ?? 0 }))
  rightTrend.value = (ops.stockOutTrends ?? []).map((p) => ({ period: p.period, value: p.count ?? 0 }))
}

async function loadFinance() {
  const range = last30Range()
  const paymentP = canOpenPayment.value
    ? Promise.all([
        financePaymentListAnalyticsApi.getDashboard({ startDate: range.dateFrom, endDate: range.dateTo }),
        financePaymentListAnalyticsApi.getTrends({
          startDate: range.dateFrom,
          endDate: range.dateTo,
          groupBy: 'day'
        })
      ]).catch(() => null)
    : Promise.resolve(null)
  const receiptP = canOpenReceipt.value
    ? Promise.all([
        financeReceiptListAnalyticsApi.getDashboard({
          receiptDateFrom: range.dateFrom,
          receiptDateTo: range.dateTo
        }),
        financeReceiptListAnalyticsApi.getTrends({
          receiptDateFrom: range.dateFrom,
          receiptDateTo: range.dateTo,
          groupBy: 'day'
        })
      ]).catch(() => null)
    : Promise.resolve(null)
  const writeOffP = canOpenFinance.value
    ? dashboardOpsApi.getFinanceWriteOffs(range).catch(() => null)
    : Promise.resolve(null)

  const [pay, rec, wo] = await Promise.all([paymentP, receiptP, writeOffP])
  if (pay) {
    values.value.payment = formatCount(pay[0].snapshot?.headerCount)
    leftTrend.value = (pay[1] ?? []).map((p) => ({ period: p.period, value: p.headerCount ?? 0 }))
  }
  if (rec) {
    values.value.receipt = formatCount(rec[0].snapshot?.headerCount)
    rightTrend.value = (rec[1] ?? []).map((p) => ({ period: p.period, value: p.headerCount ?? 0 }))
  }
  if (wo) {
    values.value.purchaseInvoiceWriteOff = formatCount(wo.purchaseInvoiceWriteOffCount)
    values.value.receivableWriteOff = formatCount(wo.receivableWriteOffCount)
    values.value.sellInvoiceWriteOff = formatCount(wo.sellInvoiceWriteOffCount)
  }
}

async function load() {
  loading.value = true
  values.value = emptyValues()
  leftTrend.value = []
  rightTrend.value = []
  moneyMasked.value = false
  scopeLabel.value = ''
  try {
    const kind = identityKind.value
    if (kind === 'purchase') {
      const stockP = loadStockItemCount()
      const payableP = loadPurchasePayableItemCount()
      await loadPurchase()
      values.value.stock = await stockP
      values.value.money = await payableP
    } else if (kind === 'logistics') {
      await loadLogistics()
    } else if (kind === 'finance') {
      await loadFinance()
    } else {
      const stockP = loadStockUsd()
      await loadSales()
      values.value.stock = await stockP
    }
  } catch {
    /* 格子保持 — */
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style lang="scss" scoped>
.overview {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  padding: 16px 20px 18px;
  overflow: visible;
}

.overview__body {
  overflow: visible;
}

.overview__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
}

.overview__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}

.overview__meta {
  margin: 4px 0 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.overview__dot {
  margin: 0 4px;
}

.overview__link {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-color-primary);
  text-decoration: none;
  &:hover {
    text-decoration: underline;
  }
}

.overview__kpis {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 12px 16px;
  &.is-four {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
  @media (max-width: 900px) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

.kpi {
  min-width: 0;
}

.kpi__label {
  display: block;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.kpi__value {
  display: block;
  margin-top: 6px;
  font-size: 22px;
  font-weight: 700;
  line-height: 1.2;
  letter-spacing: -0.02em;
  &.is-money {
    font-size: 16px;
  }
}

.kpi__hint {
  display: block;
  margin-top: 4px;
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.overview__trends {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  margin-top: 18px;
  padding-top: 16px;
  border-top: 1px solid var(--el-border-color-lighter);
  @media (max-width: 720px) {
    grid-template-columns: 1fr;
  }
}

.trend {
  position: relative;
  overflow: visible;
}

.trend h4 {
  margin: 0 0 8px;
  font-size: 13px;
  font-weight: 600;
}
</style>
