<template>
  <div class="finance-page write-off-page" :class="{ 'write-off-page--embedded': embedded }">
    <div
      ref="layoutRef"
      class="write-off-layout"
      :class="{ 'is-resizing': !!dragging, 'write-off-layout--embedded': embedded }"
      v-loading="loading"
    >
      <div
        ref="mainPairRef"
        class="write-off-main-pair"
        :class="panelsLayout === 'column' ? 'write-off-main-pair--column' : 'write-off-main-pair--row'"
      >
        <section class="info-section write-off-panel write-off-panel--center" :style="centerPanelStyle">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('sellInvoiceWriteOffWorkspace.invoicePanelTitle') }}</span>
              <span v-if="customerId && invoices.length" class="section-count">{{ invoices.length }}</span>
            </div>
            <div
              class="section-header__layout"
              role="group"
              :aria-label="t('sellInvoiceWriteOffWorkspace.panelsLayoutGroup')"
            >
              <el-tooltip
                :content="t('sellInvoiceWriteOffWorkspace.panelsLayoutRow')"
                placement="top"
                :hide-after="0"
              >
                <el-button
                  class="panels-layout-btn"
                  :class="{ 'is-active': panelsLayout === 'row' }"
                  text
                  circle
                  size="small"
                  :aria-pressed="panelsLayout === 'row'"
                  @click="setPanelsLayout('row')"
                >
                  <svg class="panels-layout-icon" viewBox="0 0 24 24" width="16" height="16" aria-hidden="true">
                    <rect x="4" y="3" width="6" height="18" rx="1.2" fill="currentColor" />
                    <rect x="14" y="3" width="6" height="18" rx="1.2" fill="currentColor" />
                  </svg>
                </el-button>
              </el-tooltip>
              <el-tooltip
                :content="t('sellInvoiceWriteOffWorkspace.panelsLayoutColumn')"
                placement="top"
                :hide-after="0"
              >
                <el-button
                  class="panels-layout-btn"
                  :class="{ 'is-active': panelsLayout === 'column' }"
                  text
                  circle
                  size="small"
                  :aria-pressed="panelsLayout === 'column'"
                  @click="setPanelsLayout('column')"
                >
                  <svg class="panels-layout-icon" viewBox="0 0 24 24" width="16" height="16" aria-hidden="true">
                    <rect x="3" y="4" width="18" height="6" rx="1.2" fill="currentColor" />
                    <rect x="3" y="14" width="18" height="6" rx="1.2" fill="currentColor" />
                  </svg>
                </el-button>
              </el-tooltip>
            </div>
          </div>
          <div class="detail-panel-section-body write-off-panel__body">
            <div class="panel-info-bar">
              <span class="panel-info-bar__label">{{ t('sellInvoiceWriteOffWorkspace.currentCustomerLabel') }}</span>
              <template v-if="!customerSummary">
                <span class="panel-info-bar__hint panel-info-bar__hint--placeholder">
                  {{ t('sellInvoiceWriteOffWorkspace.currentCustomerHint') }}
                </span>
              </template>
              <template v-else>
                <span class="panel-info-bar__customer">
                  <span class="panel-hint__value">{{ customerNameZh }}</span>
                  <span v-if="customerNameEn" class="panel-hint__sep"> / </span>
                  <span v-if="customerNameEn" class="panel-hint__value">{{ customerNameEn }}</span>
                </span>
                <span class="panel-info-bar__currency"
                  >（{{ selectedCurrency != null ? currencyLabel(selectedCurrency) : '—' }}）</span
                >
              </template>
            </div>
            <div class="detail-items-table-wrap">
              <DetailListPanelEmpty
                v-if="!customerId"
                size="medium"
                :description="t('sellInvoiceWriteOffWorkspace.selectCustomerHint')"
              />
              <DetailListPanelEmpty
                v-else-if="!invoices.length"
                size="medium"
                :description="t('sellInvoiceWriteOffWorkspace.noInvoices')"
              />
              <el-table
                v-else
                :data="invoices"
                class="detail-panel-list-table invoice-panel-table"
                :border="false"
                size="small"
                stripe
                height="100%"
                :row-class-name="invoiceRowClassName"
                row-key="id"
                @row-click="onInvoiceRowSelect"
              >
                <el-table-column
                  :label="t('sellInvoiceWriteOffWorkspace.colInvoiceCode')"
                  min-width="118"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">
                    <router-link
                      v-if="row.id"
                      class="link-text"
                      :to="{ name: 'FinanceSellInvoiceDetail', params: { id: row.id } }"
                      @click.stop
                    >
                      {{ row.invoiceCode || row.id }}
                    </router-link>
                    <span v-else>{{ row.invoiceCode || '—' }}</span>
                  </template>
                </el-table-column>
                <el-table-column :label="t('sellInvoiceWriteOffWorkspace.colInvoiceDate')" width="108">
                  <template #default="{ row }">{{ formatDate(row.invoiceDate) }}</template>
                </el-table-column>
                <el-table-column :label="t('sellInvoiceWriteOffWorkspace.colInvoiceType')" width="120">
                  <template #default="{ row }">{{ invoiceTypeLabelForRow(row) }}</template>
                </el-table-column>
                <el-table-column
                  :label="t('sellInvoiceWriteOffWorkspace.colInvoiceAmount')"
                  width="132"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    <span class="finance-amount-emphasis finance-amount-emphasis--amount">
                      {{ formatAmountWithCurrency(row.invoiceAmount, row.currency) }}
                    </span>
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('sellInvoiceWriteOffWorkspace.colMatchDone')"
                  width="132"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    <span class="finance-amount-emphasis finance-amount-emphasis--verified">
                      {{ formatAmountWithCurrency(row.matchDone, row.currency) }}
                    </span>
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('sellInvoiceWriteOffWorkspace.colMatchToBe')"
                  width="132"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    <span class="finance-amount-emphasis finance-amount-emphasis--pending">
                      {{ formatAmountWithCurrency(row.matchToBe, row.currency) }}
                    </span>
                  </template>
                </el-table-column>
                <el-table-column
                  fixed="right"
                  width="52"
                  class-name="customer-row-select-col"
                  label-class-name="customer-row-select-col"
                >
                  <template #header><span class="customer-row-select-col__head" /></template>
                  <template #default="{ row }">
                    <div class="customer-row-select-action" @click.stop>
                      <el-button
                        size="small"
                        link
                        type="primary"
                        class="customer-row-select-btn"
                        @click="onInvoiceRowSelect(row)"
                      >
                        {{ t('sellInvoiceWriteOffWorkspace.selectRow') }}
                      </el-button>
                    </div>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </section>

        <div
          class="pair-splitbar"
          :class="{
            'pair-splitbar--col': panelsLayout === 'row',
            'pair-splitbar--row': panelsLayout === 'column',
            'is-dragging': dragging
          }"
          :title="
            panelsLayout === 'column'
              ? t('sellInvoiceWriteOffWorkspace.dragCenterHeight')
              : t('sellInvoiceWriteOffWorkspace.dragCenterWidth')
          "
          @mousedown="onSplitStart($event)"
        />

        <section class="info-section write-off-panel write-off-panel--right">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('sellInvoiceWriteOffWorkspace.receivablePanelTitle') }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body write-off-panel__body">
            <div v-if="customerId" class="panel-info-bar panel-info-bar--with-total">
              <span class="panel-info-bar__main">
                <template v-if="selectedInvoice">
                  {{
                    t('sellInvoiceWriteOffWorkspace.selectedInvoiceInfoBarPrefix', {
                      invoiceCode: selectedInvoice.invoiceCode || selectedInvoice.invoiceNo || '—'
                    })
                  }}<span class="panel-info-bar__highlight-amount">{{
                    formatAmountWithCurrency(selectedInvoice.matchToBe, selectedInvoice.currency)
                  }}</span>
                </template>
                <template v-else>
                  <span class="panel-info-bar__hint panel-info-bar__hint--placeholder">
                    {{ t('sellInvoiceWriteOffWorkspace.selectInvoiceHint') }}
                  </span>
                </template>
              </span>
              <div v-if="selectedInvoice" class="panel-info-bar__actions">
                <span class="panel-info-bar__total" :class="writeOffTotalColorClass">
                  {{
                    t('sellInvoiceWriteOffWorkspace.writeOffTotalLabel', {
                      total: formatAmountWithCurrency(writeOffAmountTotal, selectedInvoice.currency)
                    })
                  }}
                </span>
                <el-button type="primary" link size="small" @click="autoFillWriteOffAmounts">
                  {{ t('sellInvoiceWriteOffWorkspace.autoFill') }}
                </el-button>
              </div>
            </div>
            <div class="receivable-main" :class="{ 'is-overflow': receivableListOverflow }">
              <div ref="receivableScrollRef" class="receivable-scroll">
                <div class="detail-items-table-wrap detail-items-table-wrap--receivable">
                  <DetailListPanelEmpty
                    v-if="!customerId"
                    size="medium"
                    :description="t('sellInvoiceWriteOffWorkspace.selectCustomerHint')"
                  />
                  <DetailListPanelEmpty
                    v-else-if="!receivableUiRows.length"
                    size="medium"
                    :description="t('sellInvoiceWriteOffWorkspace.noReceivables')"
                  />
                  <el-table
                    v-else
                    :data="receivableUiRows"
                    class="detail-panel-list-table receivable-panel-table"
                    size="small"
                    stripe
                    :highlight-current-row="embedded"
                    :row-class-name="receivableRowClassName"
                    row-key="financeReceivableId"
                    @row-click="onReceivableRowClick"
                  >
                    <el-table-column
                      prop="receivableCode"
                      :label="t('sellInvoiceWriteOffWorkspace.colReceivableCode')"
                      min-width="118"
                      show-overflow-tooltip
                    >
                      <template #default="{ row }">
                        <span class="code-text">{{ row.receivableCode || '—' }}</span>
                      </template>
                    </el-table-column>
                    <el-table-column
                      prop="stockOutCode"
                      :label="t('sellInvoiceWriteOffWorkspace.colStockOutCode')"
                      min-width="118"
                      show-overflow-tooltip
                    >
                      <template #default="{ row }">
                        <router-link
                          v-if="row.stockOutId && row.stockOutCode"
                          class="link-text"
                          :to="{ name: 'StockOutDetail', params: { id: row.stockOutId } }"
                          @click.stop
                        >
                          {{ row.stockOutCode }}
                        </router-link>
                        <span v-else>{{ row.stockOutCode || '—' }}</span>
                      </template>
                    </el-table-column>
                    <el-table-column :label="t('sellInvoiceWriteOffWorkspace.colStockOutDate')" width="108">
                      <template #default="{ row }">{{ formatDate(row.stockOutDate) }}</template>
                    </el-table-column>
                    <el-table-column
                      prop="sellOrderCode"
                      :label="t('sellInvoiceWriteOffWorkspace.colSellOrderCode')"
                      min-width="118"
                      show-overflow-tooltip
                    >
                      <template #default="{ row }">{{ row.sellOrderCode || '—' }}</template>
                    </el-table-column>
                    <el-table-column
                      prop="salesUserName"
                      :label="t('sellInvoiceWriteOffWorkspace.colSalesUser')"
                      width="96"
                      show-overflow-tooltip
                    >
                      <template #default="{ row }">{{ row.salesUserName || '—' }}</template>
                    </el-table-column>
                    <el-table-column
                      prop="freightForwarderOrderNo"
                      :label="t('sellInvoiceWriteOffWorkspace.colFreightForwarder')"
                      min-width="110"
                      show-overflow-tooltip
                    >
                      <template #default="{ row }">{{ row.freightForwarderOrderNo || '—' }}</template>
                    </el-table-column>
                    <el-table-column
                      prop="stockInCode"
                      :label="t('sellInvoiceWriteOffWorkspace.colStockInCode')"
                      min-width="110"
                      show-overflow-tooltip
                    >
                      <template #default="{ row }">{{ row.stockInCode || '—' }}</template>
                    </el-table-column>
                    <el-table-column
                      :label="t('sellInvoiceWriteOffWorkspace.colInvoiceMatchToBe')"
                      width="132"
                      min-width="130"
                      align="right"
                      header-align="right"
                    >
                      <template #default="{ row }">
                        <span class="finance-amount-emphasis finance-amount-emphasis--amount">
                          {{ formatAmountWithCurrency(row.invoiceMatchToBe, row.currency) }}
                        </span>
                      </template>
                    </el-table-column>
                    <el-table-column
                      fixed="right"
                      :label="t('sellInvoiceWriteOffWorkspace.colWriteOffAmount')"
                      width="148"
                      min-width="148"
                      align="center"
                      header-align="center"
                      class-name="write-off-amount-col"
                      label-class-name="write-off-amount-col"
                    >
                      <template #default="{ row }">
                        <el-input-number
                          v-model="row.writeOffAmount"
                          :class="[
                            'write-off-amount-input',
                            { 'write-off-amount-input--positive': isWriteOffAmountPositive(row.writeOffAmount) }
                          ]"
                          :min="0"
                          :max="row.invoiceMatchToBe"
                          :precision="2"
                          :controls="false"
                          size="small"
                          @click.stop
                        />
                      </template>
                    </el-table-column>
                  </el-table>
                </div>
              </div>
              <div v-if="customerId && canWriteFinanceSellInvoice" class="receivable-submit-bar">
                <el-button type="primary" :loading="submitting" @click="submitWriteOff">
                  {{ t('sellInvoiceWriteOffWorkspace.submit') }}
                </el-button>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  financeSellInvoiceWriteOffApi,
  type FinanceSellInvoiceWriteOffCustomerSummary,
  type FinanceSellInvoiceWriteOffInvoiceRow,
  type FinanceSellInvoiceWriteOffReceivableRow
} from '@/api/financeSellInvoiceWriteOff'
import { CURRENCY_MAP } from '@/api/finance'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useSellInvoiceWriteOffDesktopQueueStore } from '@/stores/sellInvoiceWriteOffDesktopQueue'
import {
  readSellInvoiceWriteOffPanelsLayout,
  writeSellInvoiceWriteOffPanelsLayout,
  type SellInvoiceWriteOffPanelsLayout
} from '@/utils/sellInvoiceWriteOffPanelsLayout'

