<template>
  <div class="crm-biz-list-page">
    <div class="page-header">
      <div class="header-left">
        <button type="button" class="btn-ghost btn-sm" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('commissionResult.backMonths') }}
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
            :placeholder="t('commissionResult.personKeywordPlaceholder')"
            @keyup.enter="reload"
          />
        </div>
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
        <template #col-stockOutCode="{ row }">
          <router-link
            v-if="canOpenStockOut && row.stockOutId"
            class="link-text"
            :to="`/inventory/stock-out/${row.stockOutId}`"
          >
            {{ row.stockOutCode }}
          </router-link>
          <span v-else>{{ row.stockOutCode || '—' }}</span>
        </template>
        <template #col-sellOrderCode="{ row }">
          <router-link
            v-if="canOpenSales && row.sellOrderId"
            class="link-text"
            :to="`/sales-orders/${row.sellOrderId}`"
          >
            {{ row.sellOrderCode }}
          </router-link>
          <span v-else>{{ row.sellOrderCode || '—' }}</span>
        </template>
        <template #col-purchaseOrderCode="{ row }">
          <router-link
            v-if="canOpenPurchase && row.purchaseOrderId"
            class="link-text"
            :to="`/purchase-orders/${row.purchaseOrderId}`"
          >
            {{ row.purchaseOrderCode }}
          </router-link>
          <span v-else>{{ row.purchaseOrderCode || '—' }}</span>
        </template>
        <template #col-stockOutDate="{ row }">
          {{ formatCommissionListDate(row.stockOutDate) }}
        </template>
        <template #col-receiptDate="{ row }">
          {{ formatCommissionListDate(row.receiptDate) }}
        </template>
        <template #col-gpUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.gpUsd" :currency="usdCurrency" />
        </template>
        <template #col-commissionUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.commissionUsd" :currency="usdCurrency" />
        </template>
      </CrmDataTable>
      <div v-show="!loading && rows.length === 0" class="empty-state">
        <p>{{ t('commissionResult.personEmpty') }}</p>
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
import { useAuthStore } from '@/stores'
import {
  COMMISSION_ROLE_PURCHASE,
  commissionApi,
  formatCommissionListDate,
  formatCommissionPoints
} from '@/api/commission'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

type PersonTableRow = {
  rowKey: string
  stockOutItemId?: string
  stockOutId: string
  stockOutCode: string
  stockOutDate?: string | null
  userName: string
  userLevel: number
  gpUsd: number
  ratePoints: number
  commissionUsd: number
  receiptDate?: string | null
  calcMonth?: string
  entryKind?: number
  sellOrderId?: string | null
  sellOrderCode?: string | null
  purchaseOrderId?: string | null
  purchaseOrderCode?: string | null
  term?: string | null
}

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const official = computed(() => route.meta.commissionMode === 'official')
const roleType = computed(() => (Number(route.meta.commissionRoleType) === 2 ? 2 : 1))
const isPurchase = computed(() => roleType.value === COMMISSION_ROLE_PURCHASE)
const userId = computed(() => String(route.params.userId ?? '').trim())
const calcMonth = computed(() => String(route.params.calcMonth ?? '').trim())
const canOpenStockOut = computed(() => authStore.hasPermission('inventory.read'))
const canOpenSales = computed(() => authStore.hasPermission('sales-order.read'))
const canOpenPurchase = computed(() => authStore.hasPermission('purchase-order.read'))
const usdCurrency = CurrencyCode.USD

const pageTitle = computed(() => {
  const month = calcMonth.value || ''
  if (official.value) {
    return isPurchase.value
      ? t('commissionResult.personDetailOfficialPurchaseMonth', { month })
      : t('commissionResult.personDetailOfficialSalesMonth', { month })
  }
  return isPurchase.value
    ? t('commissionResult.personDetailPurchaseMonth', { month })
    : t('commissionResult.personDetailSalesMonth', { month })
})

const userName = ref(String(route.query.userName ?? '').trim())
const userLevel = ref<number | null>(null)

const personLabel = computed(() => {
  const name = userName.value || userId.value || '—'
  if (userLevel.value == null) return name
  return `${name} · ${t('commissionResult.userLevel')} ${userLevel.value}`
})

const columnKey = computed(
  () => `commission-person-${official.value ? 'official' : 'estimated'}-${roleType.value}-v4`
)

