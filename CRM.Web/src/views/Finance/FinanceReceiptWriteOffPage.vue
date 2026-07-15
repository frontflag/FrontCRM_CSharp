<template>
  <div class="finance-page write-off-page">
    <div class="write-off-page-header">
      <h1 class="finance-list-page-title">{{ t('financeReceiptWriteOff.pageTitle') }}</h1>
      <el-button type="primary" plain @click="goWriteOffLedger">
        {{ t('financeReceiptWriteOff.openLedger') }}
      </el-button>
    </div>

    <div v-if="selectedCustomerId && filteredAdvanceBalances.length" class="advance-bar">
      <span class="advance-bar__label">{{ t('financeReceiptWriteOff.advanceBalances') }}</span>
      <el-tag v-for="(a, idx) in filteredAdvanceBalances" :key="idx" type="success" effect="plain" class="advance-tag">
        {{ currencyLabel(a.currency) }} {{ formatAmount(a.balance) }}
      </el-tag>
    </div>

    <div ref="layoutRef" class="write-off-layout" :class="{ 'is-resizing': !!dragging }" v-loading="loading">
      <aside
        class="info-section write-off-panel write-off-panel--left"
        :class="{ 'is-collapsed': leftPanelCollapsed }"
        :style="leftPanelStyle"
      >
        <div class="section-header">
          <button
            type="button"
            class="panel-toggle"
            :aria-label="leftPanelCollapsed ? t('financeReceiptWriteOff.expandCustomers') : t('financeReceiptWriteOff.collapseCustomers')"
            @click="leftPanelCollapsed = !leftPanelCollapsed"
          >
            {{ leftPanelCollapsed ? '»' : '«' }}
          </button>
          <div v-if="!leftPanelCollapsed" class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('financeReceiptWriteOff.customerPanelTitle') }}</span>
          </div>
        </div>
        <div v-if="!leftPanelCollapsed" class="detail-panel-section-body write-off-panel__body">
          <div class="panel-search">
            <el-input
              v-model="customerKeyword"
              class="panel-search__input"
              :placeholder="t('financeReceiptWriteOff.customerSearchPh')"
              @keyup.enter="loadCustomerSummaries"
            >
              <template #suffix>
                <button
                  v-if="customerKeyword.trim()"
                  type="button"
                  class="panel-search__clear"
                  :aria-label="t('financeReceiptWriteOff.customerSearchClear')"
                  :title="t('financeReceiptWriteOff.customerSearchClear')"
                  @click="clearCustomerSearch"
                >
                  <el-icon><CircleClose /></el-icon>
                </button>
              </template>
            </el-input>
            <el-button type="primary" size="small" class="panel-search__btn" @click="loadCustomerSummaries">
              {{ t('financeReceiptWriteOff.customerSearchBtn') }}
            </el-button>
            <el-checkbox v-model="excludeNoReceivable" class="panel-search__filter">
              {{ t('financeReceiptWriteOff.excludeNoReceivable') }}
            </el-checkbox>
          </div>
          <div ref="customerTableWrapRef" class="detail-items-table-wrap">
            <el-table
              ref="customerTableRef"
              :data="displayedCustomerSummaries"
              class="detail-panel-list-table customer-panel-table"
              size="small"
              stripe
              height="100%"
              table-layout="fixed"
              :row-class-name="customerRowClassName"
              :row-key="customerRowKey"
              @row-dblclick="onCustomerRowDblClick"
            >
              <el-table-column
                prop="customer"
                :width="customerExtendDisplayWidth"
                :min-width="customerExtendColMinWidth"
                class-name="customer-extend-col"
                label-class-name="customer-extend-col"
                :show-overflow-tooltip="!customerExtendExpanded"
              >
                <template #header>
                  <CustomerExtendColumnHeader
                    :active-field="customerExtendActiveField"
                    @set-active-field="setCustomerExtendActiveField"
                  />
                </template>
                <template #default="{ row }">
                  <CustomerExtendCell
                    :row="customerExtendRowSlice(row)"
                    :active-field="customerExtendActiveField"
                  />
                </template>
              </el-table-column>
              <el-table-column
                :label="t('financeReceiptWriteOff.colSalesUser')"
                :width="CUSTOMER_TABLE_COL_SALES_USER"
                align="left"
                header-align="left"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.salesUserName || '—' }}</template>
              </el-table-column>
              <el-table-column
                :label="t('financeReceiptWriteOff.colPendingWriteOffTotal')"
                :width="CUSTOMER_TABLE_COL_PENDING_TOTAL"
                align="right"
                header-align="right"
                show-overflow-tooltip
              >
                <template #default="{ row }">
                  <span class="customer-pending-total-cell">{{ formatCustomerPendingWriteOffTotal(row) }}</span>
                </template>
              </el-table-column>
              <el-table-column
                prop="pendingReceiptItemCount"
                :label="t('financeReceiptWriteOff.colPendingReceiptCount')"
                :width="CUSTOMER_TABLE_COL_PENDING"
                align="center"
                header-align="center"
              />
              <el-table-column
                prop="receiptDate"
                :width="receiptDateExtendDisplayWidth"
                :min-width="receiptDateExtendColMinWidth"
                class-name="customer-extend-col"
                label-class-name="customer-extend-col"
                :show-overflow-tooltip="!receiptDateExtendExpanded"
              >
                <template #header>
                  <WriteOffReceiptDateExtendColumnHeader
                    :active-field="receiptDateExtendActiveField"
                    @set-active-field="setReceiptDateExtendActiveField"
                  />
                </template>
                <template #default="{ row }">
                  <WriteOffReceiptDateExtendCell
                    :row="receiptDateExtendRowSlice(row)"
                    :active-field="receiptDateExtendActiveField"
                    :format-date="formatDate"
                  />
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
                      @click="onCustomerRowSelect(row)"
                    >
                      {{ t('financeReceiptWriteOff.selectCustomerRow') }}
                    </el-button>
                  </div>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>
      </aside>

      <div
        v-if="!leftPanelCollapsed"
        class="col-splitbar"
        :class="{ 'is-dragging': dragging === 'left' }"
        :title="t('financeReceiptWriteOff.dragLeftWidth')"
        @mousedown="onSplitStart('left', $event)"
      />

      <section class="info-section write-off-panel write-off-panel--center" :style="centerPanelStyle">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('financeReceiptWriteOff.receiptPanelTitle') }}</span>
            <span v-if="selectedCustomerId && filteredReceiptItems.length" class="section-count">
              {{ filteredReceiptItems.length }}
            </span>
          </div>
        </div>
        <div class="detail-panel-section-body write-off-panel__body">
          <div class="panel-info-bar">
            <span class="panel-info-bar__label">{{ t('financeReceiptWriteOff.currentCustomerLabel') }}</span>
            <template v-if="!selectedCustomerSummary">
              <span class="panel-info-bar__hint panel-info-bar__hint--placeholder">
                {{ t('financeReceiptWriteOff.currentCustomerHint') }}
              </span>
            </template>
            <template v-else>
              <span class="panel-info-bar__customer">
                <span class="panel-hint__value">{{ currentCustomerNameZh(selectedCustomerSummary) }}</span
                ><span class="panel-hint__sep"> / </span
                ><span class="panel-hint__value">{{ currentCustomerNameEn(selectedCustomerSummary) }}</span>
              </span>
              <span class="panel-info-bar__currency">
                （{{ currentCustomerCurrency(selectedCustomerSummary) }}）
              </span>
            </template>
          </div>
          <div class="detail-items-table-wrap">
            <DetailListPanelEmpty
              v-if="!selectedCustomerId"
              size="medium"
              :description="t('financeReceiptWriteOff.selectCustomerHint')"
            />
            <DetailListPanelEmpty
              v-else-if="!filteredReceiptItems.length"
              size="medium"
              :description="t('financeReceiptWriteOff.noReceiptItems')"
            />
            <el-table
              v-else
              :data="filteredReceiptItems"
              class="detail-panel-list-table receipt-panel-table"
              :border="false"
              size="small"
              stripe
              height="100%"
              :row-class-name="receiptRowClassName"
              :row-key="receiptRowKey"
              @row-dblclick="onReceiptRowDblClick"
            >
            <el-table-column :label="t('financeReceiptWriteOff.colReceiptDate')" width="108">
              <template #default="{ row }">{{ formatDate(row.receiptDate) }}</template>
            </el-table-column>
            <el-table-column prop="financeReceiptCode" :label="t('financeReceiptWriteOff.colReceiptCode')" min-width="118" show-overflow-tooltip />
            <el-table-column :label="t('financeReceiptWriteOff.colReceiptType')" width="92">
              <template #default="{ row }">{{ paymentModeLabel(row.receiptMode) }}</template>
            </el-table-column>
            <el-table-column :label="t('financeReceiptWriteOff.colPurpose')" width="72">
              <template #default="{ row }">
                {{ row.receiptPurpose === 20 ? t('financeReceiptWriteOff.purposeAdvance') : t('financeReceiptWriteOff.purposeNormal') }}
              </template>
            </el-table-column>
            <el-table-column :label="t('financeReceiptWriteOff.colReceiptAmount')" width="132" align="right" header-align="right">
              <template #default="{ row }">
                <span class="finance-amount-emphasis finance-amount-emphasis--amount">
                  {{ formatAmountWithCurrency(row.receiptAmount, row.receiptCurrency) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column :label="t('financeReceiptWriteOff.colVerifiedAmount')" width="132" align="right" header-align="right">
              <template #default="{ row }">
                <span class="finance-amount-emphasis finance-amount-emphasis--verified">
                  {{ formatAmountWithCurrency(row.item.verifiedAmount, row.receiptCurrency) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column :label="t('financeReceiptWriteOff.colRemaining')" width="132" align="right" header-align="right">
              <template #default="{ row }">
                <span class="finance-amount-emphasis finance-amount-emphasis--pending">
                  {{ formatAmountWithCurrency(row.remainingAmount, row.receiptCurrency) }}
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
                    @click="onReceiptRowSelect(row)"
                  >
                    {{ t('financeReceiptWriteOff.selectCustomerRow') }}
                  </el-button>
                </div>
              </template>
            </el-table-column>
            </el-table>
          </div>
        </div>
      </section>

      <div
        class="col-splitbar"
        :class="{ 'is-dragging': dragging === 'center' }"
        :title="t('financeReceiptWriteOff.dragCenterWidth')"
        @mousedown="onSplitStart('center', $event)"
      />

      <section class="info-section write-off-panel write-off-panel--right">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('financeReceiptWriteOff.receivablePanelTitle') }}</span>
          </div>
        </div>
        <div class="detail-panel-section-body write-off-panel__body">
          <div v-if="selectedCustomerId" class="panel-info-bar panel-info-bar--with-total">
            <span class="panel-info-bar__main">
              <template v-if="selectedReceiptItem">
                {{ t('financeReceiptWriteOff.selectedReceiptInfoBarPrefix', {
                  receiptCode: selectedReceiptItem.financeReceiptCode || '—'
                }) }}<span class="panel-info-bar__highlight-amount">{{
                  formatAmountWithCurrency(selectedReceiptItem.remainingAmount, selectedReceiptItem.receiptCurrency)
                }}</span>
              </template>
              <template v-else>
                <span class="panel-info-bar__hint panel-info-bar__hint--placeholder">
                  {{ t('financeReceiptWriteOff.selectReceiptHint') }}
                </span>
              </template>
            </span>
            <div v-if="selectedReceiptItem" class="panel-info-bar__actions">
              <span class="panel-info-bar__total" :class="writeOffTotalColorClass">
                {{ t('financeReceiptWriteOff.writeOffTotalLabel', {
                  total: formatAmountWithCurrency(writeOffAmountTotal, selectedReceiptItem.receiptCurrency)
                }) }}
              </span>
              <el-button type="primary" link size="small" @click="autoFillWriteOffAmounts">
                {{ t('financeReceiptWriteOff.autoFill') }}
              </el-button>
            </div>
          </div>
          <div class="detail-items-table-wrap">
            <DetailListPanelEmpty
              v-if="!selectedCustomerId"
              size="medium"
              :description="t('financeReceiptWriteOff.selectCustomerHint')"
            />
            <el-table
              v-else
              :data="filteredReceivableRows"
              class="detail-panel-list-table receivable-panel-table"
              size="small"
              stripe
              height="100%"
            >
            <el-table-column
              prop="receivableCode"
              :label="t('financeReceiptWriteOff.colReceivableCode')"
              min-width="118"
              show-overflow-tooltip
            >
              <template #default="{ row }">
                <span class="code-text">{{ row.receivableCode || '—' }}</span>
              </template>
            </el-table-column>
            <el-table-column :label="t('financeReceiptWriteOff.colStockOutDate')" width="108">
              <template #default="{ row }">{{ formatDate(row.stockOutDate) }}</template>
            </el-table-column>
            <el-table-column prop="stockOutCode" :label="t('financeReceiptWriteOff.colStockOut')" min-width="118" show-overflow-tooltip>
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
            <el-table-column prop="sellOrderCode" :label="t('financeReceiptWriteOff.colSellOrderCode')" min-width="118" show-overflow-tooltip>
              <template #default="{ row }">{{ row.sellOrderCode || '—' }}</template>
            </el-table-column>
            <el-table-column prop="salesUserName" :label="t('financeReceiptWriteOff.colSalesUser')" width="96" show-overflow-tooltip>
              <template #default="{ row }">{{ row.salesUserName || '—' }}</template>
            </el-table-column>
            <el-table-column prop="freightForwarderOrderNo" :label="t('financeReceiptWriteOff.colFreightForwarder')" min-width="110">
              <template #default="{ row }">
                <CrmListCopyableTextCell :text="pickCrmCopyableRowField(row, 'freightForwarderOrderNo')" />
              </template>
            </el-table-column>
            <el-table-column prop="stockInCode" :label="t('financeReceiptWriteOff.colStockInCode')" min-width="110" show-overflow-tooltip>
              <template #default="{ row }">{{ row.stockInCode || '—' }}</template>
            </el-table-column>
            <el-table-column :label="t('financeReceiptWriteOff.colToBe')" width="132" min-width="130" align="right">
              <template #default="{ row }">{{ formatAmountWithCurrency(row.verifiedToBe, row.currency) }}</template>
            </el-table-column>
            <el-table-column
              fixed="right"
              :label="t('financeReceiptWriteOff.colWriteOffAmount')"
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
                  :max="row.verifiedToBe"
                  :precision="2"
                  :controls="false"
                  size="small"
                />
              </template>
            </el-table-column>
          </el-table>
          </div>
        </div>
      </section>
    </div>

    <div v-if="selectedCustomerId && canWriteFinanceReceipt" class="footer-actions">
      <el-button @click="router.push({ name: 'FinanceReceivableList' })">
        {{ t('financeReceiptWriteOff.back') }}
      </el-button>
      <el-button type="primary" :loading="submitting" @click="submitWriteOff">
        {{ t('financeReceiptWriteOff.submit') }}
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CircleClose } from '@element-plus/icons-vue'
import {
  financeReceivableApi,
  type FinanceAdvancePoolAllocation,
  type FinanceReceiptItemWriteOffCandidate,
  type FinanceReceivableWriteOffCandidateRow,
  type FinanceReceivableWriteOffSoMismatch,
  type FinanceWriteOffCustomerSummary
} from '@/api/financeReceivable'
import type { FinanceCustomerAdvanceBalance } from '@/api/financeCustomerAdvance'
import { CURRENCY_MAP } from '@/api/finance'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import CustomerExtendColumnHeader from '@/components/list/CustomerExtendColumnHeader.vue'
import CustomerExtendCell from '@/components/list/CustomerExtendCell.vue'
import WriteOffReceiptDateExtendColumnHeader from '@/components/list/WriteOffReceiptDateExtendColumnHeader.vue'
import WriteOffReceiptDateExtendCell from '@/components/list/WriteOffReceiptDateExtendCell.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useCustomerExtendColumn } from '@/composables/useCustomerExtendColumn'
import { useWriteOffReceiptDateExtendColumn } from '@/composables/useWriteOffReceiptDateExtendColumn'
import type { CustomerExtendRowSlice } from '@/constants/listCustomerExtendColumnSpec'
import type { WriteOffReceiptDateRowSlice } from '@/constants/writeOffReceiptDateExtendColumnSpec'
import { pickCrmCopyableRowField } from '@/utils/crmListCopyableField'

type ReceivableWriteOffRow = FinanceReceivableWriteOffCandidateRow & {
  writeOffAmount: number
  poolAmount: number
}

const { t } = useI18n()
const router = useRouter()
const { canWriteFinanceReceipt } = useFinanceWriteGate()
const { paymentModeLabel } = useFinanceEnumLabels()
const {
  expanded: customerExtendExpanded,
  activeField: customerExtendActiveField,
  colWidth: customerExtendColWidth,
  colMinWidth: customerExtendColMinWidth,
  setActiveField: setCustomerExtendActiveField
} = useCustomerExtendColumn()
const {
  expanded: receiptDateExtendExpanded,
  activeField: receiptDateExtendActiveField,
  colWidth: receiptDateExtendColWidth,
  colMinWidth: receiptDateExtendColMinWidth,
  setActiveField: setReceiptDateExtendActiveField
} = useWriteOffReceiptDateExtendColumn()

const leftPanelCollapsed = ref(false)
const layoutRef = ref<HTMLElement | null>(null)
const customerTableRef = ref<{ doLayout?: () => void } | null>(null)
const customerTableWrapRef = ref<HTMLElement | null>(null)
const leftPanelWidthPx = ref(0)
const centerPanelWidthPx = ref(0)
const dragging = ref<'left' | 'center' | null>(null)

const SPLIT_BAR_WIDTH = 6
const MIN_PANEL_WIDTH = 180
const COLLAPSED_LEFT_WIDTH = 44
const DEFAULT_LEFT_RATIO = 0.5
/** 左栏客户表：固定列宽 + 表格外边距，用于计算「客户」扩展列 */
const CUSTOMER_TABLE_COL_SALES_USER = 88
const CUSTOMER_TABLE_COL_PENDING_TOTAL = 132
const CUSTOMER_TABLE_COL_PENDING = 116
const CUSTOMER_TABLE_COL_SELECT = 52
const CUSTOMER_TABLE_HORIZONTAL_INSET = 40
const CUSTOMER_EXTEND_COL_MIN = 80
const CUSTOMER_EXTEND_COL_COLLAPSED_MULTIPLIER = 1

const receiptDateExtendDisplayWidth = computed(() => {
  void receiptDateExtendExpanded.value
  void receiptDateExtendColWidth.value
  return receiptDateExtendColWidth.value
})

const customerExtendDisplayWidth = computed(() => {
  void customerExtendExpanded.value
  void customerExtendColWidth.value
  void receiptDateExtendDisplayWidth.value
  const wrapW = customerTableWrapRef.value?.clientWidth ?? 0
  const panelW = wrapW > 0 ? wrapW : leftPanelWidthPx.value
  const fixed =
    CUSTOMER_TABLE_COL_SALES_USER +
    CUSTOMER_TABLE_COL_PENDING_TOTAL +
    CUSTOMER_TABLE_COL_PENDING +
    receiptDateExtendDisplayWidth.value +
    CUSTOMER_TABLE_COL_SELECT +
    CUSTOMER_TABLE_HORIZONTAL_INSET
  const remaining = Math.max(CUSTOMER_EXTEND_COL_MIN, Math.floor(panelW - fixed))

  if (customerExtendExpanded.value) {
    return Math.max(customerExtendColMinWidth.value, Math.min(customerExtendColWidth.value, remaining))
  }
  return Math.max(
    CUSTOMER_EXTEND_COL_MIN,
    customerExtendColWidth.value * CUSTOMER_EXTEND_COL_COLLAPSED_MULTIPLIER
  )
})

let dragStartX = 0
let dragStartLeft = 0
let dragStartCenter = 0

function clamp(n: number, min: number, max: number) {
  return Math.min(max, Math.max(min, n))
}

function splitBarCount() {
  return leftPanelCollapsed.value ? 1 : 2
}

function usableLayoutWidth() {
  const total = layoutRef.value?.clientWidth ?? 0
  return Math.max(0, total - SPLIT_BAR_WIDTH * splitBarCount())
}

function initPanelWidths() {
  const usable = usableLayoutWidth()
  if (usable <= 0) return

  if (leftPanelCollapsed.value) {
    const half = Math.round(usable * DEFAULT_LEFT_RATIO)
    centerPanelWidthPx.value = clamp(half, MIN_PANEL_WIDTH, usable - MIN_PANEL_WIDTH)
    return
  }

  leftPanelWidthPx.value = clamp(
    Math.round(usable * DEFAULT_LEFT_RATIO),
    MIN_PANEL_WIDTH,
    usable - MIN_PANEL_WIDTH * 2
  )
  centerPanelWidthPx.value = clamp(
    Math.round((usable - leftPanelWidthPx.value) * DEFAULT_LEFT_RATIO),
    MIN_PANEL_WIDTH,
    usable - leftPanelWidthPx.value - MIN_PANEL_WIDTH
  )
}

const leftPanelStyle = computed(() => {
  if (leftPanelCollapsed.value) {
    return {
      width: `${COLLAPSED_LEFT_WIDTH}px`,
      flex: `0 0 ${COLLAPSED_LEFT_WIDTH}px`
    }
  }
  const w = leftPanelWidthPx.value
  return { width: `${w}px`, flex: `0 0 ${w}px` }
})

const centerPanelStyle = computed(() => {
  const w = centerPanelWidthPx.value
  return { width: `${w}px`, flex: `0 0 ${w}px` }
})

function onSplitStart(which: 'left' | 'center', e: MouseEvent) {
  e.preventDefault()
  dragging.value = which
  dragStartX = e.clientX
  dragStartLeft = leftPanelWidthPx.value
  dragStartCenter = centerPanelWidthPx.value
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'
}

function onSplitMove(e: MouseEvent) {
  if (!dragging.value) return
  const dx = e.clientX - dragStartX
  const usable = usableLayoutWidth()

  if (dragging.value === 'left') {
    const maxLeft = usable - dragStartCenter - MIN_PANEL_WIDTH
    leftPanelWidthPx.value = clamp(dragStartLeft + dx, MIN_PANEL_WIDTH, Math.max(MIN_PANEL_WIDTH, maxLeft))
  } else {
    const leftW = leftPanelCollapsed.value ? COLLAPSED_LEFT_WIDTH : leftPanelWidthPx.value
    const maxCenter = usable - leftW - MIN_PANEL_WIDTH
    centerPanelWidthPx.value = clamp(dragStartCenter + dx, MIN_PANEL_WIDTH, Math.max(MIN_PANEL_WIDTH, maxCenter))
  }
}

function onSplitEnd() {
  if (!dragging.value) return
  dragging.value = null
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
  relayoutCustomerTable()
}

function relayoutCustomerTable() {
  void nextTick(() => customerTableRef.value?.doLayout?.())
}

function setupCustomerTableResizeObserver() {
  customerTableResizeObserver?.disconnect()
  customerTableResizeObserver = null
  const el = customerTableWrapRef.value
  if (!el || typeof ResizeObserver === 'undefined') return
  customerTableResizeObserver = new ResizeObserver(() => relayoutCustomerTable())
  customerTableResizeObserver.observe(el)
}

let customerTableResizeObserver: ResizeObserver | null = null

const customerKeyword = ref('')
const excludeNoReceivable = ref(false)
const customerSummaries = ref<FinanceWriteOffCustomerSummary[]>([])
const selectedCustomerId = ref('')
const selectedCustomerSummary = ref<FinanceWriteOffCustomerSummary | null>(null)
const selectedCurrency = ref<number | null>(null)
const loading = ref(false)
const submitting = ref(false)

const receiptItems = ref<FinanceReceiptItemWriteOffCandidate[]>([])
const receivableRows = ref<ReceivableWriteOffRow[]>([])
const advanceBalances = ref<FinanceCustomerAdvanceBalance[]>([])
const selectedReceiptItemId = ref('')

const filteredReceiptItems = computed(() => {
  if (selectedCurrency.value == null) return receiptItems.value
  const currency = Number(selectedCurrency.value)
  return receiptItems.value.filter(r => Number(r.receiptCurrency) === currency)
})

const selectedReceiptItem = computed(() =>
  filteredReceiptItems.value.find(r => r.item.id === selectedReceiptItemId.value) ?? null
)

const writeOffAmountTotal = computed(() =>
  filteredReceivableRows.value.reduce((sum, row) => sum + (Number(row.writeOffAmount) || 0), 0)
)

const writeOffTotalColorClass = computed(() => {
  const total = writeOffAmountTotal.value
  if (total <= 0.0001) return 'is-zero'
  const remaining = selectedReceiptItem.value?.remainingAmount ?? 0
  if (total > remaining + 0.001) return 'is-over'
  return 'is-positive'
})

const filteredReceivableRows = computed(() => {
  if (selectedCurrency.value == null) return receivableRows.value
  return receivableRows.value.filter(r => r.currency === selectedCurrency.value)
})

const filteredAdvanceBalances = computed(() => {
  if (selectedCurrency.value == null) return advanceBalances.value
  return advanceBalances.value.filter(a => a.currency === selectedCurrency.value)
})

const displayedCustomerSummaries = computed(() => {
  const rows = customerSummaries.value
  if (!excludeNoReceivable.value) return rows
  return rows.filter(r => r.hasOpenReceivable)
})

function customerRowKey(row: FinanceWriteOffCustomerSummary) {
  return `${row.customerId}::${row.currency ?? 0}`
}

function findCustomerSummaryRow(customerId: string, currency: number | null | undefined) {
  if (currency == null) {
    return customerSummaries.value.find(r => r.customerId === customerId)
  }
  return customerSummaries.value.find(r => r.customerId === customerId && r.currency === currency)
}

function customerRowClassName({ row }: { row: FinanceWriteOffCustomerSummary }) {
  const classes: string[] = []
  if (row.customerId === selectedCustomerId.value && row.currency === selectedCurrency.value) {
    classes.push('is-write-off-customer-selected')
  }
  if (!row.hasOpenReceivable) {
    classes.push('is-write-off-customer-no-receivable')
  }
  return classes.join(' ')
}

function currentCustomerNameZh(row: FinanceWriteOffCustomerSummary) {
  return row.customerName?.trim() || row.customerId
}

function currentCustomerNameEn(row: FinanceWriteOffCustomerSummary) {
  return row.customerEnglishName?.trim() || '—'
}

function currentCustomerCurrency(row: FinanceWriteOffCustomerSummary) {
  return row.currency != null ? currencyLabel(row.currency) : '—'
}

function clearCustomerSelection() {
  selectedCustomerId.value = ''
  selectedCustomerSummary.value = null
  selectedCurrency.value = null
  receiptItems.value = []
  receivableRows.value = []
  advanceBalances.value = []
  selectedReceiptItemId.value = ''
}

function customerExtendRowSlice(row: FinanceWriteOffCustomerSummary): CustomerExtendRowSlice {
  return {
    customerName: row.customerName || row.customerId,
    customerEnglishName: row.customerEnglishName,
    customerCode: row.customerCode
  }
}

function receiptDateExtendRowSlice(row: FinanceWriteOffCustomerSummary): WriteOffReceiptDateRowSlice {
  return {
    earliestReceiptDate: row.earliestReceiptDate,
    latestReceiptDate: row.latestReceiptDate
  }
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

function formatCustomerPendingWriteOffTotal(row: FinanceWriteOffCustomerSummary) {
  const amount = row.pendingWriteOffTotal ?? row.currencyTotals?.[0]?.amount
  const currency = row.currency ?? row.currencyTotals?.[0]?.currency
  if (currency == null) return formatAmount(amount)
  return `${formatAmount(amount)} ${currencyLabel(currency)}`
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return v.slice(0, 10)
}

function isWriteOffAmountPositive(amount?: number) {
  return (Number(amount) || 0) > 0.0001
}

function currencyLabel(currency: number) {
  return CURRENCY_MAP[currency] ?? String(currency)
}

function poolMaxForCurrency(currency: number) {
  return advanceBalances.value.find(a => a.currency === currency)?.balance ?? 0
}

function clearCustomerSearch() {
  customerKeyword.value = ''
  void loadCustomerSummaries()
}

async function loadCustomerSummaries() {
  loading.value = true
  try {
    const rows = await financeReceivableApi.getWriteOffCustomerSummaries(customerKeyword.value)
    customerSummaries.value = rows ?? []
    if (!customerSummaries.value.length) {
      clearCustomerSelection()
      return
    }

    const matched =
      selectedCustomerId.value && selectedCurrency.value != null
        ? findCustomerSummaryRow(selectedCustomerId.value, selectedCurrency.value)
        : undefined
    if (matched) {
      if (excludeNoReceivable.value && !matched.hasOpenReceivable) {
        clearCustomerSelection()
        return
      }
      selectedCustomerSummary.value = matched
      await loadCandidates(matched.customerId, matched.currency ?? null, false)
      return
    }

    clearCustomerSelection()
  } finally {
    loading.value = false
    relayoutCustomerTable()
  }
}

function onCustomerRowDblClick(row: FinanceWriteOffCustomerSummary) {
  selectedCustomerSummary.value = row
  void loadCandidates(row.customerId, row.currency ?? null, true)
}

function onCustomerRowSelect(row: FinanceWriteOffCustomerSummary) {
  onCustomerRowDblClick(row)
}

function receiptRowKey(row: FinanceReceiptItemWriteOffCandidate) {
  return row.item.id
}

function receiptRowClassName({ row }: { row: FinanceReceiptItemWriteOffCandidate }) {
  return row.item.id === selectedReceiptItemId.value ? 'is-write-off-receipt-selected' : ''
}

function onReceiptRowDblClick(row: FinanceReceiptItemWriteOffCandidate) {
  selectedReceiptItemId.value = row.item.id
}

function onReceiptRowSelect(row: FinanceReceiptItemWriteOffCandidate) {
  onReceiptRowDblClick(row)
}

async function loadCandidates(customerId: string, currency: number | null, resetReceiptSelection: boolean) {
  selectedCustomerId.value = customerId
  const matchedSummary = findCustomerSummaryRow(customerId, currency)
  if (matchedSummary) {
    selectedCustomerSummary.value = matchedSummary
  } else if (!selectedCustomerSummary.value || selectedCustomerSummary.value.customerId !== customerId) {
    selectedCustomerSummary.value = findCustomerSummaryRow(customerId, currency) ?? selectedCustomerSummary.value
  }
  selectedCurrency.value = currency
  loading.value = true
  try {
    const res = await financeReceivableApi.getWriteOffCandidates(customerId)
    receiptItems.value = res.receiptItems ?? []
    advanceBalances.value = res.advanceBalances ?? []
    receivableRows.value = (res.receivables ?? []).map(r => ({
      ...r,
      writeOffAmount: 0,
      poolAmount: 0
    }))

    const receipts = filteredReceiptItems.value
    if (resetReceiptSelection || !receipts.some(r => r.item.id === selectedReceiptItemId.value)) {
      selectedReceiptItemId.value = ''
    }
  } finally {
    loading.value = false
  }
}

function buildPayload(confirmSoMismatch = false) {
  const rows = filteredReceivableRows.value
  const itemAllocs = selectedReceiptItemId.value
    ? rows
        .filter(r => r.writeOffAmount > 0)
        .map(r => ({
          financeReceiptItemId: selectedReceiptItemId.value,
          financeReceivableId: r.id,
          amount: r.writeOffAmount
        }))
    : []

  const poolAllocs: FinanceAdvancePoolAllocation[] = rows
    .filter(r => r.poolAmount > 0)
    .map(r => ({
      financeReceivableId: r.id,
      amount: r.poolAmount
    }))

  return {
    allocations: itemAllocs,
    advancePoolAllocations: poolAllocs,
    confirmSoMismatch
  }
}

function validatePayload() {
  const payload = buildPayload()
  const rows = filteredReceivableRows.value
  const itemTotal = payload.allocations.reduce((s, a) => s + a.amount, 0)
  const poolTotal = (payload.advancePoolAllocations ?? []).reduce((s, a) => s + a.amount, 0)

  if (itemTotal <= 0 && poolTotal <= 0) {
    ElMessage.warning(t('financeReceiptWriteOff.noAmount'))
    return null
  }

  if (itemTotal > 0 && !selectedReceiptItemId.value) {
    ElMessage.warning(t('financeReceiptWriteOff.selectReceiptItem'))
    return null
  }

  const selectedReceipt = filteredReceiptItems.value.find(r => r.item.id === selectedReceiptItemId.value)
  if (selectedReceipt && itemTotal > selectedReceipt.remainingAmount + 0.001) {
    ElMessage.warning(t('financeReceiptWriteOff.exceedRemaining'))
    return null
  }

  for (const row of rows) {
    const total = row.writeOffAmount + row.poolAmount
    if (total > row.verifiedToBe + 0.001) {
      ElMessage.warning(t('financeReceiptWriteOff.exceedReceivable'))
      return null
    }
  }

  const poolByCurrency = new Map<number, number>()
  for (const row of rows) {
    if (row.poolAmount <= 0) continue
    poolByCurrency.set(row.currency, (poolByCurrency.get(row.currency) ?? 0) + row.poolAmount)
  }
  for (const [currency, amount] of poolByCurrency) {
    const max = poolMaxForCurrency(currency)
    if (amount > max + 0.001) {
      ElMessage.warning(t('financeReceiptWriteOff.exceedAdvancePool', { currency: currencyLabel(currency) }))
      return null
    }
  }

  return payload
}

function roundMoney(v: number) {
  return Math.round(v * 100) / 100
}

function autoFillWriteOffAmounts() {
  const receipt = selectedReceiptItem.value
  if (!receipt) {
    ElMessage.warning(t('financeReceiptWriteOff.selectReceiptItem'))
    return
  }

  let budget = roundMoney(receipt.remainingAmount ?? 0)
  if (budget <= 0) {
    ElMessage.warning(t('financeReceiptWriteOff.autoFillNoRemaining'))
    return
  }

  const rows = filteredReceivableRows.value
  if (rows.length === 0) {
    ElMessage.warning(t('financeReceiptWriteOff.autoFillNoRows'))
    return
  }

  for (const row of rows) {
    row.writeOffAmount = 0
  }

  for (let i = 0; i < rows.length && budget > 0.001; i++) {
    const row = rows[i]
    const fillAmount = roundMoney(Math.min(row.verifiedToBe ?? 0, budget))
    row.writeOffAmount = fillAmount
    budget = roundMoney(budget - fillAmount)
  }
}

function formatSoMismatchMessage(mismatches: FinanceReceivableWriteOffSoMismatch[]) {
  return mismatches.map(m => m.message || m.financeReceivableId).join('\n')
}

async function submitWriteOff(confirmSoMismatch = false) {
  const payload = validatePayload()
  if (!payload) return

  if (confirmSoMismatch) payload.confirmSoMismatch = true

  submitting.value = true
  try {
    const result = await financeReceivableApi.applyWriteOff(payload)
    if (result?.requiresSoMismatchConfirm && !confirmSoMismatch) {
      await ElMessageBox.confirm(
        formatSoMismatchMessage(result.soMismatches ?? []),
        t('financeReceiptWriteOff.soMismatchTitle'),
        { type: 'warning', confirmButtonText: t('financeReceiptWriteOff.soMismatchConfirm') }
      )
      await submitWriteOff(true)
      return
    }
    ElMessage.success(t('financeReceiptWriteOff.success'))
    await loadCustomerSummaries()
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('financeReceiptWriteOff.failed')
    ElMessage.error(msg)
  } finally {
    submitting.value = false
  }
}

function onWindowResize() {
  initPanelWidths()
  relayoutCustomerTable()
}

function goWriteOffLedger() {
  router.push({ name: 'FinanceReceiptWriteOffLedger' })
}

onMounted(() => {
  void nextTick(() => {
    initPanelWidths()
    setupCustomerTableResizeObserver()
    relayoutCustomerTable()
    void loadCustomerSummaries()
  })
  window.addEventListener('mousemove', onSplitMove)
  window.addEventListener('mouseup', onSplitEnd)
  window.addEventListener('resize', onWindowResize)
})

onBeforeUnmount(() => {
  customerTableResizeObserver?.disconnect()
  customerTableResizeObserver = null
  window.removeEventListener('mousemove', onSplitMove)
  window.removeEventListener('mouseup', onSplitEnd)
  window.removeEventListener('resize', onWindowResize)
  onSplitEnd()
})

watch(leftPanelCollapsed, () => {
  void nextTick(() => {
    initPanelWidths()
    setupCustomerTableResizeObserver()
    relayoutCustomerTable()
  })
})

watch(customerExtendDisplayWidth, () => relayoutCustomerTable())

watch(customerExtendExpanded, () => relayoutCustomerTable())

watch(receiptDateExtendDisplayWidth, () => relayoutCustomerTable())

watch(receiptDateExtendExpanded, () => relayoutCustomerTable())

watch(excludeNoReceivable, () => {
  if (!excludeNoReceivable.value) {
    relayoutCustomerTable()
    return
  }
  if (!selectedCustomerId.value || selectedCurrency.value == null) {
    relayoutCustomerTable()
    return
  }
  const stillVisible = displayedCustomerSummaries.value.some(
    r => r.customerId === selectedCustomerId.value && r.currency === selectedCurrency.value
  )
  if (!stillVisible) {
    const first = displayedCustomerSummaries.value[0]
    if (first) {
      void loadCandidates(first.customerId, first.currency ?? null, true)
    } else {
      clearCustomerSelection()
    }
  }
  relayoutCustomerTable()
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
  .write-off-page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
  }

  .advance-bar {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;

    &__label {
      font-size: 13px;
      color: var(--el-text-color-secondary);
    }
  }

  .advance-tag {
    font-variant-numeric: tabular-nums;
  }

  .write-off-layout {
    display: flex;
    align-items: stretch;
    min-height: 520px;
    height: calc(100vh - 260px);
    max-height: 760px;

    &.is-resizing {
      cursor: col-resize;
      user-select: none;
    }
  }

  .col-splitbar {
    flex: 0 0 6px;
    width: 6px;
    cursor: col-resize;
    align-self: stretch;
    position: relative;
    z-index: 2;
    border-radius: 4px;
    background: transparent;
    transition: background 0.15s;

    &:hover,
    &.is-dragging {
      background: var(--crm-splitter-hover, rgba(64, 158, 255, 0.18));
    }

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

  .write-off-panel {
    display: flex;
    flex-direction: column;
    min-width: 0;
    margin-bottom: 0;
    background: $layer-2;
    border: 1px solid $border-card;
    border-radius: $border-radius-lg;
    overflow: hidden;

    &--left {
      &.is-collapsed {
        min-width: 44px;
      }

      .customer-panel-table {
        width: 100%;
      }

      :deep(.customer-panel-table) {
        .el-table__header-wrapper table,
        .el-table__body-wrapper table {
          width: 100% !important;
          table-layout: fixed;
        }

        th.el-table__cell .cell {
          white-space: nowrap;
          overflow: visible;
          text-overflow: clip;
        }

        th.customer-extend-col.el-table__cell .cell,
        td.customer-extend-col.el-table__cell .cell {
          overflow: visible;
        }

        .customer-pending-total-cell {
          color: $color-amber;
        }

        .el-table__body tr.is-write-off-customer-no-receivable:not(.is-write-off-customer-selected) > td.el-table__cell {
          color: var(--el-text-color-placeholder);
        }

        .el-table__body tr.is-write-off-customer-no-receivable:not(.is-write-off-customer-selected) .customer-pending-total-cell {
          color: var(--el-text-color-placeholder);
        }

        .el-table__body tr.is-write-off-customer-selected > td.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        .el-table__body tr.is-write-off-customer-selected:hover > td.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        .el-table__body tr.is-write-off-customer-selected > td.el-table__cell:first-child {
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

        .el-table__body tr.is-write-off-customer-selected td.customer-row-select-col.el-table__cell,
        .el-table__body tr.is-write-off-customer-selected td.customer-row-select-col.el-table-fixed-column--right,
        .el-table__fixed-right .el-table__body tr.is-write-off-customer-selected td.customer-row-select-col.el-table__cell {
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

    &--center {
      .detail-panel-section-body.write-off-panel__body {
        background: var(--crm-detail-panel-card-bg);
      }

      :deep(.receipt-panel-table) {
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
      flex: 1 1 auto;
      min-width: 180px;

      :deep(.receivable-panel-table) {
        th.write-off-amount-col.el-table__cell {
          background: var(--crm-detail-panel-card-head-bg) !important;
          background-color: var(--crm-detail-panel-card-head-bg) !important;
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
    justify-content: flex-start;
    gap: 16px;
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

      // 右固定列须不透明，避免横向滚动时主表列文字透出
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

      .el-table__fixed-right .el-table__body tr.is-write-off-customer-selected td.el-table__cell,
      .el-table__body tr.is-write-off-customer-selected td.el-table-fixed-column--right {
        background: #fffaea !important;
        background-color: #fffaea !important;
      }

      .el-table__fixed-right .el-table__body tr.is-write-off-receipt-selected td.el-table__cell,
      .el-table__body tr.is-write-off-receipt-selected td.el-table-fixed-column--right {
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

  .panel-search {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-shrink: 0;
    margin-bottom: 12px;
  }

  .panel-search__input {
    flex: 1;
    min-width: 0;
  }

  .panel-search__clear {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    border: none;
    background: transparent;
    color: var(--el-text-color-placeholder);
    cursor: pointer;
    font-size: 14px;
    line-height: 1;

    &:hover {
      color: var(--el-text-color-secondary);
    }
  }

  .panel-search__btn {
    flex-shrink: 0;
  }

  .panel-search__filter {
    flex-shrink: 0;
    margin-left: 4px;
    white-space: nowrap;

    :deep(.el-checkbox__label) {
      font-size: 12px;
      padding-left: 6px;
    }
  }

  .panel-toggle {
    flex-shrink: 0;
    width: 24px;
    height: 24px;
    border: 1px solid $border-panel;
    border-radius: 6px;
    background: $layer-2;
    color: $text-secondary;
    cursor: pointer;
    line-height: 1;
    padding: 0;

    &:hover {
      border-color: var(--crm-accent-025);
      color: $text-primary;
    }
  }

  .write-off-amount-input {
    width: 13ch;
    min-width: 13ch;
    flex-shrink: 0;

    :deep(.el-input__wrapper) {
      background-color: #fffaea !important;
      box-shadow: 0 0 0 1px #f5e6a8 inset;
      padding-left: 8px;
      padding-right: 10px;
    }

    :deep(.el-input__inner) {
      font-variant-numeric: tabular-nums;
      text-align: right;
      padding-right: 2px;
    }

    &--positive {
      :deep(.el-input) {
        --el-input-text-color: var(--crm-success-color);
      }

      :deep(.el-input__inner) {
        color: var(--crm-success-color) !important;
        -webkit-text-fill-color: var(--crm-success-color) !important;
        font-weight: 700;
      }
    }
  }

  .footer-actions {
    margin-top: 4px;
    display: flex;
    gap: 12px;
    justify-content: flex-end;
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