type ReceivableUiRow = FinanceSellInvoiceWriteOffReceivableRow & {
  writeOffAmount: number
}

const props = withDefaults(
  defineProps<{
    embedded?: boolean
    embedCustomerId?: string
    embedCurrency?: number | null
    embedSummary?: FinanceSellInvoiceWriteOffCustomerSummary | null
  }>(),
  {
    embedded: false,
    embedCustomerId: '',
    embedCurrency: null,
    embedSummary: null
  }
)

const emit = defineEmits<{
  applied: []
}>()

const { t } = useI18n()
const queueStore = useSellInvoiceWriteOffDesktopQueueStore()
const { canWriteFinanceSellInvoice } = useFinanceWriteGate()
const { sellInvoiceTypeLabel } = useFinanceEnumLabels()

const layoutRef = ref<HTMLElement | null>(null)
const mainPairRef = ref<HTMLElement | null>(null)
const receivableScrollRef = ref<HTMLElement | null>(null)
const centerPanelSizePx = ref(0)
const dragging = ref(false)
const panelsLayout = ref<SellInvoiceWriteOffPanelsLayout>(readSellInvoiceWriteOffPanelsLayout())

const SPLIT_BAR_WIDTH = 6
const MIN_PANEL_WIDTH = 180
const MIN_PANEL_HEIGHT = 140
const DEFAULT_CENTER_RATIO = 0.5

