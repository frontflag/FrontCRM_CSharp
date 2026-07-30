<template>
  <div class="finance-page">
    <h1 class="finance-list-page-title">{{ t('financePaymentList.pageTitle') }}</h1>
    <!-- 统计卡片（置顶） -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.monthTotal') }}</div>
        <div class="stat-value">¥ {{ formatAmount(stats.monthTotal) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.pending') }}</div>
        <div class="stat-value warning">{{ stats.pendingCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.paid') }}</div>
        <div class="stat-value success">{{ stats.paidCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.draft') }}</div>
        <div class="stat-value">{{ stats.draftCount }}</div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-if="tabModeDimension !== 'status'"
          v-model="query.status"
          :placeholder="t('financePaymentList.filters.status')"
          clearable
          class="filter-select"
          @change="loadData"
        >
          <el-option
            v-for="k in paymentStatusSelectKeys"
            :key="k"
            :label="paymentStatusLabel(k)"
            :value="k"
          />
        </el-select>
        <el-input
          v-model="query.financePaymentCode"
          :placeholder="t('financePaymentList.filters.financePaymentCode')"
          clearable
          class="search-input search-input--code"
          @keyup.enter="loadData"
          @clear="loadData"
        />
        <el-input
          v-model="query.freightForwarderOrderNo"
          :placeholder="t('financePaymentList.filters.freightForwarderOrderNo')"
          clearable
          class="search-input search-input--ff"
          @keyup.enter="loadData"
          @clear="loadData"
        />
        <el-input
          v-model="query.bankSlipNo"
          :placeholder="t('financePaymentList.filters.bankSlipNo')"
          clearable
          class="search-input search-input--slip"
          @keyup.enter="loadData"
          @clear="loadData"
        />
        <el-select
          v-if="tabModeDimension !== 'paymentMode'"
          v-model="query.paymentMode"
          :placeholder="t('financePaymentList.filters.paymentMode')"
          clearable
          class="filter-select filter-select--mode"
          @change="loadData"
        >
          <el-option
            v-for="k in paymentModeSelectKeys"
            :key="k"
            :label="paymentModeLabel(k)"
            :value="k"
          />
        </el-select>
        <el-input
          v-model="query.vendorName"
          :placeholder="t('financePaymentList.filters.vendorName')"
          clearable
          class="search-input search-input--vendor"
          @keyup.enter="loadData"
          @clear="loadData"
        />
        <el-input
          v-model="query.remark"
          :placeholder="t('financePaymentList.filters.remark')"
          clearable
          class="search-input search-input--remark"
          @keyup.enter="loadData"
          @clear="loadData"
        />
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('financePaymentList.filters.to')"
          :start-placeholder="t('financePaymentList.filters.startDate')"
          :end-placeholder="t('financePaymentList.filters.endDate')"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          class="date-picker"
          @change="loadData"
        />
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> {{ t('financePaymentList.filters.search') }}
        </el-button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="fp-list-settings-popper"
        >
          <template #reference>
            <el-button
              class="fp-settings-gear-btn"
              :title="t('financePaymentList.settingsMenu.aria')"
              :aria-label="t('financePaymentList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </el-button>
          </template>
          <div class="fp-list-settings-menu">
            <button
              type="button"
              class="fp-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('financePaymentList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="fp-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="fp-list-settings-menu__item fp-list-settings-menu__item--parent">
                <span>{{ t('financePaymentList.settingsMenu.tabMode') }}</span>
                <el-icon class="fp-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="fp-list-settings-menu__flyout">
                <button
                  v-for="dim in FINANCE_PAYMENT_LIST_TAB_MODE_OPTIONS"
                  :key="dim"
                  type="button"
                  class="fp-list-settings-menu__item"
                  :class="{ 'is-active': tabModeDimension === dim }"
                  @click="enableFilterTabMode(dim)"
                >
                  {{ tabModeDimensionLabel(dim) }}
                </button>
              </div>
            </div>
          </div>
        </el-popover>
      </div>
    </div>

    <div class="fp-main-panel" :class="{ 'fp-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="fp-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="fp-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <!-- 数据表格 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-payment-list-main-v4"
      :columns="paymentTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableData"
      v-loading="loading"
      @row-dblclick="openDetail"
      @header-dragend="onPaymentTableHeaderDragEnd"
      row-class-name="table-row-pointer"
    >
      <template #col-freightForwarderOrderNo="{ row }">
        <CrmListCopyableTextCell :text="paymentRowFreightForwarderOrderNo(row)" />
      </template>
      <template #col-financePaymentCode="{ row }">
        <span class="code-text">{{ row.financePaymentCode }}</span>
      </template>
      <template #col-status="{ row }">
        <el-tag effect="dark" :type="paymentStatusTag(row.status) as any" size="small">
          {{ paymentStatusLabel(row.status) }}
        </el-tag>
      </template>
      <template #col-vendor-header>
        <VendorExtendColumnHeader
          :active-field="vendorExtendActiveField"
          @set-active-field="setVendorExtendActiveField"
        />
      </template>
      <template #col-vendor="{ row }">
        <VendorExtendCell
          :row="row"
          :active-field="vendorExtendActiveField"
          :masked="maskPurchaseSensitiveFields"
          :empty-text="t('quoteList.na')"
        />
      </template>
      <template #col-vendorReceivingBank-header>
        <VendorReceivingBankExtendColumnHeader
          :active-field="vendorReceivingBankExtendActiveField"
          @set-active-field="setVendorReceivingBankExtendActiveField"
        />
      </template>
      <template #col-vendorReceivingBank="{ row }">
        <VendorReceivingBankExtendCell
          :row="row"
          :active-field="vendorReceivingBankExtendActiveField"
          :masked="maskPurchaseSensitiveFields"
          :empty-text="t('quoteList.na')"
        />
      </template>
      <template #col-paymentBankName="{ row }">
        <span>{{ maskPurchaseSensitiveFields ? '—' : (paymentRowBankName(row) || '—') }}</span>
      </template>
      <template #col-paymentAmountToBe="{ row }">
        <span class="amount-text amount-text--request">{{ CURRENCY_MAP[row.paymentCurrency] }} {{ formatAmount(row.paymentAmountToBe ?? 0) }}</span>
      </template>
      <template #col-paymentAmount="{ row }">
        <span class="amount-text amount-text--paid">{{ CURRENCY_MAP[row.paymentCurrency] }} {{ formatAmount(row.paymentAmount) }}</span>
      </template>
      <template #col-paymentMode="{ row }">{{ paymentModeLabel(row.paymentMode) }}</template>
      <template #col-paymentDate="{ row }">{{ row.paymentDate ? formatDisplayDate(row.paymentDate) : '-' }}</template>
      <template #col-bankSlipNo="{ row }">{{ (row as any).bankSlipNo || '-' }}</template>
      <template #col-createdAt="{ row }">
        {{ paymentRowCreateTime(row) ? formatDisplayDateTime(paymentRowCreateTime(row)!) : '-' }}
      </template>
      <template #col-createUser="{ row }">
        {{ (row as any).createUserName || (row as any).createdBy || (row as any).paymentUserName || '-' }}
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
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">{{ t('financePaymentList.actions.detail') }}</el-button>
            <el-button
              size="small"
              text
              type="primary"
              v-if="canEditRequest(row)"
              @click.stop="openEditRequest(row)"
            >
              {{ t('financePaymentList.actions.editRequest') }}
            </el-button>
            <el-button
              size="small"
              text
              type="warning"
              v-if="canPayExecute(row)"
              @click.stop="openPay(row)"
            >
              {{ t('financePaymentList.actions.pay') }}
            </el-button>
            <el-button
              size="small"
              text
              type="info"
              v-if="canWithdrawPayment(row)"
              @click.stop="withdrawPayment(row)"
            >
              {{ t('financePaymentList.actions.withdraw') }}
            </el-button>
            <el-button
              size="small"
              text
              type="warning"
              @click.stop="submitAudit(row)"
              v-if="canSubmitAudit(row)"
            >
              {{ t('financePaymentList.actions.submitAudit') }}
            </el-button>
            <el-button
              size="small"
              text
              type="danger"
              @click.stop="cancelPayment(row)"
              v-if="canFinancePaymentWrite && [1,2].includes(row.status)"
            >
              {{ t('financePaymentList.actions.cancel') }}
            </el-button>
            <el-button size="small" text type="danger" @click.stop="handleDeleteRow(row)" v-if="canFinancePaymentWrite">删除</el-button>
            <el-button
              size="small"
              text
              type="warning"
              @click.stop="handleReverseVerificationRow(row)"
              v-if="canReverseVerification(row)"
            >
              {{ t('financePaymentList.actions.reverseVerification') }}
            </el-button>
            <el-button size="small" text type="danger" @click.stop="handleForceDeleteRow(row)" v-if="canFinancePaymentWrite && canForceDelete">强制删除</el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financePaymentList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canEditRequest(row)" @click.stop="openEditRequest(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financePaymentList.actions.editRequest') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canPayExecute(row)" @click.stop="openPay(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financePaymentList.actions.pay') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWithdrawPayment(row)" @click.stop="withdrawPayment(row)">
                  <span class="op-more-item op-more-item--info">{{ t('financePaymentList.actions.withdraw') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canSubmitAudit(row)" @click.stop="submitAudit(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financePaymentList.actions.submitAudit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="canFinancePaymentWrite && [1,2].includes(row.status)"
                  @click.stop="cancelPayment(row)"
                >
                  <span class="op-more-item op-more-item--danger">{{ t('financePaymentList.actions.cancel') }}</span>
                </el-dropdown-item>
                <el-dropdown-item divided v-if="canFinancePaymentWrite" @click.stop="handleDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canReverseVerification(row)" @click.stop="handleReverseVerificationRow(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financePaymentList.actions.reverseVerification') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canFinancePaymentWrite && canForceDelete" @click.stop="handleForceDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">强制删除</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
    <div class="pagination-wrap">
      <div class="list-footer-left">
        <el-tooltip :content="t('financePaymentList.columnSettings')" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" :aria-label="t('financePaymentList.columnSettings')" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @size-change="loadData"
        @current-change="loadData"
      />
    </div>
    </div>

    <FinancePaymentRequestEditDialog
      v-model="editDialogVisible"
      :payment-id="editPaymentId"
      @success="loadData"
    />
    <FinancePaymentPayDialog
      v-model="payDialogVisible"
      :payment="payPayment"
      @success="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { ArrowRight, Search, Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  financePaymentApi,
  PAYMENT_STATUS_MAP,
  PAYMENT_MODE_MAP,
  CURRENCY_MAP,
  type FinancePayment,
  type PageQuery,
} from '@/api/finance'
import {
  FINANCE_PAYMENT_LIST_TAB_MODE_OPTIONS,
  FP_STATUS_TAB_VALUES,
  FP_PAYMENT_MODE_TAB_VALUES,
  readFinancePaymentListTabMode,
  writeFinancePaymentListTabMode,
  fpStatusFilterToTab,
  fpStatusTabToFilter,
  fpPaymentModeFilterToTab,
  fpPaymentModeTabToFilter,
  type FinancePaymentListTabModeDimension,
  type FpStatusTabId,
  type FpPaymentModeTabId
} from '@/utils/financePaymentListTabMode'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useAuthStore } from '@/stores/auth'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useFinanceWriteGate, usePurchaseOrderWriteGate } from '@/composables/useDepartmentDataReadOnly'
import FinancePaymentRequestEditDialog from '@/components/Finance/FinancePaymentRequestEditDialog.vue'
import FinancePaymentPayDialog from '@/components/Finance/FinancePaymentPayDialog.vue'
import VendorExtendColumnHeader from '@/components/list/VendorExtendColumnHeader.vue'
import VendorExtendCell from '@/components/list/VendorExtendCell.vue'
import VendorReceivingBankExtendColumnHeader from '@/components/list/VendorReceivingBankExtendColumnHeader.vue'
import VendorReceivingBankExtendCell from '@/components/list/VendorReceivingBankExtendCell.vue'
import { useVendorExtendColumn, isVendorExtendTableColumn } from '@/composables/useVendorExtendColumn'
import {
  useVendorReceivingBankExtendColumn,
  isVendorReceivingBankExtendTableColumn
} from '@/composables/useVendorReceivingBankExtendColumn'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const {
  expanded: vendorExtendExpanded,
  activeField: vendorExtendActiveField,
  colWidth: vendorExtendColWidth,
  colMinWidth: vendorExtendColMinWidth,
  setActiveField: setVendorExtendActiveField,
  applyOuterWidthFromTable: applyVendorExtendOuterWidth
} = useVendorExtendColumn()
const {
  expanded: vendorReceivingBankExtendExpanded,
  activeField: vendorReceivingBankExtendActiveField,
  colWidth: vendorReceivingBankExtendColWidth,
  colMinWidth: vendorReceivingBankExtendColMinWidth,
  setActiveField: setVendorReceivingBankExtendActiveField,
  applyOuterWidthFromTable: applyVendorReceivingBankExtendOuterWidth
} = useVendorReceivingBankExtendColumn()

function onPaymentTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (isVendorExtendTableColumn(column)) {
    applyVendorExtendOuterWidth(newWidth)
    return
  }
  if (isVendorReceivingBankExtendTableColumn(column)) {
    applyVendorReceivingBankExtendOuterWidth(newWidth)
  }
}

/** 付款保存/完成/提交审核等：RBAC write + 主部门财务非只读 */
const { canWriteFinancePayment: canFinancePaymentWrite } = useFinanceWriteGate()
const { canWritePo } = usePurchaseOrderWriteGate()
const canForceDelete = computed(() => authStore.canForceDelete())
const { paymentStatusLabel, paymentStatusTag, paymentModeLabel } = useFinanceEnumLabels()

const paymentStatusSelectKeys = Object.keys(PAYMENT_STATUS_MAP).map(k => Number(k))
const paymentModeSelectKeys = Object.keys(PAYMENT_MODE_MAP).map(k => Number(k))

// 查询
const query = reactive<PageQuery & { page: number; pageSize: number }>({
  page: 1,
  pageSize: 20,
  financePaymentCode: '',
  freightForwarderOrderNo: '',
  bankSlipNo: '',
  paymentMode: undefined,
  vendorName: '',
  remark: '',
  status: undefined,
  startDate: undefined,
  endDate: undefined,
})
const dateRange = ref<[string, string] | null>(null)
const total = ref(0)
const loading = ref(false)
const tabModeDimension = ref<FinancePaymentListTabModeDimension>(readFinancePaymentListTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)
const tableData = ref<FinancePayment[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const TAB_MODE_FILTER_I18N: Record<Exclude<FinancePaymentListTabModeDimension, 'off'>, string> = {
  status: 'financePaymentList.filters.status',
  paymentMode: 'financePaymentList.filters.paymentMode'
}

function tabModeDimensionLabel(dim: Exclude<FinancePaymentListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeFinancePaymentListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<FinancePaymentListTabModeDimension, 'off'>) {
  tabModeDimension.value = dim
  writeFinancePaymentListTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const filterTabStripVisible = computed(() => tabModeDimension.value !== 'off')

const filterTabStripAriaLabel = computed(() => {
  if (tabModeDimension.value === 'off') return ''
  return tabModeDimensionLabel(tabModeDimension.value)
})

type FpFilterTabId = FpStatusTabId | FpPaymentModeTabId

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return [] as Array<{ id: FpFilterTabId; label: string }>
  if (dim === 'status') {
    return [
      { id: 'all' as const, label: t('financePaymentList.filterTabs.all') },
      ...FP_STATUS_TAB_VALUES.map((value) => ({
        id: String(value) as FpStatusTabId,
        label: paymentStatusLabel(value)
      }))
    ]
  }
  return [
    { id: 'all' as const, label: t('financePaymentList.filterTabs.all') },
    ...FP_PAYMENT_MODE_TAB_VALUES.map((value) => ({
      id: String(value) as FpPaymentModeTabId,
      label: paymentModeLabel(value)
    }))
  ]
})

const activeFilterTabId = computed((): FpFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'status') return fpStatusFilterToTab(query.status)
  if (dim === 'paymentMode') return fpPaymentModeFilterToTab(query.paymentMode)
  return 'all'
})

function onFilterTabClick(tab: FpFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'status') {
    const next = fpStatusTabToFilter(tab as FpStatusTabId)
    if (query.status === next) return
    query.status = next
    void loadData()
    return
  }
  if (dim === 'paymentMode') {
    const next = fpPaymentModeTabToFilter(tab as FpPaymentModeTabId)
    if (query.paymentMode === next) return
    query.paymentMode = next
    void loadData()
  }
}

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 400
const OP_COL_EXPANDED_MIN_WIDTH = 380
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

/** 后端序列化为 createTime；旧 mock/别名可能用 createdAt；PascalCase 需经 unknown 再读 */
function paymentRowCreateTime(row: FinancePayment): string | undefined {
  const ext = row as unknown as Record<string, unknown>
  const raw = row.createTime || row.createdAt || ext.CreateTime
  const s = raw != null ? String(raw).trim() : ''
  return s || undefined
}

function paymentRowBankName(row: FinancePayment): string {
  const ext = row as unknown as Record<string, unknown>
  const raw = row.paymentBankName ?? ext.PaymentBankName ?? ext.paymentBankName
  const s = raw != null ? String(raw).trim() : ''
  return s
}

function paymentRowFreightForwarderOrderNo(row: FinancePayment): string {
  const ext = row as unknown as Record<string, unknown>
  const raw = row.freightForwarderOrderNo ?? ext.FreightForwarderOrderNo ?? ext.freightForwarderOrderNo
  const s = raw != null ? String(raw).trim() : ''
  return s
}

const paymentTableColumns = computed<CrmTableColumnDef[]>(() => {
  void vendorExtendExpanded.value
  void vendorExtendColWidth.value
  void vendorReceivingBankExtendExpanded.value
  void vendorReceivingBankExtendColWidth.value
  return [
  { key: 'status', label: t('financePaymentList.columns.status'), prop: 'status', width: 100, align: 'center' },
  {
    key: 'vendor',
    label: t('common.vendorExtendCol.columnTitle'),
    prop: 'vendor',
    minWidth: vendorExtendColMinWidth.value,
    width: vendorExtendColWidth.value,
    showOverflowTooltip: true,
    className: 'vendor-extend-col',
    labelClassName: 'vendor-extend-col'
  },
  {
    key: 'vendorReceivingBank',
    label: t('common.vendorReceivingBankExtendCol.columnTitle'),
    prop: 'vendorReceivingBank',
    minWidth: vendorReceivingBankExtendColMinWidth.value,
    width: vendorReceivingBankExtendColWidth.value,
    showOverflowTooltip: true,
    className: 'vendor-extend-col',
    labelClassName: 'vendor-extend-col'
  },
  {
    key: 'paymentBankName',
    label: t('financePaymentList.columns.paymentBank'),
    prop: 'paymentBankName',
    minWidth: 140,
    width: 160,
    showOverflowTooltip: true
  },
  {
    key: 'paymentAmountToBe',
    label: t('financePaymentList.columns.requestAmount'),
    prop: 'paymentAmountToBe',
    width: 200,
    minWidth: 180,
    align: 'right'
  },
  { key: 'paymentAmount', label: t('financePaymentList.columns.amount'), prop: 'paymentAmount', width: 200, minWidth: 180, align: 'right' },
  { key: 'paymentMode', label: t('financePaymentList.columns.mode'), prop: 'paymentMode', width: 110 },
  { key: 'paymentDate', label: t('financePaymentList.columns.date'), prop: 'paymentDate', width: 120 },
  { key: 'bankSlipNo', label: t('financePaymentList.columns.bankSlip'), prop: 'bankSlipNo', width: 150, showOverflowTooltip: true },
  { key: 'remark', label: t('financePaymentList.columns.remark'), prop: 'remark', minWidth: 140, showOverflowTooltip: true },
  {
    key: 'freightForwarderOrderNo',
    label: t('financePaymentList.columns.freightForwarderOrderNo'),
    prop: 'freightForwarderOrderNo',
    width: 160,
    minWidth: 140,
    showOverflowTooltip: true
  },
  { key: 'financePaymentCode', label: t('financePaymentList.columns.code'), prop: 'financePaymentCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'createdAt', label: t('financePaymentList.columns.createdAt'), prop: 'createdAt', width: 120 },
  { key: 'createUser', label: t('financePaymentList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('financePaymentList.columns.actions'),
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

// 统计
const stats = reactive({ monthTotal: 0, pendingCount: 0, paidCount: 0, draftCount: 0 })

const loadData = async () => {
  loading.value = true
  if (dateRange.value) {
    query.startDate = dateRange.value[0]
    query.endDate = dateRange.value[1]
  } else {
    query.startDate = undefined
    query.endDate = undefined
  }
  try {
    const res = await financePaymentApi.getList(query)
    tableData.value = res.items || []
    total.value = res.total || 0
    // 更新统计
    stats.monthTotal = tableData.value.filter(r => r.status === 100).reduce((s, r) => s + r.paymentAmount, 0)
    stats.pendingCount = tableData.value.filter(r => r.status === 2).length
    stats.paidCount = tableData.value.filter(r => r.status === 100).length
    stats.draftCount = tableData.value.filter(r => r.status === 1).length
  } catch (e: any) {
    tableData.value = []
    total.value = 0
    stats.monthTotal = 0
    stats.pendingCount = 0
    stats.paidCount = 0
    stats.draftCount = 0
    ElMessage.error(e?.message || t('financePaymentList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

const editDialogVisible = ref(false)
const editPaymentId = ref<string | null>(null)
const payDialogVisible = ref(false)
const payPayment = ref<FinancePayment | null>(null)

function paymentCreatorId(row: FinancePayment): string {
  const ext = row as unknown as Record<string, unknown>
  return String(row.createByUserId ?? ext.CreateByUserId ?? '').trim()
}

function canEditRequest(row: FinancePayment) {
  if (row.status !== 1 && row.status !== -1) return false
  return canFinancePaymentWrite.value || canWritePo.value
}

function canSubmitAudit(row: FinancePayment) {
  if (row.status !== 1) return false
  return canFinancePaymentWrite.value || canWritePo.value
}

function canPayExecute(row: FinancePayment) {
  return canFinancePaymentWrite.value && row.status === 10
}

function canReverseVerification(row: FinancePayment) {
  return canFinancePaymentWrite.value && row.status === 100
}

function canWithdrawPayment(row: FinancePayment) {
  if (row.status !== 10) return false
  if (canFinancePaymentWrite.value) return true
  const uid = String(authStore.user?.id ?? '').trim()
  const creator = paymentCreatorId(row)
  return !!uid && !!creator && uid === creator
}

function openEditRequest(row: FinancePayment) {
  editPaymentId.value = row.id
  editDialogVisible.value = true
}

function openPay(row: FinancePayment) {
  payPayment.value = row
  payDialogVisible.value = true
}

const withdrawPayment = async (row: FinancePayment) => {
  try {
    await ElMessageBox.confirm(
      t('financePaymentList.messages.withdrawMsg', { code: row.financePaymentCode }),
      t('financePaymentList.messages.withdrawTitle'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  try {
    await financePaymentApi.withdraw(row.id)
    ElMessage.success(t('financePaymentList.messages.withdrawn'))
    await loadData()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.withdrawFailed'))
  }
}

/// 详情
const openDetail = (row: FinancePayment) => {
  router.push({ name: 'FinancePaymentDetail', params: { id: row.id } })
}

// 状态操作
const submitAudit = async (row: FinancePayment) => {
  try {
    await ElMessageBox.confirm(
      t('financePaymentList.messages.submitAuditMsg', { code: row.financePaymentCode }),
      t('financePaymentList.messages.submitAuditTitle'),
      { type: 'info' }
    )
  } catch {
    return
  }
  try {
    await financePaymentApi.submit(row.id)
    ElMessage.success(t('financePaymentList.messages.submitted'))
    await loadData()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.operationFailed'))
  }
}

const cancelPayment = async (row: FinancePayment) => {
  try {
    await ElMessageBox.confirm(
      t('financePaymentList.messages.cancelMsg', { code: row.financePaymentCode }),
      t('financePaymentList.messages.cancelTitle'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  try {
    await financePaymentApi.cancel(row.id)
    ElMessage.success(t('financePaymentList.messages.cancelled'))
    await loadData()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.operationFailed'))
  }
}

const handleDeleteRow = async (row: FinancePayment) => {
  try {
    await ElMessageBox.confirm(`确认删除付款单 ${row.financePaymentCode} 吗？`, '删除确认', { type: 'warning' })
  } catch {
    return
  }
  try {
    await financePaymentApi.delete(row.id)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: any) {
    ElMessage.error(e?.message || '删除失败')
  }
}

const handleReverseVerificationRow = async (row: FinancePayment) => {
  const entered = window.prompt(
    t('financePaymentList.messages.reverseVerificationPrompt'),
    row.financePaymentCode || ''
  )?.trim() ?? ''
  if (!entered) return
  if (entered !== String(row.financePaymentCode || '').trim()) {
    ElMessage.error(t('financePaymentList.messages.reverseVerificationBillMismatch'))
    return
  }
  try {
    await financePaymentApi.reverseVerification(row.id, entered)
    ElMessage.success(t('financePaymentList.messages.reverseVerificationSuccess'))
    await loadData()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.reverseVerificationFailed'))
  }
}

const handleForceDeleteRow = async (row: FinancePayment) => {
  const entered = window.prompt('请输入付款单号以确认强制删除', row.financePaymentCode || '')?.trim() ?? ''
  if (!entered) return
  if (entered !== String(row.financePaymentCode || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await financePaymentApi.forceDelete(row.id, entered)
    ElMessage.success('强制删除成功')
    await loadData()
  } catch (e: any) {
    ElMessage.error(e?.message || '强制删除失败')
  }
}

const formatAmount = (v: number) => v?.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || '0.00'

onMounted(loadData)
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;
@import './finance-common.scss';

.pagination-wrap {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
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

.search-input--code,
.search-input--ff,
.search-input--slip {
  width: 160px;
}

.search-input--vendor {
  width: 150px;
}

.search-input--remark {
  width: 140px;
}

.filter-select--mode {
  width: 130px;
}

.amount-text--request {
  color: var(--el-color-warning, var(--crm-warning-color));
  font-weight: 400;
}

.amount-text--paid {
  color: vars.$success-color;
  font-weight: 600;
}

.fp-settings-gear-btn {
  padding: 8px 10px;
}

.fp-main-panel {
  width: 100%;
}

.fp-main-panel--with-filter-tabs {
  :deep(.crm-data-table-root) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }

  :deep(.el-table),
  :deep(.el-table__inner-wrapper),
  :deep(.el-table__header-wrapper) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }
}

.fp-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.fp-filter-tabs__item {
  flex: 1 1 0;
  min-width: 0;
  padding: 9px 8px;
  border: 1px solid var(--crm-border-panel, #e2e8f0);
  border-bottom: none;
  border-radius: 8px 8px 0 0;
  background: #e8edf5;
  color: var(--crm-text-primary);
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
  text-align: center;
  cursor: pointer;
  transition: background 0.12s, border-color 0.12s, color 0.12s, box-shadow 0.12s;

  &:hover {
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 45%, var(--crm-border-panel));
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }

  &.is-active {
    background: color-mix(in srgb, var(--crm-cyan-primary) 16%, var(--crm-layer-2, #fff));
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 55%, var(--crm-border-panel));
    box-shadow: inset 0 2px 0 0 var(--crm-cyan-primary);
    font-weight: 600;
    z-index: 1;
  }
}

html[data-theme='dark'] .fp-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}

</style>

<style lang="scss">
.fp-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.fp-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.fp-list-settings-menu__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 8px 10px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--crm-text-secondary, rgba(224, 244, 255, 0.7));
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  text-align: left;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: var(--crm-accent-008, rgba(0, 212, 255, 0.08));
    color: var(--crm-text-primary, #e8f4ff);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  &.is-active {
    color: var(--crm-cyan-primary, #00d4ff);
  }

  &--parent {
    cursor: default;
  }
}

.fp-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.fp-list-settings-menu__submenu {
  position: relative;
}

.fp-list-settings-menu__flyout {
  position: absolute;
  top: 0;
  left: calc(100% + 4px);
  min-width: 148px;
  padding: 6px;
  border-radius: 8px;
  border: 1px solid var(--crm-border-panel, rgba(0, 212, 255, 0.15));
  background: var(--crm-layer-2, #0d1e35);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.28);
  z-index: 10;
}
</style>
