<template>
  <div class="finance-page">
    <div class="stmt-list-page-header">
      <h1 class="finance-list-page-title">{{ t('financeReceivableStatement.listTitle') }}</h1>
      <div class="stmt-list-page-header__actions">
        <button type="button" class="btn-quote-desktop" @click="goReceivables">
          <span>{{ t('financeReceivableStatement.backToReceivables') }}</span>
          <el-icon class="btn-quote-desktop__arrow"><ArrowRight /></el-icon>
        </button>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financeReceivableStatement.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="applyFiltersAndReload"
          @clear="applyFiltersAndReload"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select
          v-model="query.currency"
          clearable
          class="filter-select"
          :placeholder="t('financeReceivableStatement.filters.currency')"
          @change="applyFiltersAndReload"
        >
          <el-option
            v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-checkbox v-model="query.onlyOpen" @change="applyFiltersAndReload">
          {{ t('financeReceivableStatement.filters.onlyOpen') }}
        </el-checkbox>
        <el-button type="primary" @click="applyFiltersAndReload">
          <el-icon><Search /></el-icon> {{ t('financeReceivableStatement.filters.search') }}
        </el-button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-receivable-statement-list-v2"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableData"
      v-loading="loading"
      highlight-current-row
      row-class-name="table-row-pointer"
      @row-click="openDetail"
      @row-dblclick="openDetail"
    >
      <template #col-latestStockOutDate="{ row }">
        {{ row.latestStockOutDate ? formatDisplayDate(row.latestStockOutDate) : '—' }}
      </template>
      <template #col-currency="{ row }">
        <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
          {{ listAmountCurrencyIso(row.currency) }}
        </span>
      </template>
      <template #col-amountTotal="{ row }">
        <MoneyCell :amount="row.amountTotal" :currency="row.currency" :masked="maskSaleSensitiveFields" />
      </template>
      <template #col-verifiedDone="{ row }">
        <MoneyCell :amount="row.verifiedDone" :currency="row.currency" :masked="maskSaleSensitiveFields" />
      </template>
      <template #col-verifiedToBe="{ row }">
        <MoneyCell :amount="row.verifiedToBe" :currency="row.currency" :masked="maskSaleSensitiveFields" />
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
              {{ t('financeReceivableStatement.actions.detail') }}
            </el-button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financeReceivableStatement.actions.detail') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('financeReceivableStatement.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('financeReceivableStatement.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
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
import { onMounted, reactive, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ArrowRight, Search, Setting } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  financeReceivableStatementApi,
  type FinanceReceivableStatementListItem
} from '@/api/financeReceivableStatement'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { lastCalendarMonthRange } from '@/utils/receivableStatementPeriod'
import MoneyCell from './ReceivableStatementMoneyCell.vue'

const { t } = useI18n()
const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const loading = ref(false)
const tableData = ref<FinanceReceivableStatementListItem[]>([])
const total = ref(0)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))

function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const query = reactive({
  keyword: '',
  currency: undefined as number | undefined,
  onlyOpen: false,
  page: 1,
  pageSize: 20
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const w = (label: string, extra?: { align?: 'left' | 'center' | 'right' }) =>
    estimateListColumnHeaderMinWidth(label, extra)
  return [
    { key: 'customerCode', prop: 'customerCode', label: t('financeReceivableStatement.columns.customerCode'), minWidth: w(t('financeReceivableStatement.columns.customerCode')) },
    { key: 'customerName', prop: 'customerName', label: t('financeReceivableStatement.columns.customerName'), minWidth: Math.max(180, w(t('financeReceivableStatement.columns.customerName'))), showOverflowTooltip: true },
    { key: 'customerEnglishName', prop: 'customerEnglishName', label: t('financeReceivableStatement.columns.customerEnglishName'), minWidth: Math.max(180, w(t('financeReceivableStatement.columns.customerEnglishName'))), showOverflowTooltip: true },
    { key: 'currency', prop: 'currency', label: t('financeReceivableStatement.columns.currency'), width: w(t('financeReceivableStatement.columns.currency'), { align: 'center' }), align: 'center' },
    { key: 'receivableCount', prop: 'receivableCount', label: t('financeReceivableStatement.columns.receivableCount'), width: w(t('financeReceivableStatement.columns.receivableCount'), { align: 'right' }), align: 'right' },
    { key: 'amountTotal', prop: 'amountTotal', label: t('financeReceivableStatement.columns.amountTotal'), minWidth: Math.max(160, w(t('financeReceivableStatement.columns.amountTotal'), { align: 'right' })), align: 'right' },
    { key: 'verifiedDone', prop: 'verifiedDone', label: t('financeReceivableStatement.columns.verifiedDone'), minWidth: Math.max(160, w(t('financeReceivableStatement.columns.verifiedDone'), { align: 'right' })), align: 'right' },
    { key: 'verifiedToBe', prop: 'verifiedToBe', label: t('financeReceivableStatement.columns.verifiedToBe'), minWidth: Math.max(160, w(t('financeReceivableStatement.columns.verifiedToBe'), { align: 'right' })), align: 'right' },
    { key: 'latestStockOutDate', prop: 'latestStockOutDate', label: t('financeReceivableStatement.columns.latestStockOutDate'), width: w(t('financeReceivableStatement.columns.latestStockOutDate')) },
    { key: 'salesUserName', prop: 'salesUserName', label: t('financeReceivableStatement.columns.salesUser'), minWidth: Math.max(140, w(t('financeReceivableStatement.columns.salesUser'))), showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('financeReceivableStatement.columns.actions'),
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

function applyFiltersAndReload() {
  query.page = 1
  void loadData()
}

async function loadData() {
  loading.value = true
  try {
    const data = await financeReceivableStatementApi.getPaged({
      keyword: query.keyword.trim() || undefined,
      onlyOpen: query.onlyOpen || undefined,
      currency: query.currency,
      page: query.page,
      pageSize: query.pageSize
    })
    tableData.value = data.items ?? []
    total.value = data.total ?? 0
  } catch (e) {
    tableData.value = []
    total.value = 0
    ElMessage.error(e instanceof Error ? e.message : t('financeReceivableStatement.loadFailed'))
  } finally {
    loading.value = false
  }
}

function openDetail(row: FinanceReceivableStatementListItem) {
  const [from, to] = lastCalendarMonthRange()
  void router.push({
    name: 'FinanceReceivableStatementDetail',
    params: { customerId: row.customerId, currency: String(row.currency) },
    query: { from, to }
  })
}

function goReceivables() {
  void router.push({ name: 'FinanceReceivableList' })
}

onMounted(() => {
  void loadData()
})
</script>

<style lang="scss" scoped>
@import './finance-common.scss';

.stmt-list-page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.stmt-list-page-header__actions {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

/* 与 /finance/receivables「客户对账单」同款 */
.btn-quote-desktop {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px 8px 18px;
  border: none;
  border-radius: 10px;
  background: #eaf5ff;
  color: #1a2332;
  font-size: 13px;
  font-weight: 500;
  font-family: 'Noto Sans SC', sans-serif;
  line-height: 1.2;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  flex-shrink: 0;

  &:hover {
    background: #ddefff;
    color: #0f172a;
  }

  &:active {
    background: #d0e8ff;
  }

  &__arrow {
    font-size: 14px;
  }
}
</style>