let dragStartX = 0
let dragStartY = 0
let dragStartCenter = 0

const loading = ref(false)
const submitting = ref(false)
const customerId = ref('')
const selectedCurrency = ref<number | null>(null)
const customerSummary = ref<FinanceSellInvoiceWriteOffCustomerSummary | null>(null)
const invoices = ref<FinanceSellInvoiceWriteOffInvoiceRow[]>([])
const receivableUiRows = ref<ReceivableUiRow[]>([])
const selectedInvoiceId = ref('')
const focusedReceivableId = ref('')
const receivableListOverflow = ref(false)
let receivableScrollResizeObserver: ResizeObserver | null = null

const customerNameZh = computed(
  () => customerSummary.value?.customerName?.trim() || customerSummary.value?.customerId || '—'
)
const customerNameEn = computed(() => customerSummary.value?.customerEnglishName?.trim() || '')

const selectedInvoice = computed(
  () => invoices.value.find((x) => x.id === selectedInvoiceId.value) ?? null
)

const writeOffAmountTotal = computed(() =>
  receivableUiRows.value.reduce((sum, row) => sum + (Number(row.writeOffAmount) || 0), 0)
)

const writeOffTotalColorClass = computed(() => {
  const total = writeOffAmountTotal.value
  if (total <= 0.0001) return 'is-zero'
  const remaining = selectedInvoice.value?.matchToBe ?? 0
  if (total > remaining + 0.001) return 'is-over'
  return 'is-positive'
})

