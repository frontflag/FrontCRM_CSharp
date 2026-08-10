<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  salesAnalyticsApi,
  type SalesAnalyticsBreakdownGroup,
  type SalesAnalyticsCustomer,
  type SalesAnalyticsQuery,
  type SalesAnalyticsScopeContext
} from '@/api/analytics/sales'
import {
  customerListAnalyticsApi,
  type CustomerListAnalyticsQuery
} from '@/api/customerListAnalytics'
import { favoriteApi } from '@/api/favorite'
import { useAuthStore } from '@/stores/auth'
import { useCustomerDictStore } from '@/stores/customerDict'
import { buildCustomerRankingDrillRoute } from '@/utils/salesAnalyticsDrill'

const props = withDefaults(
  defineProps<{
    /** list=客户列表看板；report=销售分析客户 Tab */
    mode?: 'list' | 'report'
    query?: SalesAnalyticsQuery
    /** 父页已解析的 scope（用于下钻与 Mask；接口也会再返回一份） */
    scopeContext?: SalesAnalyticsScopeContext | null
    /** 列表看板筛选（mode=list） */
    filters?: CustomerListAnalyticsQuery
    active?: boolean
  }>(),
  {
    mode: 'report',
    active: true,
    scopeContext: null
  }
)

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const customerDict = useCustomerDictStore()

const loading = ref(false)
const loadedKey = ref('')
const data = ref<SalesAnalyticsCustomer | null>(null)

const isListMode = computed(() => props.mode === 'list')

const maskAmounts = computed(
  () => data.value?.scopeContext.maskAmounts === true || props.scopeContext?.maskAmounts === true
)

function formatMoney(v?: number | null): string {
  if (v == null) return '—'
  return `$\u00a0${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

const kpiItems = computed(() => {
  const s = data.value?.snapshot
  if (!s) return []
  return [
    {
      key: 'approvedCustomers',
      label: t('salesAnalytics.customerTab.kpi.approvedCustomers'),
      value: String(s.approvedCustomerCount)
    },
    {
      key: 'repeatCustomers',
      label: t('salesAnalytics.customerTab.kpi.repeatCustomers'),
      value: String(s.repeatCustomerCount)
    }
  ]
})

function reportQueryKey(q: SalesAnalyticsQuery | undefined): string {
  if (!q) return ''
  return [
    q.viewLevel ?? '',
    q.departmentId ?? '',
    q.salesUserId ?? '',
    q.dateFrom ?? '',
    q.dateTo ?? ''
  ].join('|')
}

function listFiltersKey(f: CustomerListAnalyticsQuery | undefined): string {
  if (!f) return ''
  return [
    f.searchTerm ?? '',
    f.customerType ?? '',
    f.customerLevel ?? '',
    f.industry ?? '',
    f.currency ?? '',
    f.region ?? '',
    f.salesUserId ?? '',
    f.createdFrom ?? '',
    f.createdTo ?? '',
    f.status ?? '',
    f.favoriteOnly ? '1' : '0',
    f.favoriteIds ?? '',
    f.quickFilter ?? ''
  ].join('|')
}

function currentKey(): string {
  return isListMode.value
    ? `list|${listFiltersKey(props.filters)}`
    : `report|${reportQueryKey(props.query)}`
}

function breakdownTitle(group: SalesAnalyticsBreakdownGroup): string {
  const key = `salesAnalytics.customerTab.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  if (maskAmounts.value) {
    return `${base}（${t('salesAnalytics.customerTab.breakdown.byCount')}）`
  }
  return base
}

function breakdownValueFormat(): 'money' | 'number' {
  return maskAmounts.value ? 'number' : 'money'
}

function localizedItems(group: SalesAnalyticsBreakdownGroup) {
  return group.items.map((item) => {
    let label = item.label
    if (group.groupKey === 'customerType' && item.key !== '_unset') {
      const typeNum = Number(item.key)
      if (Number.isFinite(typeNum)) label = customerDict.typeLabel(typeNum)
    } else if (group.groupKey === 'customerLevel' && item.key !== '_unset') {
      label = customerDict.levelLabel(item.key)
    } else if (group.groupKey === 'customerIndustry' && item.key !== '_unset') {
      label = customerDict.industryLabel(item.key)
    }
    return { ...item, label }
  })
}

function drillScope() {
  if (isListMode.value) {
    return {
      scopeContext: data.value?.scopeContext ?? props.scopeContext
    }
  }
  return {
    dateFrom: props.query?.dateFrom,
    dateTo: props.query?.dateTo,
    salesUserId: props.query?.salesUserId,
    scopeContext: data.value?.scopeContext ?? props.scopeContext
  }
}

function onCustomerRankingClick(row: { id: string; name: string }) {
  if (!authStore.hasPermission('sales-order.read')) {
    ElMessage.warning(t('salesAnalytics.drill.noPermission'))
    return
  }
  void router.push(buildCustomerRankingDrillRoute(row.id, row.name, drillScope()))
}

