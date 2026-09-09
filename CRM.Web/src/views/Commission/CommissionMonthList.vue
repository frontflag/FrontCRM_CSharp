<template>
  <div class="crm-biz-list-page">
    <div class="page-header">
      <div class="header-left">
        <button type="button" class="btn-ghost btn-sm" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('commissionResult.backSummary') }}
        </button>
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M12 3v18" />
              <path d="M5 10h14" />
              <path d="M7 21h10a2 2 0 0 0 2-2V8L16 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2z" />
            </svg>
          </div>
          <h1 class="page-title">{{ pageTitle }}</h1>
        </div>
        <div class="count-badge">{{ personLabel }}</div>
        <div class="count-badge">{{ t('commissionResult.count', { count: total }) }}</div>
      </div>
    </div>

    <div v-if="!official" class="statistics-row">
      <div class="stat-card">
        <div class="stat-value">{{ totalLineCount }}</div>
        <div class="stat-label">{{ t('commissionResult.lineCount') }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ formatCommissionMoney(totalCommissionUsd) }}</div>
        <div class="stat-label">{{ t('commissionResult.estimatedAmount') }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="keyword"
            class="search-input"
            :placeholder="official ? t('commissionResult.monthKeywordPlaceholder') : t('commissionResult.statsMonthPlaceholder')"
            @keyup.enter="reload"
          />
        </div>
        <el-select
          v-if="official"
          v-model="term"
          class="status-select status-select--wide"
          :placeholder="t('commissionResult.term')"
          :teleported="false"
          @change="reload"
        >
          <el-option v-for="item in terms" :key="item.term" :label="item.label" :value="item.term" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="reload">{{ t('commissionResult.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetSearch">{{ t('commissionResult.reset') }}</button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        :column-layout-key="columnKey"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-key="rowKey"
        highlight-current-row
        @row-dblclick="onRowDblclick"
      >
        <template #col-userName="{ row }">
          <span class="dock-quote-tier-line">{{ row.userName || userName || '—' }}</span>
        </template>
        <template #col-calcMonth="{ row }">
          <span class="dock-quote-tier-line">{{ row.calcMonth || '—' }}</span>
        </template>
        <template #col-userLevel="{ row }">
          <span class="dock-quote-tier-line">{{ row.userLevel }}</span>
        </template>
        <template #col-qualifyGpUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.qualifyGpUsd" :currency="usdCurrency" />
        </template>
        <template #col-lineCount="{ row }">
          <span class="dock-quote-tier-line">{{ row.lineCount }}</span>
        </template>
        <template #col-monthGpUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.monthGpUsd" :currency="usdCurrency" />
        </template>
        <template #col-commissionUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.commissionUsd" :currency="usdCurrency" />
        </template>
      </CrmDataTable>
      <div v-show="!loading && rows.length === 0" class="empty-state">
        <p>{{ t('commissionResult.monthEmpty') }}</p>
      </div>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="load"
        @size-change="onPageSizeChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import CrmDataTable from '@/components/CrmDataTable.vue'
import ReceivableStatementMoneyCell from '@/views/Finance/ReceivableStatementMoneyCell.vue'
import { CurrencyCode } from '@/constants/currency'
import {
  COMMISSION_ROLE_PURCHASE,
  commissionApi,
  formatCommissionMoney,
  formatCommissionPoints,
  type CommissionMonthRow,
  type CommissionTermOption
} from '@/api/commission'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

type MonthTableRow = CommissionMonthRow & { rowKey: string }

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const official = computed(() => route.meta.commissionMode === 'official')
const roleType = computed(() => (Number(route.meta.commissionRoleType) === 2 ? 2 : 1))
const isPurchase = computed(() => roleType.value === COMMISSION_ROLE_PURCHASE)
const userId = computed(() => String(route.params.userId ?? '').trim())
const usdCurrency = CurrencyCode.USD

const pageTitle = computed(() => {
  if (official.value) {
    return isPurchase.value
      ? t('commissionResult.monthListOfficialPurchase')
      : t('commissionResult.monthListOfficialSales')
  }
  return isPurchase.value
    ? t('commissionResult.monthListPurchase')
    : t('commissionResult.monthListSales')
})

const userName = ref(String(route.query.userName ?? '').trim())
const userLevel = ref<number | null>(null)

const personLabel = computed(() => {
  const name = userName.value || userId.value || '—'
  if (userLevel.value == null) return name
  return `${name} · ${t('commissionResult.userLevel')} ${userLevel.value}`
})

const columnKey = computed(
  () => `commission-month-${official.value ? 'official' : 'estimated'}-${roleType.value}-v5`
)

const loading = ref(false)
const keyword = ref('')
const term = ref(String(route.query.term ?? '').trim())
const defaultTerm = ref(String(route.query.term ?? '').trim())
const terms = ref<CommissionTermOption[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const totalLineCount = ref(0)
const totalCommissionUsd = ref(0)
const rows = ref<MonthTableRow[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

function headerMin(label: string) {
  return estimateListColumnHeaderMinWidth(label)
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const account = official.value ? t('commissionResult.userAccount') : t('commissionResult.commissionUser')
  const month = official.value ? t('commissionResult.calcMonth') : t('commissionResult.statsMonth')
  const level = t('commissionResult.levelShort')
  const qualifyGp = t('commissionResult.qualifyGp')
  const qualified = t('commissionResult.qualified')
  const gp = t('commissionResult.monthGp')
  const points = t('commissionResult.pointsFull')
  const amount = official.value ? t('commissionResult.officialAmount') : t('commissionResult.estimatedAmount')
  const count = t('commissionResult.lineCount')
  const accountW = Math.max(160, headerMin(account))
  const monthW = Math.max(120, headerMin(month))
  const qualifyW = Math.max(160, headerMin(qualifyGp))
  const gpW = Math.max(160, headerMin(gp))
  const amountW = Math.max(160, headerMin(amount))
  const cols: CrmTableColumnDef[] = [
    { key: 'calcMonth', label: month, prop: 'calcMonth', width: monthW, align: 'center' },
    {
      key: 'userName',
      label: account,
      prop: 'userName',
      width: accountW,
      minWidth: accountW,
      showOverflowTooltip: true
    }
  ]
  if (!official.value) {
    cols.push(
      { key: 'userLevel', label: level, width: Math.max(72, headerMin(level)), align: 'center', prop: 'userLevel' },
      { key: 'qualifyGpUsd', label: qualifyGp, width: qualifyW, minWidth: qualifyW, align: 'right' }
    )
  }
  cols.push(
    {
      key: 'qualified',
      label: qualified,
      width: Math.max(88, headerMin(qualified)),
      align: 'center',
      formatter: (row: unknown) =>
        (row as MonthTableRow).qualified ? t('commissionResult.yes') : t('commissionResult.no')
    },
    { key: 'monthGpUsd', label: gp, width: gpW, minWidth: gpW, align: 'right' },
    {
      key: 'ratePoints',
      label: points,
      width: Math.max(96, headerMin(points)),
      align: 'right',
      formatter: (row: unknown) => formatCommissionPoints((row as MonthTableRow).ratePoints)
    },
    { key: 'commissionUsd', label: amount, width: amountW, minWidth: amountW, align: 'right' },
    { key: 'lineCount', label: count, width: Math.max(120, headerMin(count)), align: 'right' },
    {
      key: 'flexGutter',
      label: '',
      minWidth: 48,
      hideable: false,
      reorderable: false,
      pinned: 'end',
      resizable: false,
      className: 'crm-list-flex-col',
      labelClassName: 'crm-list-flex-col'
    }
  )
  return cols
})

async function bootstrapOfficialTerms() {
  terms.value = await commissionApi.officialTerms(roleType.value)
  if (!term.value && terms.value.length > 0) {
    term.value = terms.value[0].term
    defaultTerm.value = term.value
  }
}

async function load() {
  if (!userId.value) {
    ElMessage.error(t('commissionResult.personMissing'))
    goBack()
    return
  }
  loading.value = true
  try {
    if (official.value && terms.value.length === 0) await bootstrapOfficialTerms()
    const data = official.value
      ? await commissionApi.officialMonths({
          roleType: roleType.value,
          userId: userId.value,
          keyword: keyword.value.trim() || undefined,
          term: term.value || undefined,
          page: page.value,
          pageSize: pageSize.value
        })
      : await commissionApi.estimatedMonths({
          roleType: roleType.value,
          userId: userId.value,
          keyword: keyword.value.trim() || undefined,
          page: page.value,
          pageSize: pageSize.value
        })
    userName.value = data.userName || userName.value || userId.value
    userLevel.value = data.userLevel
    rows.value = (data.items || []).map((r) => ({ ...r, rowKey: r.calcMonth }))
    total.value = data.totalCount
    totalLineCount.value = data.totalLineCount ?? 0
    totalCommissionUsd.value = data.totalCommissionUsd ?? 0
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionResult.loadFailed'))
  } finally {
    loading.value = false
  }
}

function reload() {
  page.value = 1
  void load()
}

function resetSearch() {
  keyword.value = ''
  if (official.value) term.value = defaultTerm.value || terms.value[0]?.term || ''
  reload()
}

function onPageSizeChange() {
  page.value = 1
  void load()
}

function onRowDblclick(row: MonthTableRow) {
  const query: Record<string, string> = {}
  if (userName.value) query.userName = userName.value
  if (official.value && term.value) query.term = term.value
  router.push({
    name: official.value
      ? isPurchase.value
        ? 'CommissionOfficialPurchaseMonth'
        : 'CommissionOfficialSalesMonth'
      : isPurchase.value
        ? 'CommissionEstimatedPurchaseMonth'
        : 'CommissionEstimatedSalesMonth',
    params: { userId: userId.value, calcMonth: row.calcMonth },
    query
  })
}

function goBack() {
  router.push({
    name: official.value
      ? isPurchase.value
        ? 'CommissionOfficialPurchase'
        : 'CommissionOfficialSales'
      : isPurchase.value
        ? 'CommissionEstimatedPurchase'
        : 'CommissionEstimatedSales'
  })
}

watch(userId, () => {
  page.value = 1
  void load()
})

onMounted(() => {
  void load()
})
</script>

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';
</style>