function clamp(n: number, min: number, max: number) {
  return Math.min(max, Math.max(min, n))
}

function isColumnLayout() {
  return panelsLayout.value === 'column'
}

function usablePairSize() {
  const pair = mainPairRef.value
  if (!pair) return 0
  if (isColumnLayout()) {
    return Math.max(0, pair.clientHeight - SPLIT_BAR_WIDTH)
  }
  return Math.max(0, pair.clientWidth - SPLIT_BAR_WIDTH)
}

function centerMinSize() {
  return isColumnLayout() ? MIN_PANEL_HEIGHT : MIN_PANEL_WIDTH
}

function initPanelSizes() {
  void nextTick(() => {
    const usablePair = usablePairSize()
    if (usablePair <= 0) return
    const min = centerMinSize()
    const half = Math.round(usablePair * DEFAULT_CENTER_RATIO)
    centerPanelSizePx.value = clamp(half, min, Math.max(min, usablePair - min))
  })
}

function setPanelsLayout(layout: SellInvoiceWriteOffPanelsLayout) {
  if (panelsLayout.value === layout) return
  panelsLayout.value = layout
  writeSellInvoiceWriteOffPanelsLayout(layout)
  centerPanelSizePx.value = 0
  void nextTick(() => {
    initPanelSizes()
    updateReceivableListOverflow()
  })
}

const centerPanelStyle = computed(() => {
  const size = centerPanelSizePx.value
  if (isColumnLayout()) {
    if (size <= 0) {
      return { flex: '1 1 0', width: '100%', minHeight: `${MIN_PANEL_HEIGHT}px` }
    }
    return {
      width: '100%',
      height: `${size}px`,
      flex: `0 0 ${size}px`,
      minHeight: `${MIN_PANEL_HEIGHT}px`
    }
  }
  if (size <= 0) {
    return { flex: '1 1 0', width: 'auto', minWidth: `${MIN_PANEL_WIDTH}px` }
  }
  return { width: `${size}px`, flex: `0 0 ${size}px`, minWidth: `${MIN_PANEL_WIDTH}px` }
})

function onSplitStart(e: MouseEvent) {
  e.preventDefault()
  if (centerPanelSizePx.value <= 0) initPanelSizes()
  dragging.value = true
  dragStartX = e.clientX
  dragStartY = e.clientY
  dragStartCenter = centerPanelSizePx.value
  document.body.style.cursor = isColumnLayout() ? 'row-resize' : 'col-resize'
  document.body.style.userSelect = 'none'
}

function onSplitMove(e: MouseEvent) {
  if (!dragging.value) return
  const min = centerMinSize()
  const usablePair = usablePairSize()
  const delta = isColumnLayout() ? e.clientY - dragStartY : e.clientX - dragStartX
  const pair = mainPairRef.value
  const pairTotal = isColumnLayout()
    ? (pair?.clientHeight ?? 0) - SPLIT_BAR_WIDTH
    : (pair?.clientWidth ?? 0) - SPLIT_BAR_WIDTH
  const max = Math.max(min, (pairTotal || usablePair) - min)
  centerPanelSizePx.value = clamp(dragStartCenter + delta, min, max)
}

function onSplitEnd() {
  if (!dragging.value) return
  dragging.value = false
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
  void nextTick(() => updateReceivableListOverflow())
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return String(v).slice(0, 10)
}

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatAmountWithCurrency(amount?: number, currency?: number) {
  if (amount == null) return '—'
  if (currency == null) return formatAmount(amount)
  return `${formatAmount(amount)} ${currencyLabel(currency)}`
}

function currencyLabel(currency: number) {
  return CURRENCY_MAP[currency] ?? String(currency)
}

function isWriteOffAmountPositive(amount?: number) {
  return (Number(amount) || 0) > 0.0001
}

function invoiceTypeLabelForRow(row: FinanceSellInvoiceWriteOffInvoiceRow) {
  return sellInvoiceTypeLabel(row.sellInvoiceType)
}

function invoiceRowClassName({ row }: { row: FinanceSellInvoiceWriteOffInvoiceRow }) {
  return row.id === selectedInvoiceId.value ? 'is-write-off-receipt-selected' : ''
}

function receivableRowClassName({ row }: { row: ReceivableUiRow }) {
  return row.financeReceivableId === focusedReceivableId.value ? 'is-siwo-receivable-focused' : ''
}

function onInvoiceRowSelect(row: FinanceSellInvoiceWriteOffInvoiceRow) {
  selectedInvoiceId.value = row.id
  clearWriteOffAmounts()
}

