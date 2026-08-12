<template>
  <div class="finance-page write-off-page" :class="{ 'write-off-page--embedded': embedded }">
    <div v-if="!embedded" class="write-off-page-header">
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

    <div
      ref="layoutRef"
      class="write-off-layout"
      :class="{ 'is-resizing': !!dragging, 'write-off-layout--embedded': embedded }"
      v-loading="loading"
    >
      <aside
        v-if="!embedded"
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
        v-if="!embedded && !leftPanelCollapsed"
        class="col-splitbar"
        :class="{ 'is-dragging': dragging === 'left' }"
        :title="t('financeReceiptWriteOff.dragLeftWidth')"
        @mousedown="onSplitStart('left', $event)"
      />

      <div
        ref="mainPairRef"
        class="write-off-main-pair"
        :class="
          panelsLayout === 'column' ? 'write-off-main-pair--column' : 'write-off-main-pair--row'
        "
      >
      <section class="info-section write-off-panel write-off-panel--center" :style="centerPanelStyle">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('financeReceiptWriteOff.receiptPanelTitle') }}</span>
            <span v-if="selectedCustomerId && filteredReceiptItems.length" class="section-count">
              {{ filteredReceiptItems.length }}
            </span>
          </div>
          <div class="section-header__layout" role="group" :aria-label="t('financeReceiptWriteOff.panelsLayoutGroup')">
            <el-tooltip :content="t('financeReceiptWriteOff.panelsLayoutRow')" placement="top" :hide-after="0">
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
            <el-tooltip :content="t('financeReceiptWriteOff.panelsLayoutColumn')" placement="top" :hide-after="0">
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
            <span class="panel-info-bar__label">{{ t('financeReceiptWriteOff.currentCustomerLabel') }}</span>
            <template v-if="!selectedCustomerSummary">
              <span class="panel-info-bar__hint panel-info-bar__hint--placeholder">
                {{ t('financeReceiptWriteOff.currentCustomerHint') }}
              </span>
            </template>
            <template v-else>
              <span class="panel-info-bar__customer">
                <span class="panel-hint__value">{{ currentCustomerNameZh(selectedCustomerSummary) }}</span>
                <template v-if="currentCustomerNameEn(selectedCustomerSummary)">
                  <span class="panel-hint__sep"> / </span>
                  <span class="panel-hint__value">{{ currentCustomerNameEn(selectedCustomerSummary) }}</span>
                </template>
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
              @row-click="onReceiptRowSelect"
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
        class="pair-splitbar"
        :class="{
          'pair-splitbar--col': panelsLayout === 'row',
          'pair-splitbar--row': panelsLayout === 'column',
          'is-dragging': dragging === 'center'
        }"
        :title="
          panelsLayout === 'column'
            ? t('financeReceiptWriteOff.dragCenterHeight')
            : t('financeReceiptWriteOff.dragCenterWidth')
        "
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
          <div
            class="receivable-main"
            :class="{ 'is-overflow': receivableListOverflow }"
          >
          <div ref="receivableScrollRef" class="receivable-scroll">
          <div class="detail-items-table-wrap detail-items-table-wrap--receivable">
            <DetailListPanelEmpty
              v-if="!selectedCustomerId"
              size="medium"
              :description="t('financeReceiptWriteOff.selectCustomerHint')"
            />
            <el-table
              v-else
              ref="receivableTableRef"
              :data="filteredReceivableRows"
              class="detail-panel-list-table receivable-panel-table"
              size="small"
              stripe
              :highlight-current-row="embedded"
              :row-class-name="receivableRowClassName"
              @row-click="onReceivableRowClick"
              @current-change="onReceivableCurrentChange"
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
          <div
            v-if="selectedCustomerId && canWriteFinanceReceipt"
            class="receivable-submit-bar"
          >
            <el-button
              v-if="!embedded"
              @click="router.push({ name: 'FinanceReceivableList' })"
            >
              {{ t('financeReceiptWriteOff.back') }}
            </el-button>
            <el-button type="primary" :loading="submitting" @click="submitWriteOff">
              {{ t('financeReceiptWriteOff.submit') }}
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
import {
  computed,
  nextTick,
  onBeforeUnmount,
  onMounted,
  ref,
  watch
} from 'vue'
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
import { useReceiptWriteOffDesktopQueueStore } from '@/stores/receiptWriteOffDesktopQueue'
import {
  readReceiptWriteOffPanelsLayout,
  writeReceiptWriteOffPanelsLayout,
  type ReceiptWriteOffPanelsLayout
} from '@/utils/receiptWriteOffPanelsLayout'

