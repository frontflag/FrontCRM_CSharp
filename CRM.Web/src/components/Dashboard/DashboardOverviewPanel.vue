<template>
  <section class="overview" aria-labelledby="dashboard-overview-title">
    <header class="overview__head">
      <div>
        <h3 id="dashboard-overview-title" class="overview__title">{{ t('dashboard.overview.title') }}</h3>
        <p class="overview__meta">
          <span>{{ t('dashboard.overview.last30Days') }}</span>
          <span class="overview__dot">·</span>
          <span>{{ scopeText }}</span>
        </p>
      </div>
      <router-link v-if="analysisTo" class="overview__link" :to="analysisTo">
        {{ t('dashboard.overview.openAnalysis') }}
      </router-link>
    </header>

    <div v-loading="loading" class="overview__body">
      <div class="overview__kpis">
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
            value-format="money"
            :value-prefix="t('dashboard.overview.tipUsdPrefix')"
            :masked="moneyMasked"
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
import DashboardSparkline from './DashboardSparkline.vue'

const { t } = useI18n()
const authStore = useAuthStore()

const isPurchasePrimary = computed(() => {
  const t0 = authStore.user?.identityType ?? 0
  return t0 === 2 || t0 === 3
})

const canOpenSales = computed(
  () => authStore.hasPermission('analytics-sales.read') || authStore.hasPermission('sales-order.read')
)
const canOpenPurchase = computed(
  () =>
    authStore.hasPermission('analytics-purchase.read') || authStore.hasPermission('purchase-order.read')
)
const canOpenStock = computed(
  () => authStore.hasPermission('analytics-logistics.read') || authStore.hasPermission('inventory.read')
)

const analysisTo = computed(() => {
  if (isPurchasePrimary.value) return canOpenPurchase.value ? { name: 'PurchaseAnalytics' } : null
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
  money: dash
})
const leftTrend = ref<{ period: string; value: number }[]>([])
const rightTrend = ref<{ period: string; value: number }[]>([])
const moneyMasked = ref(false)

const scopeText = computed(() => scopeLabel.value || t('dashboard.overview.scopeUnknown'))

const kpis = computed(() => {
  if (isPurchasePrimary.value) {
    return [
      { key: 'quote', label: t('dashboard.overview.quote'), value: values.value.countA },
      { key: 'po', label: t('dashboard.overview.purchaseOrder'), value: values.value.countB },
      { key: 'rate', label: t('dashboard.overview.conversion'), value: values.value.rate },
      {
        key: 'stock',
        label: t('dashboard.overview.onHand'),
        value: values.value.stock,
        money: values.value.stock !== dash,
        hint: t('dashboard.overview.asOfToday')
      },
      {
        key: 'pay',
        label: t('dashboard.overview.payable'),
        value: values.value.money,
        money: values.value.money !== dash,
        hint: t('dashboard.overview.asOfToday')
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

const leftTrendTitle = computed(() =>
  isPurchasePrimary.value ? t('dashboard.overview.quoteTrend') : t('dashboard.overview.rfqTrend')
)
const rightTrendTitle = computed(() => t('dashboard.overview.dealTrend'))

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

async function loadStockUsd(): Promise<string> {
  if (!canOpenStock.value) return dash
  try {
    const logi = await logisticsAnalyticsApi.getDashboard({ inventoryType: 'all' })
    const masked = logi.scopeContext?.maskAmounts === true || logi.scopeContext?.maskSalesAmounts === true
    return formatUsd(logi.snapshot?.onHandAmountUsd, masked)
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
  values.value.money = formatUsd(dashRes.todo?.payableAmount, mask)
  leftTrend.value = (trends ?? []).map((p) => ({ period: p.period, value: p.quoteItemCount ?? 0 }))
  rightTrend.value = (trends ?? []).map((p) => ({
    period: p.period,
    value: mask ? 0 : (p.purchaseAmountApproved ?? 0)
  }))
}

async function load() {
  loading.value = true
  values.value = { countA: dash, countB: dash, rate: dash, stock: dash, money: dash }
  leftTrend.value = []
  rightTrend.value = []
  moneyMasked.value = false
  scopeLabel.value = ''
  try {
    const stockP = loadStockUsd()
    if (isPurchasePrimary.value) await loadPurchase()
    else await loadSales()
    values.value.stock = await stockP
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