function onReceivableRowClick(row: ReceivableUiRow) {
  focusReceivable(row)
}

function mapReceivables(rows: FinanceSellInvoiceWriteOffReceivableRow[]): ReceivableUiRow[] {
  return (rows ?? []).map((r) => ({ ...r, writeOffAmount: 0 }))
}

function clearWriteOffAmounts() {
  for (const row of receivableUiRows.value) {
    row.writeOffAmount = 0
  }
}

function focusReceivable(row: ReceivableUiRow) {
  focusedReceivableId.value = row.financeReceivableId
  if (props.embedded) {
    queueStore.setFocusedReceivable(row)
  }
}

function pickDefaultFocusedReceivable() {
  const rows = receivableUiRows.value
  if (!rows.length) {
    focusedReceivableId.value = ''
    if (props.embedded) queueStore.setFocusedReceivable(null)
    return
  }
  const still = focusedReceivableId.value
    ? rows.find((r) => r.financeReceivableId === focusedReceivableId.value)
    : undefined
  const next = still ?? rows[0]
  focusReceivable(next)
}

function updateReceivableListOverflow() {
  const scroll = receivableScrollRef.value
  const main = scroll?.parentElement
  if (!scroll || !main) {
    receivableListOverflow.value = false
    return
  }
  const submit = main.querySelector('.receivable-submit-bar') as HTMLElement | null
  const submitH = submit ? submit.offsetHeight + 10 : 0
  const available = Math.max(0, main.clientHeight - submitH)
  receivableListOverflow.value = scroll.scrollHeight > available + 1
}

function setupReceivableScrollObserver() {
  receivableScrollResizeObserver?.disconnect()
  receivableScrollResizeObserver = null
  const scroll = receivableScrollRef.value
  const main = scroll?.parentElement
  if (!scroll || typeof ResizeObserver === 'undefined') return
  receivableScrollResizeObserver = new ResizeObserver(() => {
    void nextTick(() => updateReceivableListOverflow())
  })
  receivableScrollResizeObserver.observe(scroll)
  if (main) receivableScrollResizeObserver.observe(main)
  void nextTick(() => updateReceivableListOverflow())
}

async function loadCandidates(cid: string, currency: number, resetInvoiceSelection: boolean) {
  customerId.value = cid
  selectedCurrency.value = currency
  if (props.embedSummary && props.embedSummary.customerId === cid) {
    customerSummary.value = props.embedSummary
  }
  loading.value = true
  try {
    const res = await financeSellInvoiceWriteOffApi.getCandidates(cid, currency)
    customerSummary.value = {
      customerId: res.customerId,
      customerName: res.customerName,
      customerEnglishName: res.customerEnglishName,
      currency: res.currency,
      pendingWriteOffTotal: 0,
      pendingInvoiceCount: res.invoices?.length ?? 0,
      hasOpenReceivable: (res.receivables?.length ?? 0) > 0
    }
    invoices.value = res.invoices ?? []
    receivableUiRows.value = mapReceivables(res.receivables ?? [])

    if (resetInvoiceSelection || !invoices.value.some((x) => x.id === selectedInvoiceId.value)) {
      selectedInvoiceId.value = invoices.value[0]?.id ?? ''
    }
    clearWriteOffAmounts()
  } finally {
    loading.value = false
    void nextTick(() => {
      pickDefaultFocusedReceivable()
      setupReceivableScrollObserver()
      updateReceivableListOverflow()
    })
  }
}

async function syncEmbeddedSelection(resetInvoiceSelection: boolean) {
  const cid = (props.embedCustomerId || '').trim()
  if (!cid || props.embedCurrency == null) {
    customerId.value = ''
    customerSummary.value = null
    invoices.value = []
    receivableUiRows.value = []
    selectedInvoiceId.value = ''
    if (props.embedded) queueStore.setFocusedReceivable(null)
    return
  }
  if (props.embedSummary && props.embedSummary.customerId === cid) {
    customerSummary.value = props.embedSummary
  }
  await loadCandidates(cid, props.embedCurrency, resetInvoiceSelection)
}

function roundMoney(v: number) {
  return Math.round(v * 100) / 100
}

function autoFillWriteOffAmounts() {
  const invoice = selectedInvoice.value
  if (!invoice) {
    ElMessage.warning(t('sellInvoiceWriteOffWorkspace.selectInvoiceFirst'))
    return
  }
  let budget = roundMoney(invoice.matchToBe ?? 0)
  if (budget <= 0) {
    ElMessage.warning(t('sellInvoiceWriteOffWorkspace.autoFillNoRemaining'))
    return
  }
  clearWriteOffAmounts()
  for (const row of receivableUiRows.value) {
    if (budget <= 0.001) break
    const fillAmount = roundMoney(Math.min(row.invoiceMatchToBe ?? 0, budget))
    row.writeOffAmount = fillAmount
    budget = roundMoney(budget - fillAmount)
  }
}

function buildAllocations() {
  const allocations: { financeReceivableId: string; amount: number }[] = []
  for (const row of receivableUiRows.value) {
    const amount = Number(row.writeOffAmount) || 0
    if (amount > 0.0001) {
      allocations.push({ financeReceivableId: row.financeReceivableId, amount })
    }
  }
  return allocations
}

