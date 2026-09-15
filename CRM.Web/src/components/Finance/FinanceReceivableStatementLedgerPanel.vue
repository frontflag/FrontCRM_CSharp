<template>
  <div class="crm-biz-list-page stmt-ledger-panel" v-loading="embedLoading && loading">
    <el-alert v-if="showErrorAlert && errorMsg && !detail" :title="errorMsg" type="error" show-icon class="stmt-alert" />

    <div v-if="showMetaHeader && detail" class="stmt-embed-header">
      <div class="section-header__meta">
        <button
          v-if="showPreview"
          type="button"
          class="btn-primary btn-sm stmt-embed-header__preview"
          :disabled="!detail"
          @click="goPreview"
        >
          {{ t('financeReceivableStatement.previewReport') }}
        </button>
        <span class="section-header-meta-item">
          <span class="section-header-meta-item__label">{{ t('financeReceivableStatement.fields.generatedOn') }}</span>
          <span class="section-header-meta-item__value">{{ detail.statement.generatedOn }}</span>
        </span>
        <span class="section-header-meta-item">
          <span class="section-header-meta-item__label">{{ t('financeReceivableStatement.fields.currency') }}</span>
          <span class="section-header-meta-item__value">
            <el-select
              v-if="allowCurrencySwitch && currencyOptions.length > 1"
              :model-value="activeCurrency"
              size="small"
              class="stmt-currency-select"
              @update:model-value="onCurrencyChange"
            >
              <el-option
                v-for="code in currencyOptions"
                :key="code"
                :label="listAmountCurrencyIso(code)"
                :value="code"
              />
            </el-select>
            <span v-else :class="['dock-tier-ccy', listAmountCurrencyDockClass(detail.statement.currency)]">
              {{ listAmountCurrencyIso(detail.statement.currency) }}
            </span>
          </span>
        </span>
      </div>
    </div>

    <template v-if="detail">
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
import { computed, onMounted, ref, watch } from 'vue'
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
import { listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { lastCalendarMonthRange, todayYmd } from '@/utils/receivableStatementPeriod'
import MoneyCell from '@/views/Finance/ReceivableStatementMoneyCell.vue'

type LedgerRow = FinanceReceivableStatementLine & { rowKey: string }

const props = withDefaults(
  defineProps<{
    customerId: string
    currency: number
    customerCode?: string
    showMetaHeader?: boolean
    showPreview?: boolean
    allowCurrencySwitch?: boolean
    embedLoading?: boolean
    showErrorAlert?: boolean
  }>(),
  {
    customerCode: '',
    showMetaHeader: false,
    showPreview: false,
    allowCurrencySwitch: false,
    embedLoading: true,
    showErrorAlert: true
  }
)

const emit = defineEmits<{
  'update:detail': [value: FinanceReceivableStatementDetail | null]
  'update:loading': [value: boolean]
  'update:error': [value: string]
}>()

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
const activeCurrency = ref(props.currency)
const currencyOptions = ref<number[]>([])
let currencyResolved = false
let panelStarted = false

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
    query: { ...route.query, from, to, aging: agingDate }
  })
}

function setLoading(value: boolean) {
  loading.value = value
  emit('update:loading', value)
}

function setError(value: string) {
  errorMsg.value = value
  emit('update:error', value)
}

function setDetail(value: FinanceReceivableStatementDetail | null) {
  detail.value = value
  emit('update:detail', value)
}

async function resolveCurrencyIfNeeded() {
  if (!props.allowCurrencySwitch) {
    activeCurrency.value = props.currency
    return
  }
  if (currencyResolved) return
  currencyResolved = true
  const q = Number(typeof route.query.stmtCurrency === 'string' ? route.query.stmtCurrency : NaN)
  if (q >= 1 && q <= 6) {
    activeCurrency.value = q
  } else {
    activeCurrency.value = props.currency
  }
  const code = props.customerCode.trim()
  if (!code || !props.customerId) return
  try {
    const data = await financeReceivableStatementApi.getPaged({
      keyword: code,
      page: 1,
      pageSize: 50
    })
    const mine = (data.items ?? []).filter(
      (i) => i.customerId.toLowerCase() === props.customerId.toLowerCase()
    )
    const codes = [...new Set(mine.map((i) => i.currency))]
    currencyOptions.value = codes
    if (mine.length && !mine.some((i) => i.currency === activeCurrency.value)) {
      mine.sort((a, b) => b.verifiedToBe - a.verifiedToBe)
      activeCurrency.value = mine[0].currency
    }
  } catch {
    currencyOptions.value = []
  }
}

async function loadDetail() {
  const customerId = props.customerId
  const currency = activeCurrency.value
  if (!customerId || !Number.isFinite(currency) || currency < 1) {
    setError(t('financeReceivableStatement.notFound'))
    setDetail(null)
    return
  }
  const from = periodFrom.value || currentPeriod()[0]
  const to = periodTo.value || currentPeriod()[1]
  periodFrom.value = from
  periodTo.value = to
  aging.value = aging.value || (typeof route.query.aging === 'string' && route.query.aging ? route.query.aging : todayYmd())
  setLoading(true)
  setError('')
  try {
    setDetail(
      await financeReceivableStatementApi.getDetail(customerId, currency, {
        from,
        to,
        aging: aging.value
      })
    )
  } catch (e) {
    setDetail(null)
    setError(getApiErrorMessage(e, t('financeReceivableStatement.notFound')))
  } finally {
    setLoading(false)
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

function goPreview() {
  const from = periodFrom.value || currentPeriod()[0]
  const to = periodTo.value || currentPeriod()[1]
  void router.push({
    name: 'FinanceReceivableStatementPreview',
    params: { customerId: props.customerId, currency: String(activeCurrency.value) },
    query: { from, to, aging: aging.value || todayYmd() }
  })
}

function onCurrencyChange(value: string | number) {
  const next = Number(value)
  if (!Number.isFinite(next) || next === activeCurrency.value) return
  activeCurrency.value = next
  void router.replace({
    query: { ...route.query, stmtCurrency: String(next) }
  })
  void loadDetail()
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

onMounted(async () => {
  const [from, to] = currentPeriod()
  periodFrom.value = from
  periodTo.value = to
  aging.value = typeof route.query.aging === 'string' && route.query.aging ? route.query.aging : todayYmd()
  await resolveCurrencyIfNeeded()
  await loadDetail()
  panelStarted = true
})

watch(
  () => [props.customerId, props.currency] as const,
  async ([id, currency], [prevId]) => {
    if (!panelStarted || !id) return
    if (id !== prevId) currencyResolved = false
    if (!props.allowCurrencySwitch) activeCurrency.value = currency
    await resolveCurrencyIfNeeded()
    await loadDetail()
  }
)

defineExpose({ goPreview })
</script>

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';
</style>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.stmt-alert {
  margin-bottom: 12px;
}

.stmt-ledger-panel {
  padding: 0;
  min-height: 0;
  background: transparent;
}

.stmt-embed-header {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  margin-bottom: 12px;
}

.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
}

.stmt-embed-header__preview {
  flex-shrink: 0;
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

.stmt-currency-select {
  width: 92px;
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