type ReceivableWriteOffRow = FinanceReceivableWriteOffCandidateRow & {
  writeOffAmount: number
  poolAmount: number
}

const props = withDefaults(
  defineProps<{
    /** 收款核销桌面嵌入：隐藏左栏客户列表与页头 */
    embedded?: boolean
    embedCustomerId?: string
    embedCurrency?: number | null
    embedSummary?: FinanceWriteOffCustomerSummary | null
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
const router = useRouter()
const receiptWriteOffDesktopQueueStore = useReceiptWriteOffDesktopQueueStore()
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
const mainPairRef = ref<HTMLElement | null>(null)
const customerTableRef = ref<{ doLayout?: () => void } | null>(null)
const customerTableWrapRef = ref<HTMLElement | null>(null)
const leftPanelWidthPx = ref(0)
/** 收款面板在分割轴上的尺寸：左右模式为宽度，上下模式为高度 */
const centerPanelSizePx = ref(0)
const dragging = ref<'left' | 'center' | null>(null)
const panelsLayout = ref<ReceiptWriteOffPanelsLayout>(readReceiptWriteOffPanelsLayout())

const SPLIT_BAR_WIDTH = 6
const MIN_PANEL_WIDTH = 180
const MIN_PANEL_HEIGHT = 140
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
let dragStartY = 0
let dragStartLeft = 0
let dragStartCenter = 0

function clamp(n: number, min: number, max: number) {
  return Math.min(max, Math.max(min, n))
}

function isColumnLayout() {
  return panelsLayout.value === 'column'
}

/** 外层 layout 横向可用宽度（含左栏时的分割条） */
function leftSplitBarCount() {
  if (props.embedded) return 0
  return leftPanelCollapsed.value ? 0 : 1
}

function usableLayoutWidth() {
  const total = layoutRef.value?.clientWidth ?? 0
  return Math.max(0, total - SPLIT_BAR_WIDTH * leftSplitBarCount())
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

function initPanelWidths() {
  if (!props.embedded && !leftPanelCollapsed.value) {
    const usable = usableLayoutWidth()
    if (usable > 0) {
      leftPanelWidthPx.value = clamp(
        leftPanelWidthPx.value || Math.round(usable * DEFAULT_LEFT_RATIO),
        MIN_PANEL_WIDTH,
        Math.max(MIN_PANEL_WIDTH, usable - MIN_PANEL_WIDTH)
      )
    }
  }

  void nextTick(() => {
    const usablePair = usablePairSize()
    if (usablePair <= 0) return
    const min = centerMinSize()
    const half = Math.round(usablePair * DEFAULT_LEFT_RATIO)
    centerPanelSizePx.value = clamp(half, min, Math.max(min, usablePair - min))
  })
}

function setPanelsLayout(layout: ReceiptWriteOffPanelsLayout) {
  if (panelsLayout.value === layout) return
  panelsLayout.value = layout
  writeReceiptWriteOffPanelsLayout(layout)
  centerPanelSizePx.value = 0
  void nextTick(() => {
    initPanelWidths()
    setupReceivableScrollObserver()
    updateReceivableListOverflow()
  })
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

function onSplitStart(which: 'left' | 'center', e: MouseEvent) {
  e.preventDefault()
  if (centerPanelSizePx.value <= 0) initPanelWidths()
  dragging.value = which
  dragStartX = e.clientX
  dragStartY = e.clientY
  dragStartLeft = leftPanelWidthPx.value
  dragStartCenter = centerPanelSizePx.value
  document.body.style.cursor = which === 'center' && isColumnLayout() ? 'row-resize' : 'col-resize'
  document.body.style.userSelect = 'none'
}

function onSplitMove(e: MouseEvent) {
  if (!dragging.value) return

  if (dragging.value === 'left') {
    if (props.embedded) return
    const dx = e.clientX - dragStartX
    const usable = usableLayoutWidth()
    const maxLeft = usable - MIN_PANEL_WIDTH
    leftPanelWidthPx.value = clamp(dragStartLeft + dx, MIN_PANEL_WIDTH, Math.max(MIN_PANEL_WIDTH, maxLeft))
    return
  }

  const min = centerMinSize()
  const usablePair = usablePairSize()
  const delta = isColumnLayout() ? e.clientY - dragStartY : e.clientX - dragStartX
  // usablePairSize 已扣分割条；拖动时用当前 pair 总尺寸重算更稳
  const pair = mainPairRef.value
  const pairTotal = isColumnLayout()
    ? (pair?.clientHeight ?? 0) - SPLIT_BAR_WIDTH
    : (pair?.clientWidth ?? 0) - SPLIT_BAR_WIDTH
  const max = Math.max(min, (pairTotal || usablePair) - min)
  centerPanelSizePx.value = clamp(dragStartCenter + delta, min, max)
}

function onSplitEnd() {
  if (!dragging.value) return
  dragging.value = null
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
  relayoutCustomerTable()
  void nextTick(() => {
    setupReceivableScrollObserver()
    updateReceivableListOverflow()
  })
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
const receivableScrollRef = ref<HTMLElement | null>(null)
const receivableTableRef = ref<{ setCurrentRow?: (row?: ReceivableWriteOffRow | null) => void } | null>(null)
const receivableListOverflow = ref(false)
let receivableScrollResizeObserver: ResizeObserver | null = null
const focusedReceivableId = ref('')

const receiptItems = ref<FinanceReceiptItemWriteOffCandidate[]>([])
const receivableRows = ref<ReceivableWriteOffRow[]>([])
const advanceBalances = ref<FinanceCustomerAdvanceBalance[]>([])
const selectedReceiptItemId = ref('')

function toFocusedReceivablePayload(row: ReceivableWriteOffRow): FinanceReceivableWriteOffCandidateRow {
  const { writeOffAmount: _w, poolAmount: _p, ...rest } = row
  return rest
}

function syncFocusedReceivableToDesktop(row: ReceivableWriteOffRow | null) {
  if (!props.embedded) return
  focusedReceivableId.value = row?.id || ''
  receiptWriteOffDesktopQueueStore.setFocusedReceivable(row ? toFocusedReceivablePayload(row) : null)
}

function pickDefaultFocusedReceivable() {
  if (!props.embedded) return
  const rows = filteredReceivableRows.value
  if (!rows.length) {
    syncFocusedReceivableToDesktop(null)
    void nextTick(() => receivableTableRef.value?.setCurrentRow?.(undefined))
    return
  }
  const still = focusedReceivableId.value
    ? rows.find((r) => r.id === focusedReceivableId.value)
    : undefined
  const next = still ?? rows[0]
  syncFocusedReceivableToDesktop(next)
  void nextTick(() => receivableTableRef.value?.setCurrentRow?.(next))
}

function onReceivableRowClick(row: ReceivableWriteOffRow) {
  if (!props.embedded) return
  syncFocusedReceivableToDesktop(row)
}

function onReceivableCurrentChange(row: ReceivableWriteOffRow | undefined) {
  if (!props.embedded || !row) return
  syncFocusedReceivableToDesktop(row)
}

function receivableRowClassName({ row }: { row: ReceivableWriteOffRow }) {
  if (props.embedded && row.id && row.id === focusedReceivableId.value) {
    return 'is-rwo-receivable-focused'
  }
  return ''
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
  const contentH = scroll.scrollHeight
  receivableListOverflow.value = contentH > available + 1
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
  return row.customerEnglishName?.trim() || ''
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
  syncFocusedReceivableToDesktop(null)
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
  const matchedSummary =
    findCustomerSummaryRow(customerId, currency) ||
    (props.embedSummary &&
    props.embedSummary.customerId === customerId &&
    (currency == null || props.embedSummary.currency === currency)
      ? props.embedSummary
      : null)
  if (matchedSummary) {
    selectedCustomerSummary.value = matchedSummary
  } else if (!selectedCustomerSummary.value || selectedCustomerSummary.value.customerId !== customerId) {
    selectedCustomerSummary.value =
      findCustomerSummaryRow(customerId, currency) ?? selectedCustomerSummary.value
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
    void nextTick(() => {
      pickDefaultFocusedReceivable()
      setupReceivableScrollObserver()
      updateReceivableListOverflow()
    })
  }
}

async function syncEmbeddedSelection(resetReceiptSelection: boolean) {
  const customerId = (props.embedCustomerId || '').trim()
  if (!customerId) {
    clearCustomerSelection()
    return
  }
  if (props.embedSummary && props.embedSummary.customerId === customerId) {
    selectedCustomerSummary.value = props.embedSummary
  }
  await loadCandidates(customerId, props.embedCurrency ?? null, resetReceiptSelection)
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
    if (props.embedded) {
      emit('applied')
      const customerId = selectedCustomerId.value
      const currency = selectedCurrency.value
      if (customerId) {
        await loadCandidates(customerId, currency, false)
      }
    } else {
      await loadCustomerSummaries()
    }
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
  void nextTick(() => updateReceivableListOverflow())
}

function goWriteOffLedger() {
  router.push({ name: 'FinanceReceiptWriteOffLedger' })
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

onMounted(() => {
  void nextTick(() => {
    initPanelWidths()
    setupReceivableScrollObserver()
    if (!props.embedded) {
      setupCustomerTableResizeObserver()
      relayoutCustomerTable()
      void loadCustomerSummaries()
    } else {
      void syncEmbeddedSelection(true)
    }
  })
  window.addEventListener('mousemove', onSplitMove)
  window.addEventListener('mouseup', onSplitEnd)
  window.addEventListener('resize', onWindowResize)
})
onBeforeUnmount(() => {
  customerTableResizeObserver?.disconnect()
  customerTableResizeObserver = null
  receivableScrollResizeObserver?.disconnect()
  receivableScrollResizeObserver = null
  if (props.embedded) {
    receiptWriteOffDesktopQueueStore.setFocusedReceivable(null)
  }
  window.removeEventListener('mousemove', onSplitMove)
  window.removeEventListener('mouseup', onSplitEnd)
  window.removeEventListener('resize', onWindowResize)
  onSplitEnd()
})

watch(
  () => [filteredReceivableRows.value.length, selectedCustomerId.value, !!selectedReceiptItem.value] as const,
  () => {
    void nextTick(() => {
      setupReceivableScrollObserver()
      updateReceivableListOverflow()
    })
  }
)

watch(
  () =>
    props.embedded
      ? filteredReceivableRows.value.map((r) => r.id).join('|')
      : '',
  () => {
    if (!props.embedded) return
    void nextTick(() => pickDefaultFocusedReceivable())
  }
)

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
  &.write-off-page--embedded {
    height: 100%;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 0;
  }

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

  .col-splitbar,
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

  .col-splitbar,
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
      min-width: 180px;

      .receivable-main {
        flex: 1 1 auto;
        min-height: 0;
        display: flex;
        flex-direction: column;
        justify-content: flex-start;
      }

      /* 行少：滚动区随内容高度，提交栏紧跟最后一行；行多：占满剩余空间并内部滚动 */
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
        .el-table__body tr.is-rwo-receivable-focused > td.el-table__cell {
          background: #fffaea !important;
          background-color: #fffaea !important;
        }

        .el-table__body tr.is-rwo-receivable-focused:hover > td.el-table__cell {
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