function validatePayload() {
  const invoice = selectedInvoice.value
  if (!invoice) {
    ElMessage.warning(t('sellInvoiceWriteOffWorkspace.selectInvoiceFirst'))
    return null
  }
  const allocations = buildAllocations()
  if (!allocations.length) {
    ElMessage.warning(t('sellInvoiceWriteOffWorkspace.noAmount'))
    return null
  }
  const total = allocations.reduce((s, a) => s + a.amount, 0)
  if (total > (invoice.matchToBe ?? 0) + 0.001) {
    ElMessage.warning(t('sellInvoiceWriteOffWorkspace.exceedInvoiceRemaining'))
    return null
  }
  for (const row of receivableUiRows.value) {
    const amount = Number(row.writeOffAmount) || 0
    if (amount > (row.invoiceMatchToBe ?? 0) + 0.001) {
      ElMessage.warning(t('sellInvoiceWriteOffWorkspace.exceedReceivable'))
      return null
    }
  }
  return { financeSellInvoiceId: invoice.id, allocations }
}

async function submitWriteOff() {
  const payload = validatePayload()
  if (!payload) return
  submitting.value = true
  try {
    await financeSellInvoiceWriteOffApi.apply(payload)
    ElMessage.success(t('sellInvoiceWriteOffWorkspace.success'))
    emit('applied')
    const cid = customerId.value
    const currency = selectedCurrency.value
    if (cid && currency != null) {
      await loadCandidates(cid, currency, false)
    }
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('sellInvoiceWriteOffWorkspace.failed')
    ElMessage.error(msg)
  } finally {
    submitting.value = false
  }
}

function onWindowResize() {
  initPanelSizes()
  void nextTick(() => updateReceivableListOverflow())
}

watch(
  () =>
    [
      props.embedded,
      props.embedCustomerId,
      props.embedCurrency,
      props.embedSummary
        ? `${props.embedSummary.customerId}::${props.embedSummary.currency ?? 0}`
        : ''
    ] as const,
  () => {
    if (!props.embedded) return
    void syncEmbeddedSelection(true)
  }
)

watch(
  () => receivableUiRows.value.map((r) => r.financeReceivableId).join('|'),
  () => {
    if (!props.embedded) return
    void nextTick(() => pickDefaultFocusedReceivable())
  }
)

watch(
  () => [receivableUiRows.value.length, customerId.value, !!selectedInvoice.value] as const,
  () => {
    void nextTick(() => {
      setupReceivableScrollObserver()
      updateReceivableListOverflow()
    })
  }
)

onMounted(() => {
  void nextTick(() => {
    initPanelSizes()
    setupReceivableScrollObserver()
    if (props.embedded) {
      void syncEmbeddedSelection(true)
    }
  })
  window.addEventListener('mousemove', onSplitMove)
  window.addEventListener('mouseup', onSplitEnd)
  window.addEventListener('resize', onWindowResize)
})

onBeforeUnmount(() => {
  receivableScrollResizeObserver?.disconnect()
  receivableScrollResizeObserver = null
  if (props.embedded) {
    queueStore.setFocusedReceivable(null)
  }
  window.removeEventListener('mousemove', onSplitMove)
  window.removeEventListener('mouseup', onSplitEnd)
  window.removeEventListener('resize', onWindowResize)
  onSplitEnd()
})
</script>

<style scoped lang="scss">
@import './finance-common.scss';

@mixin write-off-info-bar-surface {
  padding: 10px 14px;
  border-radius: 8px;
  border: 1px solid #f5e6a8;
  background: #fffaea;
  font-size: 14px;
  color: $text-primary;
  line-height: 1.5;
}

@mixin write-off-select-col-cell-surface {
  td.customer-row-select-col.el-table__cell,
  td.customer-row-select-col.el-table-fixed-column--right,
  .el-table__fixed-right td.customer-row-select-col.el-table__cell {
    padding: 0 !important;
    border-left: none !important;
    background: var(--crm-detail-panel-card-bg) !important;
    background-color: var(--crm-detail-panel-card-bg) !important;
    box-shadow: inset 1px 0 0 var(--crm-table-header-line);

    .cell {
      padding: 0 !important;
      overflow: visible;
    }
  }

  .el-table__body tr.el-table__row--striped td.customer-row-select-col.el-table__cell,
  .el-table__body tr.el-table__row--striped td.customer-row-select-col.el-table-fixed-column--right,
  .el-table__fixed-right .el-table__body tr.el-table__row--striped td.customer-row-select-col.el-table__cell {
    background: var(--crm-table-fixed-right-stripe, var(--crm-table-row-stripe)) !important;
    background-color: var(--crm-table-fixed-right-stripe, var(--crm-table-row-stripe)) !important;
  }

  .el-table__body tr:hover td.customer-row-select-col.el-table__cell,
  .el-table__body tr.hover-row td.customer-row-select-col.el-table__cell,
  .el-table__body tr:hover td.customer-row-select-col.el-table-fixed-column--right,
  .el-table__body tr.hover-row td.customer-row-select-col.el-table-fixed-column--right,
  .el-table__fixed-right .el-table__body tr:hover td.customer-row-select-col.el-table__cell,
  .el-table__fixed-right .el-table__body tr.hover-row td.customer-row-select-col.el-table__cell {
    background: var(--crm-table-row-hover) !important;
    background-color: var(--crm-table-row-hover) !important;
  }
}

