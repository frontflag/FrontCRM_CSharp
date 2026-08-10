<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import AnalyticsKpiGrid from '@/components/Analytics/AnalyticsKpiGrid.vue'
import AnalyticsBreakdownPieChart from '@/components/Analytics/AnalyticsBreakdownPieChart.vue'
import {
  purchaseAnalyticsApi,
  type PurchaseAnalyticsBreakdownGroup,
  type PurchaseAnalyticsQuery,
  type PurchaseAnalyticsScopeContext,
  type PurchaseAnalyticsVendor
} from '@/api/analytics/purchase'
import {
  vendorListAnalyticsApi,
  type VendorListAnalyticsQuery
} from '@/api/vendorListAnalytics'
import { favoriteApi } from '@/api/favorite'
import { useAuthStore } from '@/stores/auth'
import { useVendorDictStore } from '@/stores/vendorDict'
import { buildVendorRankingDrillRoute } from '@/utils/purchaseAnalyticsDrill'

const props = withDefaults(
  defineProps<{
    /** list=供应商列表看板；report=采购分析供应商 Tab */
    mode?: 'list' | 'report'
    query?: PurchaseAnalyticsQuery
    scopeContext?: PurchaseAnalyticsScopeContext | null
    filters?: VendorListAnalyticsQuery
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
const vendorDict = useVendorDictStore()

const loading = ref(false)
const loadedKey = ref('')
const data = ref<PurchaseAnalyticsVendor | null>(null)

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
      key: 'approvedVendors',
      label: t('purchaseAnalytics.vendorTab.kpi.approvedVendors'),
      value: String(s.approvedVendorCount)
    },
    {
      key: 'repeatVendors',
      label: t('purchaseAnalytics.vendorTab.kpi.repeatVendors'),
      value: String(s.repeatVendorCount)
    }
  ]
})

function reportQueryKey(q: PurchaseAnalyticsQuery | undefined): string {
  if (!q) return ''
  return [
    q.viewLevel ?? '',
    q.departmentId ?? '',
    q.purchaseUserId ?? '',
    q.dateFrom ?? '',
    q.dateTo ?? ''
  ].join('|')
}