const loading = ref(false)
const keyword = ref('')
const term = ref(String(route.query.term ?? '').trim())
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const rows = ref<PersonTableRow[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

function headerMin(label: string) {
  return estimateListColumnHeaderMinWidth(label)
}

function entryKindText(kind?: number) {
  if (kind === 1) return t('commissionResult.entryBase')
  if (kind === 2) return t('commissionResult.entryOther')
  return '—'
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const outDate = t('commissionResult.stockOutDateFull')
  const outCode = t('commissionResult.stockOut')
  const gp = t('commissionResult.gpShort')
  const points = t('commissionResult.pointsFull')
  const amount = t('commissionResult.commissionAmount')
  const month = t('commissionResult.calcMonth')
  const kind = t('commissionResult.entryKind')
  const cols: CrmTableColumnDef[] = [
    { key: 'stockOutDate', label: outDate, width: Math.max(112, headerMin(outDate)), align: 'center' },
    { key: 'stockOutCode', label: outCode, minWidth: Math.max(140, headerMin(outCode)), showOverflowTooltip: true },
    {
      key: 'sellOrderCode',
      label: t('commissionResult.sellOrder'),
      minWidth: Math.max(140, headerMin(t('commissionResult.sellOrder'))),
      showOverflowTooltip: true
    }
  ]
  if (isPurchase.value) {
    cols.push({
      key: 'purchaseOrderCode',
      label: t('commissionResult.purchaseOrder'),
      minWidth: Math.max(140, headerMin(t('commissionResult.purchaseOrder'))),
      showOverflowTooltip: true
    })
  }
  cols.push(
    {
      key: 'receiptDate',
      label: t('commissionResult.receiptDateFull'),
      width: Math.max(128, headerMin(t('commissionResult.receiptDateFull'))),
      align: 'center'
    },
    { key: 'gpUsd', label: gp, minWidth: Math.max(100, headerMin(gp)), align: 'right' },
    {
      key: 'calcMonth',
      label: month,
      width: Math.max(112, headerMin(month)),
      align: 'center',
      formatter: (row: unknown) => (row as PersonTableRow).calcMonth || calcMonth.value || '—'
    },
    {
      key: 'entryKind',
      label: kind,
      width: Math.max(96, headerMin(kind)),
      align: 'center',
      formatter: (row: unknown) => entryKindText((row as PersonTableRow).entryKind)
    },
    {
      key: 'ratePoints',
      label: points,
      width: Math.max(96, headerMin(points)),
      align: 'right',
      formatter: (row: unknown) => formatCommissionPoints((row as PersonTableRow).ratePoints)
    },
    { key: 'commissionUsd', label: amount, minWidth: Math.max(120, headerMin(amount)), align: 'right' }
  )
  return cols
})

async function load() {
  if (!userId.value || !calcMonth.value) {
    ElMessage.error(t('commissionResult.personMissing'))
    goBack()
    return
  }
  loading.value = true
  try {
    const params = {
      roleType: roleType.value,
      userId: userId.value,
      calcMonth: calcMonth.value,
      keyword: keyword.value.trim() || undefined,
      term: official.value ? term.value || undefined : undefined,
      page: page.value,
      pageSize: pageSize.value
    }
    const data = official.value
      ? await commissionApi.officialPersonLines(params)
      : await commissionApi.estimatedPersonLines(params)
    userName.value = data.userName || userName.value || userId.value
    userLevel.value = data.userLevel
    rows.value = (data.items || []).map((r) => ({ ...r, rowKey: r.stockOutItemId }))
    total.value = data.totalCount
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
  reload()
}

function onPageSizeChange() {
  page.value = 1
  void load()
}

function onRowDblclick(row: PersonTableRow) {
  if (canOpenStockOut.value && row.stockOutId) {
    window.open(`/inventory/stock-out/${row.stockOutId}`, '_blank')
  }
}

function goBack() {
  const query: Record<string, string> = {}
  if (userName.value) query.userName = userName.value
  if (official.value && term.value) query.term = term.value
  router.push({
    name: official.value
      ? isPurchase.value
        ? 'CommissionOfficialPurchasePerson'
        : 'CommissionOfficialSalesPerson'
      : isPurchase.value
        ? 'CommissionEstimatedPurchasePerson'
        : 'CommissionEstimatedSalesPerson',
    params: { userId: userId.value },
    query
  })
}

watch([userId, calcMonth], () => {
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

<style scoped lang="scss">
.link-text {
  color: inherit;
  text-decoration: none;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
  }
}
</style>