.write-off-page {
  &.write-off-page--embedded {
    height: 100%;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 0;
  }

  .write-off-layout {
    display: flex;
    align-items: stretch;
    min-height: 520px;
    height: calc(100vh - 260px);
    max-height: 760px;

    &.is-resizing {
      user-select: none;
    }

    &.write-off-layout--embedded {
      flex: 1;
      min-height: 0;
      height: auto;
      max-height: none;
    }
  }

  .write-off-main-pair {
    flex: 1 1 auto;
    min-width: 0;
    min-height: 0;
    display: flex;
    align-items: stretch;

    &--row {
      flex-direction: row;
    }

    &--column {
      flex-direction: column;

      .write-off-panel--center,
      .write-off-panel--right {
        width: 100%;
      }

      .write-off-panel--right {
        flex: 1 1 auto;
        min-height: 0;
      }
    }

    &--row .write-off-panel--right {
      flex: 1 1 0;
      min-width: 0;
    }
  }

  .pair-splitbar {
    position: relative;
    z-index: 2;
    border-radius: 4px;
    background: transparent;
    transition: background 0.15s;
    align-self: stretch;

    &:hover,
    &.is-dragging {
      background: var(--crm-splitter-hover, rgba(64, 158, 255, 0.18));
    }
  }

  .pair-splitbar--col {
    flex: 0 0 6px;
    width: 6px;
    cursor: col-resize;

    &::after {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      width: 2px;
      height: 32px;
      transform: translate(-50%, -50%);
      border-radius: 1px;
      background: $border-panel;
      pointer-events: none;
    }
  }

  .pair-splitbar--row {
    flex: 0 0 6px;
    height: 6px;
    width: 100%;
    cursor: row-resize;

    &::after {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      width: 32px;
      height: 2px;
      transform: translate(-50%, -50%);
      border-radius: 1px;
      background: $border-panel;
      pointer-events: none;
    }
  }

  .write-off-panel {
    display: flex;
    flex-direction: column;
    min-width: 0;
    margin-bottom: 0;
    background: $layer-2;
    border: 1px solid $border-card;
    border-radius: $border-radius-lg;
    overflow: hidden;

    &--center {
      .detail-panel-section-body.write-off-panel__body {
        background: var(--crm-detail-panel-card-bg);
      }

      :deep(.invoice-panel-table) {
        .el-table__body tr.is-write-off-receipt-selected > td.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        .el-table__body tr.is-write-off-receipt-selected:hover > td.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        .el-table__body tr.is-write-off-receipt-selected > td.el-table__cell:first-child {
          position: relative;
          padding-left: 22px !important;

          &::before {
            content: '';
            position: absolute;
            left: 8px;
            top: 50%;
            transform: translateY(-50%);
            width: 0;
            height: 0;
            border-top: 5px solid transparent;
            border-bottom: 5px solid transparent;
            border-left: 7px solid var(--crm-list-row-indicator-color);
            pointer-events: none;
            z-index: 1;
          }

          .cell {
            padding-left: 0;
          }
        }

        th.customer-row-select-col.el-table__cell {
          padding: 0 !important;
          border-left: none !important;
          background: var(--crm-detail-panel-card-head-bg) !important;
          background-color: var(--crm-detail-panel-card-head-bg) !important;

          .cell {
            padding: 0 !important;
            overflow: visible;
          }
        }

        @include write-off-select-col-cell-surface;

        .el-table__body tr.is-write-off-receipt-selected td.customer-row-select-col.el-table__cell,
        .el-table__body tr.is-write-off-receipt-selected td.customer-row-select-col.el-table-fixed-column--right,
        .el-table__fixed-right .el-table__body tr.is-write-off-receipt-selected td.customer-row-select-col.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        .customer-row-select-action {
          display: flex;
          align-items: center;
          justify-content: flex-end;
          min-height: 100%;
          padding-right: 4px;
        }

        .customer-row-select-btn {
          opacity: 0;
          pointer-events: none;
          transition: opacity 0.15s;
          font-size: 12px;
          padding: 0 4px;
        }

        .el-table__body tr:hover .customer-row-select-btn,
        .el-table__body tr.hover-row .customer-row-select-btn {
          opacity: 1;
          pointer-events: auto;
        }
      }
    }

    &--right {
      min-width: 180px;

      .receivable-main {
        flex: 1 1 auto;
        min-height: 0;
        display: flex;
        flex-direction: column;
        justify-content: flex-start;
      }

      .receivable-scroll {
        flex: 0 1 auto;
        min-height: 0;
        overflow: auto;
      }

      .receivable-main.is-overflow .receivable-scroll {
        flex: 1 1 auto;
      }

      .detail-items-table-wrap--receivable {
        flex: 0 0 auto;
        min-height: 0;
      }

      :deep(.receivable-panel-table) {
        .el-table__body tr.is-siwo-receivable-focused > td.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        .el-table__body tr.is-siwo-receivable-focused:hover > td.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        th.write-off-amount-col.el-table__cell {
          background: var(--crm-detail-panel-card-head-bg) !important;
          background-color: var(--crm-detail-panel-card-head-bg) !important;
        }

        th.write-off-amount-col.el-table__cell .cell {
          font-weight: 700;
        }

        th.write-off-amount-col.el-table__cell .cell,
        td.write-off-amount-col.el-table__cell .cell {
          padding-left: 10px !important;
          padding-right: 10px !important;
        }

        td.write-off-amount-col.el-table__cell .cell {
          display: flex;
          justify-content: center;
          overflow: visible;
        }
      }
    }
  }

  .section-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 14px 20px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.05);
    background: var(--crm-detail-section-header-bg);
    flex-shrink: 0;

    .section-title {
      margin: 0;
      font-size: 14px;
      font-weight: 600;
      color: $text-primary;
    }
  }

  .section-header__main {
    display: flex;
    align-items: center;
    gap: 10px;
    min-width: 0;
  }

  .section-header__layout {
    display: inline-flex;
    align-items: center;
    gap: 2px;
    flex-shrink: 0;
  }

  .panels-layout-btn {
    color: var(--el-text-color-secondary);

    &.is-active {
      color: var(--el-color-primary);
    }
  }

  .panels-layout-icon {
    display: block;
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

  .section-count {
    font-size: 11px;
    padding: 1px 7px;
    border-radius: 999px;
    background: rgba(0, 212, 255, 0.1);
    color: $cyan-primary;
  }

  .detail-panel-section-body.write-off-panel__body {
    flex: 1;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 16px 20px 20px;
  }

  .detail-items-table-wrap {
    flex: 1 1 auto;
    min-height: 0;

    :deep(.el-table.detail-panel-list-table) {
      --el-table-border-color: transparent;
      --el-table-fixed-box-shadow: none;
      background: transparent !important;

      .el-table__fixed-right,
      .el-table__fixed-right-patch {
        background: var(--crm-detail-panel-card-bg) !important;
        background-color: var(--crm-detail-panel-card-bg) !important;
      }

      .el-table__fixed-right th.el-table__cell,
      th.el-table-fixed-column--right {
        background: var(--crm-detail-panel-card-head-bg) !important;
        background-color: var(--crm-detail-panel-card-head-bg) !important;
      }

      .el-table__fixed-right td.el-table__cell,
      td.el-table-fixed-column--right {
        background: var(--crm-detail-panel-card-bg) !important;
        background-color: var(--crm-detail-panel-card-bg) !important;
        box-shadow: inset 1px 0 0 var(--crm-table-header-line);
      }

      .el-table__fixed-right .el-table__body tr.el-table__row--striped td.el-table__cell,
      .el-table__body tr.el-table__row--striped td.el-table-fixed-column--right {
        background: var(--crm-table-fixed-right-stripe, var(--crm-table-row-stripe)) !important;
        background-color: var(--crm-table-fixed-right-stripe, var(--crm-table-row-stripe)) !important;
      }

      .el-table__fixed-right .el-table__body tr.el-table__row:hover td.el-table__cell,
      .el-table__fixed-right .el-table__body tr.el-table__row.hover-row td.el-table__cell,
      .el-table__body tr.el-table__row:hover td.el-table-fixed-column--right,
      .el-table__body tr.el-table__row.hover-row td.el-table-fixed-column--right {
        background: var(--crm-table-row-hover) !important;
        background-color: var(--crm-table-row-hover) !important;
      }

      .el-table__fixed-right .el-table__body tr.is-write-off-receipt-selected td.el-table__cell,
      .el-table__body tr.is-write-off-receipt-selected td.el-table-fixed-column--right {
        background: #fffaea !important;
        background-color: #fffaea !important;
      }

      .el-table__fixed-right .el-table__body tr.is-siwo-receivable-focused td.el-table__cell,
      .el-table__body tr.is-siwo-receivable-focused td.el-table-fixed-column--right {
        background: #fffaea !important;
        background-color: #fffaea !important;
      }
    }
  }

  .panel-info-bar {
    @include write-off-info-bar-surface;
    flex-shrink: 0;
    margin: 0 0 12px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;

    &__hint--placeholder {
      color: $text-muted;
      font-style: italic;
    }

    &__highlight-amount {
      color: $color-amber;
      font-weight: 700;
      font-variant-numeric: tabular-nums;
    }

    .panel-hint__value {
      color: $color-amber;
    }

    .panel-hint__sep {
      color: $text-muted;
    }

    &--with-total {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      overflow: visible;
      text-overflow: clip;
    }

    &__main {
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    &__actions {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;
    }

    &__total {
      flex-shrink: 0;
      font-variant-numeric: tabular-nums;
      font-weight: 600;

      &.is-zero {
        color: $text-primary;
      }

      &.is-positive {
        color: $success-color;
      }

      &.is-over {
        color: var(--el-color-danger);
      }
    }
  }

  .write-off-amount-input {
    width: 13ch;
    min-width: 13ch;
    flex-shrink: 0;

    :deep(.el-input__wrapper) {
      background-color: #fff !important;
      box-shadow: 0 0 0 1px var(--el-border-color) inset;
      padding-left: 8px;
      padding-right: 10px;
    }

    :deep(.el-input__inner) {
      font-variant-numeric: tabular-nums;
      text-align: right;
      padding-right: 2px;
      color: $text-primary !important;
      -webkit-text-fill-color: $text-primary !important;
    }

    &--positive {
      :deep(.el-input) {
        --el-input-text-color: #{$color-amber};
      }

      :deep(.el-input__inner) {
        color: $color-amber !important;
        -webkit-text-fill-color: $color-amber !important;
        font-weight: 700;
      }
    }
  }

  .receivable-submit-bar {
    flex: 0 0 auto;
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: 12px;
    margin-top: 10px;
    padding: 10px 0 4px;
    border-top: 1px solid var(--el-border-color-lighter);
    background: #fff;
    z-index: 3;
  }

  .link-text {
    color: var(--el-color-primary);
    text-decoration: none;

    &:hover {
      text-decoration: underline;
    }
  }
}
</style>
