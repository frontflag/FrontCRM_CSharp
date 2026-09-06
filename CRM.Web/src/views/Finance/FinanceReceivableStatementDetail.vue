<template>
  <div class="crm-biz-list-page stmt-detail-page" v-loading="loading">
    <div class="page-header">
      <div class="header-left">
        <button type="button" class="btn-ghost btn-sm" @click="goBack">
          {{ t('financeReceivableStatement.back') }}
        </button>
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="16" y1="13" x2="8" y2="13" />
              <line x1="16" y1="17" x2="8" y2="17" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('financeReceivableStatement.detailTitle') }}</h1>
        </div>
        <div v-if="detail" class="count-badge stmt-header-badge">
          <span>{{ customerDisplayName }}</span>
          <span class="stmt-header-badge__sep">·</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(detail.statement.currency)]">
            {{ listAmountCurrencyIso(detail.statement.currency) }}
          </span>
          <span class="stmt-header-badge__sep">·</span>
          <span>{{ t('financeReceivableStatement.lineCount', { n: ledgerRows.length }) }}</span>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-primary" :disabled="!detail" @click="goPreview">
          {{ t('financeReceivableStatement.previewReport') }}
        </button>
      </div>
    </div>

    <el-alert v-if="errorMsg" :title="errorMsg" type="error" show-icon class="stmt-alert" />

    <template v-if="detail">
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('financeReceivableStatement.customerSection') }}</span>
          </div>
          <div class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('financeReceivableStatement.fields.generatedOn') }}</span>
              <span class="section-header-meta-item__value">{{ detail.statement.generatedOn }}</span>
            </span>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('financeReceivableStatement.fields.currency') }}</span>
              <span class="section-header-meta-item__value">
                <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(detail.statement.currency)]">
                  {{ listAmountCurrencyIso(detail.statement.currency) }}
                </span>
              </span>
            </span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.customerName') }}</span>
            <span class="info-value">{{ customerDisplayName }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.contact') }}</span>
            <span class="info-value">{{ detail.customer.canViewFull ? (detail.customer.contactName || '—') : '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.salesUser') }}</span>
            <span class="info-value">{{ detail.customer.salesUserName || '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.phone') }}</span>
            <span class="info-value">{{ detail.customer.canViewFull ? (detail.customer.contactPhone || '—') : '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.paymentDays') }}</span>
            <span class="info-value">{{ paymentDaysText }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.creditLimit') }}</span>
            <span class="info-value">{{ creditLimitText }}</span>
          </div>
        </div>
      </div>

      <div class="stat-cards">
        <div class="stat-card">
          <div class="stat-value">
            <MoneyCell :amount="detail.statement.opening" :currency="detail.statement.currency" :masked="maskSaleSensitiveFields" />
          </div>
          <div class="stat-label">{{ t('financeReceivableStatement.kpi.opening') }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-value">
            <MoneyCell :amount="detail.statement.periodIncrease" :currency="detail.statement.currency" :masked="maskSaleSensitiveFields" />
          </div>
          <div class="stat-label">{{ t('financeReceivableStatement.kpi.increase') }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-value stat-value--recv">
            <MoneyCell :amount="detail.statement.periodReceived" :currency="detail.statement.currency" :masked="maskSaleSensitiveFields" />
          </div>
          <div class="stat-label">{{ t('financeReceivableStatement.kpi.received') }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-value stat-value--end">
            <MoneyCell :amount="detail.statement.ending" :currency="detail.statement.currency" :masked="maskSaleSensitiveFields" />
          </div>
          <div class="stat-label">{{ t('financeReceivableStatement.kpi.ending') }}</div>
        </div>
      </div>

      <div class="search-bar">
        <div class="search-left">
          <div class="date-range-group">
            <el-date-picker
              v-model="periodFrom"
              type="date"
              value-format="YYYY-MM-DD"
              clearable
              :teleported="false"
              :placeholder="t('financeReceivableStatement.fields.periodFrom')"
              class="filter-date"
            />
            <span class="date-range-sep">{{ t('financeReceivableStatement.filters.dateSep') }}</span>
            <el-date-picker
              v-model="periodTo"
              type="date"
              value-format="YYYY-MM-DD"
              clearable
              :teleported="false"
              :placeholder="t('financeReceivableStatement.fields.periodTo')"
              class="filter-date"
            />
          </div>
          <el-date-picker
            v-model="aging"
            type="date"
            value-format="YYYY-MM-DD"
            clearable
            :teleported="false"
            :placeholder="t('financeReceivableStatement.fields.agingCutoff')"
            class="filter-date"
          />
          <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="handleSearch">
            {{ t('financeReceivableStatement.filters.search') }}
          </button>
          <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="handleReset">
            {{ t('financeReceivableStatement.filters.reset') }}
          </button>
        </div>
      </div>

      <div class="table-wrapper table-stack">
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
            <div class="list-footer-spacer" aria-hidden="true" />
          </div>
        </div>

        <CrmDataTable
          ref="dataTableRef"
          column-layout-key="finance-receivable-statement-detail-ledger-v1"
          :columns="tableColumns"
          :show-column-settings="false"
          :density-toggle-anchor-el="rowDensityToggleAnchorEl"
          :data="ledgerRows"
          row-key="rowKey"
          highlight-current-row
          row-class-name="table-row-pointer"
          @row-dblclick="onRowDblClick"
        >
          <template #col-date="{ row }">{{ formatLedgerDate(row.date) }}</template>
          <template #col-docNo="{ row }">
            <router-link
              v-if="row.lineType === 'increase' && row.receivableId && row.docNo"
              class="link-text"
              :to="`/finance/receivables/${row.receivableId}`"
              @click.stop
              @dblclick.stop
            >{{ row.docNo }}</router-link>
            <router-link
              v-else-if="row.lineType === 'receipt' && row.receiptId && row.docNo"
              class="link-text"
              :to="`/finance/receipts/${row.receiptId}`"
              @click.stop
              @dblclick.stop
            >{{ row.docNo }}</router-link>
            <span v-else>{{ displayDocNo(row) }}</span>
          </template>
          <template #col-summary="{ row }">{{ lineSummary(row) }}</template>
          <template #col-increaseAmount="{ row }">
            <MoneyCell
              v-if="row.increaseAmount != null"
              :amount="row.increaseAmount"
              :currency="detail.statement.currency"
              :masked="maskSaleSensitiveFields"
            />
            <span v-else>—</span>
          </template>
          <template #col-receivedAmount="{ row }">
            <span v-if="row.receivedAmount != null" class="amt-recv">
              <MoneyCell :amount="row.receivedAmount" :currency="detail.statement.currency" :masked="maskSaleSensitiveFields" />
            </span>
            <span v-else>—</span>
          </template>
          <template #col-balance="{ row }">
            <span :class="{ 'amt-end': row.rowKey === lastLedgerRowKey }">
              <MoneyCell :amount="row.balance" :currency="detail.statement.currency" :masked="maskSaleSensitiveFields" />
            </span>
          </template>
        </CrmDataTable>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import {
  financeReceivableStatementApi,
  type FinanceReceivableStatementDetail,
  type FinanceReceivableStatementLine
} from '@/api/financeReceivableStatement'
import { getApiErrorMessage } from '@/utils/apiError'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { formatTotalAmountNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { lastCalendarMonthRange, todayYmd } from '@/utils/receivableStatementPeriod'
import MoneyCell from './ReceivableStatementMoneyCell.vue'

type LedgerRow = FinanceReceivableStatementLine & { rowKey: string }

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const loading = ref(false)
const errorMsg = ref('')
const detail = ref<FinanceReceivableStatementDetail | null>(null)
const periodFrom = ref('')
const periodTo = ref('')
const aging = ref(todayYmd())
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const customerDisplayName = computed(() => {
  const c = detail.value?.customer
  if (!c) return '—'
  return c.customerName || c.customerEnglishName || c.customerCode || '—'
})

const paymentDaysText = computed(() => {
  const days = detail.value?.customer.paymentDays
  if (days == null || !detail.value?.customer.canViewFull) return '—'
  return t('financeReceivableStatement.paymentDaysValue', { n: days })
})

const creditLimitText = computed(() => {
  const v = detail.value?.customer.creditLimit
  if (v == null || !detail.value?.customer.canViewFull) return '—'
  if (maskSaleSensitiveFields.value) return '—'
  return `${formatTotalAmountNumber(v)}（${t('financeReceivableStatement.creditFromMaster')}）`
})

const ledgerRows = computed<LedgerRow[]>(() =>
  (detail.value?.lines ?? []).map((row, index) => ({
    ...row,
    rowKey: `${row.lineType}:${row.date}:${row.docNo ?? ''}:${row.receivableId ?? ''}:${row.receiptId ?? ''}:${index}`
  }))
)

const lastLedgerRowKey = computed(() => {
  const rows = ledgerRows.value
  return rows.length > 0 ? rows[rows.length - 1].rowKey : ''
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const w = (label: string, extra?: { align?: 'left' | 'center' | 'right' }) =>
    estimateListColumnHeaderMinWidth(label, extra)
  return [
    {
      key: 'date',
      prop: 'date',
      label: t('financeReceivableStatement.ledger.date'),
      width: Math.max(120, w(t('financeReceivableStatement.ledger.date')))
    },
    {
      key: 'docNo',
      prop: 'docNo',
      label: t('financeReceivableStatement.ledger.docNo'),
      width: Math.max(140, w(t('financeReceivableStatement.ledger.docNo'))),
      minWidth: Math.max(140, w(t('financeReceivableStatement.ledger.docNo'))),
      showOverflowTooltip: true
    },
    {
      key: 'summary',
      prop: 'summary',
      label: t('financeReceivableStatement.ledger.summary'),
      width: Math.max(220, w(t('financeReceivableStatement.ledger.summary'))),
      minWidth: Math.max(220, w(t('financeReceivableStatement.ledger.summary'))),
      showOverflowTooltip: true
    },
    {
      key: 'increaseAmount',
      prop: 'increaseAmount',
      label: t('financeReceivableStatement.ledger.increase'),
      width: Math.max(150, w(t('financeReceivableStatement.ledger.increase'), { align: 'right' })),
      minWidth: Math.max(150, w(t('financeReceivableStatement.ledger.increase'), { align: 'right' })),
      align: 'right'
    },
    {
      key: 'receivedAmount',
      prop: 'receivedAmount',
      label: t('financeReceivableStatement.ledger.received'),
      width: Math.max(150, w(t('financeReceivableStatement.ledger.received'), { align: 'right' })),
      minWidth: Math.max(150, w(t('financeReceivableStatement.ledger.received'), { align: 'right' })),
      align: 'right'
    },
    {
      key: 'balance',
      prop: 'balance',
      label: t('financeReceivableStatement.ledger.balance'),
      width: Math.max(160, w(t('financeReceivableStatement.ledger.balance'), { align: 'right' })),
      minWidth: Math.max(160, w(t('financeReceivableStatement.ledger.balance'), { align: 'right' })),
      align: 'right'
    },
    {
      key: 'flexGutter',
      label: '',
      minWidth: 1,
      hideable: false,
      reorderable: false,
      pinned: 'end',
      resizable: false,
      className: 'stmt-ledger-flex-col',
      labelClassName: 'stmt-ledger-flex-col'
    }
  ]
})

function lineSummary(row: FinanceReceivableStatementLine) {
  if (row.lineType === 'opening') return t('financeReceivableStatement.kpi.opening')
  if (row.lineType === 'receipt') {
    if (!row.docNo) return t('financeReceivableStatement.advanceWriteOff')
    return `${t('financeReceivableStatement.receiptWriteOff')} ${row.docNo}`
  }
  return row.summary || '—'
}

function displayDocNo(row: FinanceReceivableStatementLine) {
  if (row.lineType === 'opening') return ''
  if (row.lineType === 'receipt' && !row.docNo) return t('financeReceivableStatement.advanceDoc')
  return row.docNo || '—'
}

function formatLedgerDate(ymd: string) {
  if (/^\d{4}-\d{2}-\d{2}$/.test(ymd)) return ymd.slice(2)
  return ymd || '—'
}

function currentPeriod(): [string, string] {
  const qFrom = typeof route.query.from === 'string' ? route.query.from : ''
  const qTo = typeof route.query.to === 'string' ? route.query.to : ''
  if (qFrom && qTo) return [qFrom, qTo]
  return lastCalendarMonthRange()
}

function syncQuery(from: string, to: string, agingDate: string) {
  void router.replace({
    name: 'FinanceReceivableStatementDetail',
    params: route.params,
    query: { from, to, aging: agingDate }
  })
}

async function loadDetail() {
  const customerId = String(route.params.customerId || '')
  const currency = Number(route.params.currency)
  if (!customerId || !Number.isFinite(currency)) {
    errorMsg.value = t('financeReceivableStatement.notFound')
    return
  }
  const from = periodFrom.value || currentPeriod()[0]
  const to = periodTo.value || currentPeriod()[1]
  periodFrom.value = from
  periodTo.value = to
  aging.value = aging.value || (typeof route.query.aging === 'string' && route.query.aging ? route.query.aging : todayYmd())
  loading.value = true
  errorMsg.value = ''
  try {
    detail.value = await financeReceivableStatementApi.getDetail(customerId, currency, {
      from,
      to,
      aging: aging.value
    })
  } catch (e) {
    detail.value = null
    errorMsg.value = getApiErrorMessage(e, t('financeReceivableStatement.notFound'))
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  if (!periodFrom.value || !periodTo.value) {
    ElMessage.error(t('financeReceivableStatement.invalidPeriod'))
    return
  }
  if (periodFrom.value > periodTo.value) {
    ElMessage.error(t('financeReceivableStatement.invalidPeriod'))
    return
  }
  if (!aging.value) aging.value = todayYmd()
  syncQuery(periodFrom.value, periodTo.value, aging.value)
  void loadDetail()
}

function handleReset() {
  const [from, to] = lastCalendarMonthRange()
  periodFrom.value = from
  periodTo.value = to
  aging.value = todayYmd()
  syncQuery(from, to, aging.value)
  void loadDetail()
}

function goBack() {
  void router.push({ name: 'FinanceReceivableStatementList' })
}

function goPreview() {
  const from = periodFrom.value || currentPeriod()[0]
  const to = periodTo.value || currentPeriod()[1]
  void router.push({
    name: 'FinanceReceivableStatementPreview',
    params: route.params,
    query: { from, to, aging: aging.value || todayYmd() }
  })
}

function onRowDblClick(row: LedgerRow) {
  if (row.lineType === 'increase' && row.receivableId) {
    void router.push(`/finance/receivables/${row.receivableId}`)
    return
  }
  if (row.lineType === 'receipt' && row.receiptId) {
    void router.push(`/finance/receipts/${row.receiptId}`)
  }
}

onMounted(() => {
  const [from, to] = currentPeriod()
  periodFrom.value = from
  periodTo.value = to
  aging.value = typeof route.query.aging === 'string' && route.query.aging ? route.query.aging : todayYmd()
  void loadDetail()
})
</script>

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';
</style>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/business-detail-info-grid.scss';

.stmt-alert {
  margin-bottom: 12px;
}

.stmt-header-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.stmt-header-badge__sep {
  color: $text-muted;
}

.date-range-group {
  display: inline-flex;
  align-items: center;
  flex-wrap: nowrap;
}

.date-range-sep {
  padding: 0 8px;
  color: $text-muted;
  font-size: 13px;
  flex-shrink: 0;
}

.filter-date {
  width: 140px;

  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
}

.section-title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  margin-left: auto;
}

.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;

  &__label {
    color: $text-muted;

    &::after {
      content: '：';
    }
  }

  &__value {
    color: $text-secondary;
  }
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.45);
  }
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);
}

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    font-size: 12px;

    &::after {
      content: '：';
    }
  }

  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }
}

