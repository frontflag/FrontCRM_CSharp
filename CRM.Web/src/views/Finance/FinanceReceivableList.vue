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
          @keyup.enter="applyFiltersAndReload"
          @clear="applyFiltersAndReload"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select
          v-model="invoiceWriteOffFilter"
          :placeholder="t('financeReceivableList.filters.invoiceWriteOffStatus')"
          clearable
          class="filter-select filter-select--write-off"
          @change="applyFiltersAndReload"
        >
          <el-option :label="t('financeReceivableList.verification.pending')" :value="0" />
          <el-option :label="t('financeReceivableList.verification.partial')" :value="1" />
          <el-option :label="t('financeReceivableList.verification.complete')" :value="2" />
          <el-option :label="t('financeReceivableList.filters.writeOffOpen')" :value="WRITE_OFF_OPEN" />
        </el-select>
        <el-select
          v-model="receiptWriteOffFilter"
          :placeholder="t('financeReceivableList.filters.receiptWriteOffStatus')"
          clearable
          class="filter-select filter-select--write-off"
          @change="applyFiltersAndReload"
        >
          <el-option :label="t('financeReceivableList.verification.pending')" :value="0" />
          <el-option :label="t('financeReceivableList.verification.partial')" :value="1" />
          <el-option :label="t('financeReceivableList.verification.complete')" :value="2" />
          <el-option :label="t('financeReceivableList.filters.writeOffOpen')" :value="WRITE_OFF_OPEN" />
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
        <el-button type="primary" @click="applyFiltersAndReload">
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
        <button type="button" class="btn-export" :disabled="exporting" @click="() => void handleExport()">
          {{ t('financeReceivableList.filters.export') }}
        </button>
      </div>
    </div>

    <FinanceReceivableListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <CrmDataTable
      v-show="viewMode === 'list'"
      ref="dataTableRef"
      column-layout-key="finance-receivable-list-main-v8"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableData"
      v-loading="loading"
      row-class-name="table-row-pointer"
      @row-dblclick="onRowDblClick"
      @header-dragend="onReceivableTableHeaderDragEnd"
    >
      <template #col-verificationStatus="{ row }">
        <el-tag :type="verificationTagType(row.verificationStatus)" size="small">
          {{ verificationLabel(row.verificationStatus) }}
        </el-tag>
      </template>
      <template #col-invoiceMatchStatus="{ row }">
        <el-tag :type="verificationTagType(row.invoiceMatchStatus ?? 0)" size="small">
          {{ verificationLabel(row.invoiceMatchStatus ?? 0) }}
        </el-tag>
      </template>
      <template #col-stockOutDate="{ row }">{{ row.stockOutDate ? formatDisplayDate(row.stockOutDate) : '—' }}</template>
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
      <template #col-outboundQty="{ row }">
        <span v-if="row.outboundQty == null" class="dock-tier-empty">—</span>
        <span v-else class="dock-quote-tier-line">{{
          Number(row.outboundQty).toLocaleString('zh-CN', { maximumFractionDigits: 6 })
        }}</span>
      </template>
      <template #col-amount="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.amount)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.amount))]" :key="'recv-amt-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
      </template>
      <template #col-invoiceMatchDone="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.invoiceMatchDone)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.invoiceMatchDone))]" :key="'inv-done-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
      </template>
      <template #col-invoiceMatchToBe="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.invoiceMatchToBe)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.invoiceMatchToBe))]" :key="'inv-tobe-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
      </template>
      <template #col-verifiedDone="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.verifiedDone)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.verifiedDone))]" :key="'recv-done-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
      </template>
      <template #col-verifiedToBe="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.verifiedToBe)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.verifiedToBe))]" :key="'recv-tobe-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
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

    <div v-show="viewMode === 'list'" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('financeReceivableList.columnSettings')" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" :aria-label="t('financeReceivableList.columnSettings')" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
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
import { Search, Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { financeReceivableApi, type FinanceReceivable } from '@/api/financeReceivable'
import type { FinanceReceivableListAnalyticsQuery } from '@/api/financeReceivableAnalytics'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import CustomerExtendColumnHeader from '@/components/list/CustomerExtendColumnHeader.vue'
import CustomerExtendCell from '@/components/list/CustomerExtendCell.vue'
import { useCustomerExtendColumn, isCustomerExtendTableColumn } from '@/composables/useCustomerExtendColumn'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useListBoardHelpOverride } from '@/composables/useHelpDocOverride'
import { downloadCsvBlob } from '@/utils/exportFileName'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import {
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  listTotalAmountHasValue,
  splitListMoneyParts,
} from '@/utils/moneyFormat'
import FinanceReceivableListBoard from './FinanceReceivableListBoard.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
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
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const WRITE_OFF_OPEN = 'open' as const
type WriteOffFilter = number | typeof WRITE_OFF_OPEN | undefined

