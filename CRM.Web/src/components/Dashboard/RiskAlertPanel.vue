<template>
  <section class="risk" aria-labelledby="dashboard-risk-title">
    <header class="risk__head">
      <div>
        <h3 id="dashboard-risk-title" class="risk__title">{{ t('dashboard.risk.title') }}</h3>
        <p class="risk__meta">
          <template v-if="!loading && data">
            <span v-if="data.immediateCount > 0" class="risk__urgent">
              {{ t('dashboard.risk.immediate', { n: data.immediateCount }) }}
            </span>
            <span v-else-if="data.anyEnabled">{{ t('dashboard.risk.none') }}</span>
            <span v-else>{{ t('dashboard.risk.unconfigured') }}</span>
          </template>
        </p>
      </div>
      <router-link v-if="canOpenParams" class="risk__link" to="/system/risk-alert-params">
        {{ t('dashboard.risk.openParams') }}
      </router-link>
    </header>

    <div v-loading="loading" class="risk__body">
      <p v-if="!loading && displayRows.length === 0" class="risk__empty">
        {{ data?.anyEnabled ? t('dashboard.risk.none') : t('dashboard.risk.unconfigured') }}
      </p>
      <button
        v-for="row in displayRows"
        :key="row.code"
        type="button"
        class="risk-row"
        :disabled="!row.to"
        @click="go(row)"
      >
        <span class="risk-row__dot" aria-hidden="true" />
        <div class="risk-row__body">
          <span class="risk-row__title">{{ titleOf(row.code) }}</span>
          <span class="risk-row__detail">{{ detailOf(row) }}</span>
        </div>
      </button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, type RouteLocationRaw } from 'vue-router'
import { useAuthStore } from '@/stores'
import { riskAlertApi, type RiskAlertDashboard, type RiskAlertItem, type RiskAlertItemCode } from '@/api/riskAlert'

const { t } = useI18n()
const authStore = useAuthStore()
const router = useRouter()

const canOpenParams = computed(
  () =>
    authStore.canForceDelete() &&
    authStore.canAccessSystemPermission('system.params.risk-alert.read')
)

const loading = ref(false)
const data = ref<RiskAlertDashboard | null>(null)

const canReceivable = computed(
  () => authStore.hasPermission('finance-receipt.read') || authStore.hasPermission('sales-order.read')
)

function titleOf(code: RiskAlertItemCode) {
  const map: Record<RiskAlertItemCode, string> = {
    'inventory-amount': t('dashboard.risk.inventoryAmount'),
    'stock-age': t('dashboard.risk.stockAge'),
    'receivable-amount': t('dashboard.risk.receivableAmount'),
    'customer-receivable': t('dashboard.risk.customerReceivable'),
    'so-receivable-age': t('dashboard.risk.soReceivableAge')
  }
  return map[code]
}

function money(v: number | null | undefined, masked: boolean) {
  if (masked || v == null) return t('dashboard.risk.masked')
  return `$${v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function detailOf(row: RiskAlertItem) {
  if (row.code === 'inventory-amount' || row.code === 'receivable-amount') {
    return t('dashboard.risk.vsThreshold', {
      actual: money(row.actualUsd, row.masked),
      threshold: money(row.thresholdUsd, false)
    })
  }
  if (row.code === 'stock-age') {
    return t('dashboard.risk.ageDetail', {
      count: row.hitCount ?? 0,
      days: row.actualDays ?? 0
    })
  }
  if (row.code === 'customer-receivable') {
    return t('dashboard.risk.customerDetail', {
      name: row.masked ? t('dashboard.risk.masked') : row.subjectName || t('dashboard.risk.unknownSubject'),
      actual: money(row.actualUsd, row.masked),
      threshold: money(row.thresholdUsd, false)
    })
  }
  return t('dashboard.risk.soDetail', {
    code: row.subjectCode || t('dashboard.risk.unknownSubject'),
    days: row.actualDays ?? 0
  })
}

function drillTo(row: RiskAlertItem): RouteLocationRaw | null {
  if (row.code === 'inventory-amount' || row.code === 'stock-age') {
    if (authStore.hasPermission('inventory.read')) return { path: '/inventory/stock-items' }
    if (authStore.hasPermission('analytics-logistics.read')) return { name: 'LogisticsAnalytics' }
    return null
  }
  if (row.code === 'so-receivable-age' && row.subjectId && authStore.hasPermission('sales-order.read')) {
    return { path: `/sales-orders/${row.subjectId}` }
  }
  if (canReceivable.value) return { path: '/finance/receivables' }
  return null
}

const displayRows = computed(() =>
  (data.value?.items ?? []).map((row) => ({
    ...row,
    to: drillTo(row)
  }))
)

function go(row: RiskAlertItem & { to?: RouteLocationRaw | null }) {
  if (!row.to) return
  void router.push(row.to)
}

onMounted(async () => {
  loading.value = true
  try {
    data.value = await riskAlertApi.getDashboard()
  } catch {
    data.value = { immediateCount: 0, anyEnabled: false, items: [] }
  } finally {
    loading.value = false
  }
})
</script>

<style lang="scss" scoped>
.risk {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  padding: 16px 20px 14px;
  margin-top: 16px;
}

.risk__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}

.risk__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}

.risk__meta {
  margin: 4px 0 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.risk__urgent {
  color: #d9480f;
  font-weight: 600;
}

.risk__link {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-color-primary);
  text-decoration: none;
  &:hover {
    text-decoration: underline;
  }
}

.risk__empty {
  margin: 0;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.risk-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  width: 100%;
  padding: 8px 0;
  border: 0;
  background: transparent;
  text-align: left;
  cursor: pointer;
  & + & {
    border-top: 1px solid var(--el-border-color-extra-light);
  }
  &:disabled {
    cursor: default;
  }
}

.risk-row__dot {
  width: 8px;
  height: 8px;
  margin-top: 5px;
  border-radius: 50%;
  background: #f76707;
  flex-shrink: 0;
}

.risk-row__title {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.risk-row__detail {
  display: block;
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