.info-grid--basic {
  .info-item {
    &:nth-child(3n) {
      border-right: none;
    }

    &:nth-last-child(-n + 3) {
      border-bottom: none;
    }
  }

  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;
}

.stat-cards {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 12px;
}

.stat-card {
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 20px;
  text-align: center;
}

.stat-label {
  font-size: 12px;
  color: $text-muted;
}

.stat-value {
  margin-bottom: 5px;
  font-variant-numeric: tabular-nums;
  font-size: 22px;
  font-weight: 700;
  color: $text-primary;
  font-family: 'Noto Sans SC', sans-serif;

  :deep(.dock-tier-price-line) {
    display: inline-flex;
    justify-content: center;
    width: auto;
    font-size: inherit;
  }

  :deep(.dock-tier-amt-int),
  :deep(.dock-tier-amt-frac) {
    font-size: 22px;
    font-weight: 700;
  }
}

.stat-value--recv :deep(.dock-tier-amt),
.amt-recv :deep(.dock-tier-amt) {
  color: #2f9e44;
}

.stat-value--end :deep(.dock-tier-amt),
.amt-end :deep(.dock-tier-amt) {
  color: #c0392b;
}

.table-stack {
  display: flex;
  flex-direction: column-reverse;
}

.table-stack .pagination-wrapper {
  margin-top: 12px;
}

:deep(.stmt-ledger-flex-col .cell) {
  padding: 0;
}

.link-text {
  color: var(--el-color-primary);
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