const loading = ref(false)
const exporting = ref(false)
const tableData = ref<FinanceReceivable[]>([])
const total = ref(0)
const viewMode = ref<'list' | 'board'>('list')
useListBoardHelpOverride('pages/应收款看板_MENU_FINANCE_RECEIVABLE_BOARD.md', viewMode)
const stockOutDateRange = ref<[string, string] | null>(null)
const receiptWriteOffFilter = ref<WriteOffFilter>(undefined)
const invoiceWriteOffFilter = ref<WriteOffFilter>(undefined)

const query = reactive({
  keyword: '',
  stockOutDateFrom: undefined as string | undefined,
  stockOutDateTo: undefined as string | undefined,
  page: 1,
  pageSize: 20
})

function appendWriteOffQueryParams(target: Record<string, unknown>) {
  if (receiptWriteOffFilter.value === WRITE_OFF_OPEN) target.onlyOpen = true
  else if (typeof receiptWriteOffFilter.value === 'number') target.verificationStatus = receiptWriteOffFilter.value
  if (invoiceWriteOffFilter.value === WRITE_OFF_OPEN) target.invoiceMatchOnlyOpen = true
  else if (typeof invoiceWriteOffFilter.value === 'number') target.invoiceMatchStatus = invoiceWriteOffFilter.value
}

const boardFilters = computed<FinanceReceivableListAnalyticsQuery>(() => {
  const filters: FinanceReceivableListAnalyticsQuery = {
    keyword: query.keyword?.trim() || undefined,
    stockOutDateFrom: query.stockOutDateFrom,
    stockOutDateTo: query.stockOutDateTo
  }
  appendWriteOffQueryParams(filters as Record<string, unknown>)
  return filters
})

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
  const w = (label: string, extra?: { align?: 'left' | 'center' | 'right'; extra?: number }) =>
    estimateListColumnHeaderMinWidth(label, extra)
  return [
    {
      key: 'receivableCode',
      prop: 'receivableCode',
      label: t('financeReceivableList.columns.code'),
      minWidth: w(t('financeReceivableList.columns.code'))
    },
    {
      key: 'stockOutDate',
      prop: 'stockOutDate',
      label: t('financeReceivableList.columns.stockOutDate'),
      width: w(t('financeReceivableList.columns.stockOutDate')),
      minWidth: w(t('financeReceivableList.columns.stockOutDate'))
    },
    {
      key: 'stockOutCode',
      prop: 'stockOutCode',
      label: t('financeReceivableList.columns.stockOutCode'),
      minWidth: w(t('financeReceivableList.columns.stockOutCode'))
    },
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
      minWidth: Math.max(140, w(t('financeReceivableList.columns.salesUser'))),
      showOverflowTooltip: true
    },
    {
      key: 'pn',
      prop: 'pn',
      label: t('financeReceivableList.columns.pn'),
      minWidth: Math.max(200, w(t('financeReceivableList.columns.pn'))),
      showOverflowTooltip: true
    },
    {
      key: 'brand',
      prop: 'brand',
      label: t('financeReceivableList.columns.brand'),
      minWidth: Math.max(140, w(t('financeReceivableList.columns.brand'))),
      showOverflowTooltip: true
    },
    {
      key: 'outboundQty',
      prop: 'outboundQty',
      label: t('financeReceivableList.columns.qty'),
      width: w(t('financeReceivableList.columns.qty'), { align: 'right' }),
      minWidth: w(t('financeReceivableList.columns.qty'), { align: 'right' }),
      align: 'right'
    },
    {
      key: 'amount',
      prop: 'amount',
      label: t('financeReceivableList.columns.amount'),
      width: Math.max(180, w(t('financeReceivableList.columns.amount'), { align: 'right' })),
      minWidth: Math.max(180, w(t('financeReceivableList.columns.amount'), { align: 'right' })),
      align: 'right'
    },
    {
      key: 'verificationStatus',
      prop: 'verificationStatus',
      label: t('financeReceivableList.columns.verificationStatus'),
      width: w(t('financeReceivableList.columns.verificationStatus'), { align: 'center' }),
      minWidth: w(t('financeReceivableList.columns.verificationStatus'), { align: 'center' }),
      align: 'center'
    },
    {
      key: 'verifiedDone',
      prop: 'verifiedDone',
      label: t('financeReceivableList.columns.verifiedDone'),
      width: Math.max(180, w(t('financeReceivableList.columns.verifiedDone'), { align: 'right' })),
      minWidth: Math.max(180, w(t('financeReceivableList.columns.verifiedDone'), { align: 'right' })),
      align: 'right'
    },
    {
      key: 'verifiedToBe',
      prop: 'verifiedToBe',
      label: t('financeReceivableList.columns.verifiedToBe'),
      width: Math.max(180, w(t('financeReceivableList.columns.verifiedToBe'), { align: 'right' })),
      minWidth: Math.max(180, w(t('financeReceivableList.columns.verifiedToBe'), { align: 'right' })),
      align: 'right'
    },
    {
      key: 'invoiceMatchStatus',
      prop: 'invoiceMatchStatus',
      label: t('financeReceivableList.columns.invoiceMatchStatus'),
      width: w(t('financeReceivableList.columns.invoiceMatchStatus'), { align: 'center' }),
      minWidth: w(t('financeReceivableList.columns.invoiceMatchStatus'), { align: 'center' }),
      align: 'center'
    },
    {
      key: 'invoiceMatchDone',
      prop: 'invoiceMatchDone',
      label: t('financeReceivableList.columns.invoiceMatchDone'),
      width: Math.max(180, w(t('financeReceivableList.columns.invoiceMatchDone'), { align: 'right' })),
      minWidth: Math.max(180, w(t('financeReceivableList.columns.invoiceMatchDone'), { align: 'right' })),
      align: 'right'
    },
    {
      key: 'invoiceMatchToBe',
      prop: 'invoiceMatchToBe',
      label: t('financeReceivableList.columns.invoiceMatchToBe'),
      width: Math.max(180, w(t('financeReceivableList.columns.invoiceMatchToBe'), { align: 'right' })),
      minWidth: Math.max(180, w(t('financeReceivableList.columns.invoiceMatchToBe'), { align: 'right' })),
      align: 'right'
    },
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

