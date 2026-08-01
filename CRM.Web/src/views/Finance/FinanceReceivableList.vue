<template>
  <div class="finance-page">
    <h1 class="finance-list-page-title">{{ t('financeReceivableList.pageTitle') }}</h1>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financeReceivableList.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select
          v-model="query.verificationStatus"
          :placeholder="t('financeReceivableList.filters.verificationStatus')"
          clearable
          class="filter-select"
          @change="loadData"
        >
          <el-option :label="t('financeReceivableList.verification.pending')" :value="0" />
          <el-option :label="t('financeReceivableList.verification.partial')" :value="1" />
          <el-option :label="t('financeReceivableList.verification.complete')" :value="2" />
        </el-select>
        <el-date-picker
          v-model="stockOutDateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          unlink-panels
          :range-separator="t('financeReceivableList.filters.dateSep')"
          :start-placeholder="t('financeReceivableList.filters.stockOutDateFrom')"
          :end-placeholder="t('financeReceivableList.filters.stockOutDateTo')"
          class="filter-date-range"
          @change="onStockOutDateChange"
        />
        <el-checkbox v-model="query.onlyOpen" @change="loadData">
          {{ t('financeReceivableList.filters.onlyOpen') }}
        </el-checkbox>
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> {{ t('financeReceivableList.filters.search') }}
        </el-button>
        <el-button
          class="btn-ghost btn-sm btn-board-active"
          @click="toggleViewMode"
        >
          {{
            viewMode === 'board'
              ? t('financeReceivableList.filters.listView')
              : t('financeReceivableList.filters.boardView')
          }}
        </el-button>
      </div>
      <div class="search-right">
        <el-button v-if="canWriteFinanceReceipt" type="primary" @click="goWriteOff">
          {{ t('financeReceivableList.goWriteOff') }}
        </el-button>
      </div>
    </div>

    <FinanceReceivableListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <CrmDataTable
      v-show="viewMode === 'list'"
      column-layout-key="finance-receivable-list-main-v4"
      :columns="tableColumns"
      :show-column-settings="false"
      :data="tableData"
      v-loading="loading"
      row-class-name="table-row-pointer"
      @row-dblclick="openDetail"
      @header-dragend="onReceivableTableHeaderDragEnd"
    >
      <template #col-verificationStatus="{ row }">
        <el-tag :type="verificationTagType(row.verificationStatus)" size="small">
          {{ verificationLabel(row.verificationStatus) }}
        </el-tag>
      </template>
      <template #col-stockOutDate="{ row }">{{ formatDate(row.stockOutDate) }}</template>
      <template #col-receivableCode="{ row }">
        <span class="code-text">{{ row.receivableCode || '—' }}</span>
      </template>
      <template #col-stockOutCode="{ row }">
        <router-link class="link-text" :to="`/inventory/stock-out/${row.stockOutId}`">
          {{ row.stockOutCode }}
        </router-link>
      </template>
      <template #col-customer-header>
        <CustomerExtendColumnHeader
          :active-field="customerExtendActiveField"
          @set-active-field="setCustomerExtendActiveField"
        />
      </template>
      <template #col-customer="{ row }">
        <CustomerExtendCell
          :row="row"
          :active-field="customerExtendActiveField"
          :masked="maskSaleSensitiveFields"
          :empty-text="t('quoteList.na')"
        />
      </template>
      <template #col-salesUserName="{ row }">
        {{ maskSaleSensitiveFields ? '—' : (row.salesUserName || '—') }}
      </template>
      <template #col-amount="{ row }">
        <span class="amount-text amount-text--receivable">{{
          maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(row.amount, row.currency)
        }}</span>
      </template>
      <template #col-verifiedDone="{ row }">
        <span class="amount-text amount-text--received">{{
          maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(row.verifiedDone, row.currency)
        }}</span>
      </template>
      <template #col-verifiedToBe="{ row }">
        <span class="amount-text amount-text--pending">{{
          maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(row.verifiedToBe, row.currency)
        }}</span>
      </template>
      <template #col-actions-header>
        <div class="list-op-col-header--icon-only">
          <button
            type="button"
            class="op-col-toggle-btn list-op-col-toggle"
            :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
            @click.stop="toggleOpCol"
          >
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">
              {{ t('financeReceivableList.actions.detail') }}
            </el-button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financeReceivableList.actions.detail') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div v-show="viewMode === 'list'" class="pagination-wrap">
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @current-change="loadData"
        @size-change="loadData"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Search } from '@element-plus/icons-vue'