function listFiltersKey(f: VendorListAnalyticsQuery | undefined): string {
  if (!f) return ''
  return [
    f.searchTerm ?? '',
    f.status ?? '',
    f.level ?? '',
    f.industry ?? '',
    f.currency ?? '',
    f.credit ?? '',
    f.ascriptionType ?? '',
    f.purchaseUserId ?? '',
    f.createdFrom ?? '',
    f.createdTo ?? '',
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

function breakdownTitle(group: PurchaseAnalyticsBreakdownGroup): string {
  const key = `purchaseAnalytics.vendorTab.breakdown.${group.groupKey}`
  const translated = t(key)
  const base = translated !== key ? translated : group.groupLabel
  if (maskAmounts.value) {
    return `${base}（${t('purchaseAnalytics.vendorTab.breakdown.byCount')}）`
  }
  return base
}

function breakdownValueFormat(): 'money' | 'number' {
  return maskAmounts.value ? 'number' : 'money'
}

function localizedItems(group: PurchaseAnalyticsBreakdownGroup) {
  return group.items.map((item) => {
    let label = item.label
    if (group.groupKey === 'vendorCredit' && item.key !== '_unset') {
      const idNum = Number(item.key)
      if (Number.isFinite(idNum)) label = vendorDict.identityLabel(idNum)
    } else if (group.groupKey === 'vendorLevel' && item.key !== '_unset') {
      const levelNum = Number(item.key)
      if (Number.isFinite(levelNum)) label = vendorDict.levelLabel(levelNum)
    } else if (group.groupKey === 'vendorIndustry' && item.key !== '_unset') {
      label = vendorDict.industryLabel(item.key)
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
    purchaseUserId: props.query?.purchaseUserId,
    scopeContext: data.value?.scopeContext ?? props.scopeContext
  }
}

function onVendorRankingClick(row: { id: string; name: string }) {
  if (!authStore.hasPermission('purchase-order.read')) {
    ElMessage.warning(t('purchaseAnalytics.drill.noPermission'))
    return
  }
  void router.push(buildVendorRankingDrillRoute(row.id, row.name, drillScope()))
}

async function resolveListQuery(): Promise<VendorListAnalyticsQuery> {
  const base = { ...(props.filters ?? {}) }
  if (!base.favoriteOnly) return base
  const ids = await favoriteApi.getFavoriteEntityIds('VENDOR')
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
    await vendorDict.ensureLoaded()
    if (isListMode.value) {
      const q = await resolveListQuery()
      data.value = await vendorListAnalyticsApi.getVendor(q)
    } else {
      data.value = await purchaseAnalyticsApi.getVendor(props.query ?? {})
    }
    loadedKey.value = key
  } catch (e: unknown) {
    const msg = e instanceof Error
      ? e.message
      : isListMode.value
        ? t('vendorList.board.loadFailed')
        : t('purchaseAnalytics.vendorTab.loadFailed')
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
  <div class="purchase-analytics-vendor" v-loading="loading">
    <p class="metric-hint">
      {{ isListMode ? t('vendorList.board.hint') : t('purchaseAnalytics.vendorTab.hint') }}
    </p>

    <section class="section">
      <h3 class="section-title">{{ t('purchaseAnalytics.vendorTab.sections.kpi') }}</h3>
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
              ? t('purchaseAnalytics.trendUnit.moneyCaption')
              : undefined
          "
        />
      </div>
    </div>

    <div class="rankings-row">
      <div class="card ranking-panel">
        <div class="section-title-row">
          <h3 class="section-title">{{ t('purchaseAnalytics.vendorTab.rankings.byAmount') }}</h3>
          <span v-if="!maskAmounts" class="unit-caption">{{ t('purchaseAnalytics.trendUnit.moneyCaption') }}</span>
        </div>
        <el-table
          :data="data?.rankings.vendorByAmount ?? []"
          size="small"
          stripe
          class="ranking-table--drill"
          @row-click="onVendorRankingClick"
        >
          <el-table-column prop="name" :label="t('purchaseAnalytics.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('purchaseAnalytics.rankings.orderCount')" width="90" />
          <el-table-column v-if="!maskAmounts" :label="t('purchaseAnalytics.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <div class="section-title-row">
          <h3 class="section-title">{{ t('purchaseAnalytics.vendorTab.rankings.byOrderCount') }}</h3>
          <span v-if="!maskAmounts" class="unit-caption">{{ t('purchaseAnalytics.trendUnit.moneyCaption') }}</span>
        </div>
        <el-table
          :data="data?.rankings.vendorByOrderCount ?? []"
          size="small"
          stripe
          class="ranking-table--drill"
          @row-click="onVendorRankingClick"
        >
          <el-table-column prop="name" :label="t('purchaseAnalytics.rankings.name')" />
          <el-table-column prop="orderCount" :label="t('purchaseAnalytics.rankings.orderCount')" width="90" />
          <el-table-column v-if="!maskAmounts" :label="t('purchaseAnalytics.rankings.amount')" width="160">
            <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <div class="card ranking-panel">
        <div class="section-title-row">
          <h3 class="section-title">{{ t('purchaseAnalytics.vendorTab.rankings.byRepeat') }}</h3>
          <span v-if="!maskAmounts" class="unit-caption">{{ t('purchaseAnalytics.trendUnit.moneyCaption') }}</span>
        </div>
        <el-table
          :data="data?.rankings.vendorByRepeatOrderCount ?? []"
          size="small"
          stripe
          class="ranking-table--drill"
          @row-click="onVendorRankingClick"
        >
          <el-table-column prop="name" :label="t('purchaseAnalytics.rankings.name')" />
          <el-table-column
            prop="orderCount"
            :label="t('purchaseAnalytics.vendorTab.rankings.repeatOrders')"
            width="110"
          />
          <el-table-column v-if="!maskAmounts" :label="t('purchaseAnalytics.rankings.amount')" width="160">
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