async function resolveListQuery(): Promise<CustomerListAnalyticsQuery> {
  const base = { ...(props.filters ?? {}) }
  if (!base.favoriteOnly) return base
  const ids = await favoriteApi.getFavoriteEntityIds('CUSTOMER')
  if (ids.length === 0) {
    base.favoriteIds = ''
    return base
  }
  base.favoriteIds = ids.join(',')
  return base
}

async function loadData() {
  if (!props.active) return
  const key = currentKey()
  loading.value = true
  try {
    await customerDict.ensureLoaded()
    if (isListMode.value) {
      const q = await resolveListQuery()
      data.value = await customerListAnalyticsApi.getCustomer(q)
    } else {
      data.value = await salesAnalyticsApi.getCustomer(props.query ?? {})
    }
    loadedKey.value = key
  } catch (e: unknown) {
    const msg = e instanceof Error
      ? e.message
      : isListMode.value
        ? t('customerList.board.loadFailed')
        : t('salesAnalytics.customerTab.loadFailed')
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
}

watch(
  () => [props.active, currentKey()] as const,
  ([active, key]) => {
    if (!active) return
    if (key === loadedKey.value && data.value) return
    void loadData()
  },
  { immediate: true }
)

defineExpose({ reload: loadData })
</script>

<template>
  <div class="sales-analytics-customer" v-loading="loading">
    <p class="metric-hint">
      {{ isListMode ? t('customerList.board.hint') : t('salesAnalytics.customerTab.hint') }}
    </p>

    <section class="section">
      <h3 class="section-title">{{ t('salesAnalytics.customerTab.sections.kpi') }}</h3>
      <AnalyticsKpiGrid :items="kpiItems" />
    </section>

    <div class="charts-row">
      <div v-for="group in data?.breakdowns ?? []" :key="group.groupKey" class="card chart-panel">
        <AnalyticsBreakdownPieChart
          :title="breakdownTitle(group)"
          :items="localizedItems(group)"
          :value-format="breakdownValueFormat()"
          :unit-caption="
            breakdownValueFormat() === 'money'
              ? t('salesAnalytics.trendUnit.moneyCaption')
              : undefined
          "
        />
      </div>
    </div>

    <div class="rankings-row">
      <div class="card ranking-panel">
        <div class="section-title-row">
          <h3 class="section-title">{{ t('salesAnalytics.customerTab.rankings.byAmount') }}</h3>
          <span v-if="!maskAmounts" class="unit-caption">{{ t('salesAnalytics.trendUnit.moneyCaption') }}</span>
        </div>
        <el-table
          :data="data?.rankings.customerByAmount ?? []"
          size="small"
          stripe
          class="ranking-table--drill"
          @row-click="onCustomerRankingClick"
        >
          <el-table-column prop="name" :label="t('salesAnalytics.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('salesAnalytics.rankings.orderCount')" width="90" />
          <el-table-column v-if="!maskAmounts" :label="t('salesAnalytics.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <div class="section-title-row">
          <h3 class="section-title">{{ t('salesAnalytics.customerTab.rankings.byOrderCount') }}</h3>
          <span v-if="!maskAmounts" class="unit-caption">{{ t('salesAnalytics.trendUnit.moneyCaption') }}</span>
        </div>
        <el-table
          :data="data?.rankings.customerByOrderCount ?? []"
          size="small"
          stripe
          class="ranking-table--drill"
          @row-click="onCustomerRankingClick"
        >
          <el-table-column prop="name" :label="t('salesAnalytics.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('salesAnalytics.rankings.orderCount')" width="90" />
          <el-table-column v-if="!maskAmounts" :label="t('salesAnalytics.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <div class="section-title-row">
          <h3 class="section-title">{{ t('salesAnalytics.customerTab.rankings.byRepeat') }}</h3>
          <span v-if="!maskAmounts" class="unit-caption">{{ t('salesAnalytics.trendUnit.moneyCaption') }}</span>
        </div>
        <el-table
          :data="data?.rankings.customerByRepeatOrderCount ?? []"
          size="small"
          stripe
          class="ranking-table--drill"
          @row-click="onCustomerRankingClick"
        >
          <el-table-column prop="name" :label="t('salesAnalytics.rankings.name')" />
          <el-table-column
            prop="orderCount"
            :label="t('salesAnalytics.customerTab.rankings.repeatOrders')"
            width="110"
          />
          <el-table-column v-if="!maskAmounts" :label="t('salesAnalytics.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.metric-hint {
  margin: 0 0 16px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.section {
  margin-bottom: 16px;
}

.section > .section-title {
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
}

.section-title-row .section-title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}

.unit-caption {
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  flex-shrink: 0;
}

.card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 16px;
}

.charts-row,
.rankings-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.chart-panel,
.ranking-panel {
  min-height: 240px;
}

.ranking-table--drill :deep(.el-table__row) {
  cursor: pointer;
}
</style>