import { financeReceivableApi, type FinanceReceivable } from '@/api/financeReceivable'
import type { FinanceReceivableListAnalyticsQuery } from '@/api/financeReceivableAnalytics'
import { CURRENCY_MAP } from '@/api/finance'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import CustomerExtendColumnHeader from '@/components/list/CustomerExtendColumnHeader.vue'
import CustomerExtendCell from '@/components/list/CustomerExtendCell.vue'
import { useCustomerExtendColumn, isCustomerExtendTableColumn } from '@/composables/useCustomerExtendColumn'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useListBoardHelpOverride } from '@/composables/useHelpDocOverride'
import FinanceReceivableListBoard from './FinanceReceivableListBoard.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const { canWriteFinanceReceipt } = useFinanceWriteGate()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const {
  expanded: customerExtendExpanded,
  activeField: customerExtendActiveField,
  colWidth: customerExtendColWidth,
  colMinWidth: customerExtendColMinWidth,
  setActiveField: setCustomerExtendActiveField,
  applyOuterWidthFromTable: applyCustomerExtendOuterWidth
} = useCustomerExtendColumn()

function onReceivableTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isCustomerExtendTableColumn(column)) return
  applyCustomerExtendOuterWidth(newWidth)
}

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 88
const OP_COL_EXPANDED_MIN_WIDTH = 80
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const loading = ref(false)
const tableData = ref<FinanceReceivable[]>([])
const total = ref(0)
const viewMode = ref<'list' | 'board'>('list')
useListBoardHelpOverride('pages/应收款看板_MENU_FINANCE_RECEIVABLE_BOARD.md', viewMode)
const stockOutDateRange = ref<[string, string] | null>(null)

const query = reactive({
  keyword: '',
  verificationStatus: undefined as number | undefined,
  onlyOpen: true,
  stockOutDateFrom: undefined as string | undefined,
  stockOutDateTo: undefined as string | undefined,
  page: 1,
  pageSize: 20
})

const boardFilters = computed<FinanceReceivableListAnalyticsQuery>(() => ({
  keyword: query.keyword?.trim() || undefined,
  verificationStatus: query.verificationStatus,
  onlyOpen: query.onlyOpen,
  stockOutDateFrom: query.stockOutDateFrom,
  stockOutDateTo: query.stockOutDateTo
}))

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
  if (viewMode.value === 'list') void loadData()
}

function onStockOutDateChange(val: [string, string] | null) {
  query.stockOutDateFrom = val?.[0]
  query.stockOutDateTo = val?.[1]
  query.page = 1
  void loadData()
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  void customerExtendExpanded.value
  void customerExtendColWidth.value
  return [
    {
      key: 'verificationStatus',
      prop: 'verificationStatus',
      label: t('financeReceivableList.columns.verificationStatus'),
      width: 108,
      minWidth: 108,
      align: 'center'
    },
    { key: 'stockOutDate', prop: 'stockOutDate', label: t('financeReceivableList.columns.stockOutDate'), width: 120 },
    { key: 'receivableCode', prop: 'receivableCode', label: t('financeReceivableList.columns.code'), minWidth: 120 },
    { key: 'stockOutCode', prop: 'stockOutCode', label: t('financeReceivableList.columns.stockOutCode'), minWidth: 130 },
    {
      key: 'customer',
      label: t('common.customerExtendCol.columnTitle'),
      prop: 'customer',
      minWidth: customerExtendColMinWidth.value,
      width: customerExtendColWidth.value,
      showOverflowTooltip: true,
      className: 'customer-extend-col',
      labelClassName: 'customer-extend-col'
    },
    {
      key: 'salesUserName',
      prop: 'salesUserName',
      label: t('financeReceivableList.columns.salesUser'),
      width: 96,
      minWidth: 96,
      showOverflowTooltip: true
    },
    { key: 'pn', prop: 'pn', label: t('financeReceivableList.columns.pn'), minWidth: 120 },
    { key: 'brand', prop: 'brand', label: t('financeReceivableList.columns.brand'), minWidth: 100 },
    { key: 'outboundQty', prop: 'outboundQty', label: t('financeReceivableList.columns.qty'), width: 112, minWidth: 112, align: 'right' },
    { key: 'amount', prop: 'amount', label: t('financeReceivableList.columns.amount'), width: 190, minWidth: 180, align: 'right', className: 'receivable-amount-col', labelClassName: 'receivable-amount-col' },
    { key: 'verifiedDone', prop: 'verifiedDone', label: t('financeReceivableList.columns.verifiedDone'), width: 190, minWidth: 180, align: 'right', className: 'receivable-amount-col', labelClassName: 'receivable-amount-col' },
    { key: 'verifiedToBe', prop: 'verifiedToBe', label: t('financeReceivableList.columns.verifiedToBe'), width: 190, minWidth: 180, align: 'right', className: 'receivable-amount-col', labelClassName: 'receivable-amount-col' },
    {
      key: 'actions',
      label: t('financeReceivableList.columns.actions'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
      resizable: false
    }
  ]
})

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function currencyLabel(currency?: number) {
  if (currency == null) return ''
  return CURRENCY_MAP[currency] ?? String(currency)
}