function applyFiltersAndReload() {
  query.page = 1
  void loadData()
}

async function loadData() {
  loading.value = true
  try {
    if (viewMode.value === 'board') return
    const params: Record<string, unknown> = {
      keyword: query.keyword || undefined,
      stockOutDateFrom: query.stockOutDateFrom,
      stockOutDateTo: query.stockOutDateTo,
      page: query.page,
      pageSize: query.pageSize
    }
    appendWriteOffQueryParams(params)
    const res = await financeReceivableApi.getPaged(params)
    tableData.value = res.items ?? []
    total.value = res.total ?? 0
  } finally {
    loading.value = false
  }
}

async function handleExport() {
  try {
    await ElMessageBox.confirm(
      t('financeReceivableList.messages.exportConfirmMessage'),
      t('financeReceivableList.messages.exportConfirmTitle'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }
  exporting.value = true
  try {
    const params: Record<string, unknown> = {
      keyword: query.keyword || undefined,
      stockOutDateFrom: query.stockOutDateFrom,
      stockOutDateTo: query.stockOutDateTo
    }
    appendWriteOffQueryParams(params)
    const blob = await financeReceivableApi.exportList(params)
    downloadCsvBlob(blob, '应收款.csv')
    ElMessage.success(t('financeReceivableList.messages.exportSuccess'))
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('financeReceivableList.messages.exportFailed'))
  } finally {
    exporting.value = false
  }
}

function applyKeywordFromRoute(q: typeof route.query) {
  if (!('keyword' in q)) return
  const raw = q.keyword
  const kw = typeof raw === 'string'
    ? raw.trim()
    : Array.isArray(raw)
      ? String(raw[0] ?? '').trim()
      : ''
  query.keyword = kw
  if (kw) query.page = 1
}

function syncQueryFromRoute() {
  if (route.name !== 'FinanceReceivableList') return
  const q = route.query
  if (q.onlyOpen === '1' || q.onlyOpen === 'true') {
    receiptWriteOffFilter.value = WRITE_OFF_OPEN
  }
  applyKeywordFromRoute(q)
}

watch(
  () => [route.name, route.query] as const,
  async () => {
    syncQueryFromRoute()
    if (route.name === 'FinanceReceivableList') await loadData()
  },
  { deep: true, immediate: true }
)

function openDetail(row: FinanceReceivable) {
  router.push({ name: 'FinanceReceivableDetail', params: { id: row.id } })
}

/** 无独立编辑页：Ctrl+双击与双击均进详情（《列表交互规范》§3.2）。 */
function onRowDblClick(row: FinanceReceivable) {
  openDetail(row)
}
</script>

<style scoped lang="scss">
@import './finance-common.scss';

.filter-date-range {
  width: 260px;
}

.filter-select--write-off {
  width: 190px;
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.list-main-pagination {
  margin-left: auto;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
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

:deep(.crm-items-table--density-compact) {
  .dock-tier-price-line,
  .dock-quote-tier-line {
    white-space: nowrap;
  }
}
</style>