function formatAmountWithCurrency(amount?: number, currency?: number) {
  if (amount == null) return '—'
  if (currency == null) return formatAmount(amount)
  return `${formatAmount(amount)} ${currencyLabel(currency)}`
}

function formatDate(v?: string) {
  if (!v) return '—'
  return v.slice(0, 10)
}

function verificationLabel(status: number) {
  if (status === 2) return t('financeReceivableList.verification.complete')
  if (status === 1) return t('financeReceivableList.verification.partial')
  return t('financeReceivableList.verification.pending')
}

function verificationTagType(status: number): 'success' | 'warning' | 'info' {
  if (status === 2) return 'success'
  if (status === 1) return 'warning'
  return 'info'
}

async function loadData() {
  loading.value = true
  try {
    if (viewMode.value === 'board') return
    const res = await financeReceivableApi.getPaged({
      keyword: query.keyword || undefined,
      verificationStatus: query.verificationStatus,
      onlyOpen: query.onlyOpen,
      stockOutDateFrom: query.stockOutDateFrom,
      stockOutDateTo: query.stockOutDateTo,
      page: query.page,
      pageSize: query.pageSize
    })
    tableData.value = res.items ?? []
    total.value = res.total ?? 0
  } finally {
    loading.value = false
  }
}

function syncQueryFromRoute() {
  if (route.name !== 'FinanceReceivableList') return
  const q = route.query
  if (q.onlyOpen === '0' || q.onlyOpen === 'false') {
    query.onlyOpen = false
  } else if (q.onlyOpen === '1' || q.onlyOpen === 'true') {
    query.onlyOpen = true
  }
}

watch(
  () => [route.name, route.query] as const,
  async () => {
    syncQueryFromRoute()
    if (route.name === 'FinanceReceivableList') await loadData()
  },
  { deep: true, immediate: true }
)

function goWriteOff() {
  router.push({ name: 'FinanceReceiptWriteOff' })
}

function openDetail(row: FinanceReceivable) {
  router.push({ name: 'FinanceReceivableDetail', params: { id: row.id } })
}
</script>

<style scoped lang="scss">
@import './finance-common.scss';
@import '@/assets/styles/variables.scss';

.pagination-wrap {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.filter-date-range {
  width: 260px;
}

.btn-board-active {
  border-color: #13c2c2;
  color: #13c2c2;
}

.btn-board-active:hover {
  border-color: #36cfc9;
  color: #36cfc9;
  background: rgba(19, 194, 194, 0.08);
}

.link-text {
  color: var(--el-color-primary);
  text-decoration: none;
  &:hover { text-decoration: underline; }
}

.amount-text {
  white-space: nowrap;

  &--receivable {
    color: $cyan-primary;
    font-weight: 700;
  }

  &--received {
    color: $success-color;
    font-weight: 700;
  }

  &--pending {
    color: #e8a838;
    font-weight: 700;
  }
}
</style>
