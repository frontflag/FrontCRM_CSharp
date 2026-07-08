<template>
  <div class="finance-page ff-payable-list-page">
    <div class="page-header-row">
      <h1 class="finance-list-page-title">{{ t('financeFfPayableList.pageTitle') }}</h1>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financeFfPayableList.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="handleSearch"
          @clear="handleSearch"
        />
        <el-select
          v-model="query.payableStatus"
          :placeholder="t('financeFfPayableList.filters.status')"
          clearable
          class="filter-select"
          style="width: 130px"
          @change="handleSearch"
        >
          <el-option :label="t('financeFfPayableList.statusPending')" :value="FF_PAYABLE_STATUS.Pending" />
          <el-option :label="t('financeFfPayableList.statusPartial')" :value="FF_PAYABLE_STATUS.Partial" />
          <el-option :label="t('financeFfPayableList.statusCompleted')" :value="FF_PAYABLE_STATUS.Completed" />
        </el-select>
        <el-select
          v-model="query.freightForwarderCompanyId"
          :placeholder="t('financeFfPayableList.filters.ffCompany')"
          clearable
          filterable
          class="filter-select"
          style="width: 160px"
          @change="handleSearch"
        >
          <el-option v-for="c in ffCompanies" :key="c.id" :label="c.cname" :value="c.id" />
        </el-select>
        <el-button type="primary" @click="handleSearch">{{ t('financeFfPayableList.search') }}</el-button>
        <el-button @click="resetFilters">{{ t('financeFfPayableList.filters.reset') }}</el-button>
      </div>
      <div class="search-right">
        <el-button @click="goCompanyManage">{{ t('financeFfPayableList.manageCompanies') }}</el-button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-ff-payable-list-main"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableData"
      v-loading="loading"
      row-class-name="table-row-pointer"
      @row-dblclick="openDetail"
    >
      <template #col-payableStatus="{ row }">
        <el-tag :type="statusTagType(row.payableStatus)" size="small" effect="plain">
          {{ statusLabel(row.payableStatus) }}
        </el-tag>
      </template>
      <template #col-financeReceiptCode="{ row }">
        <span class="code-text">{{ row.financeReceiptCode }}</span>
      </template>
      <template #col-customerName="{ row }">
        <span>{{ row.customerName || '—' }}</span>
      </template>
      <template #col-freightForwarderCompanyName="{ row }">
        <span>{{ row.freightForwarderCompanyName || '—' }}</span>
      </template>
      <template #col-receiptAmount="{ row }">
        <span class="amount-text amount-text--receivable dock-quote-tier-line">{{ formatTotalAmountNumber(row.receiptAmount) }}</span>
      </template>
      <template #col-paidAmount="{ row }">
        <span class="amount-text amount-text--received dock-quote-tier-line">{{ formatTotalAmountNumber(row.paidAmount) }}</span>
      </template>
      <template #col-pendingAmount="{ row }">
        <span class="amount-text amount-text--pending dock-quote-tier-line">{{ formatTotalAmountNumber(row.pendingAmount) }}</span>
      </template>
      <template #col-receiptCurrency="{ row }">
        <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.receiptCurrency)]">
          {{ listAmountCurrencyIso(row.receiptCurrency) }}
        </span>
      </template>
      <template #col-receiptDate="{ row }">
        <span class="text-secondary">{{ row.receiptDate ? formatDisplayDate(row.receiptDate) : '—' }}</span>
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
            <el-button link type="primary" size="small" @click.stop="openDetail(row)">
              {{ t('financeFfPayableList.viewDetail') }}
            </el-button>
            <el-button
              v-if="canWriteFinanceReceipt && row.pendingAmount > 0"
              link
              type="warning"
              size="small"
              @click.stop="openPay(row)"
            >
              {{ t('financeFfPayableList.pay') }}
            </el-button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financeFfPayableList.viewDetail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="canWriteFinanceReceipt && row.pendingAmount > 0"
                  @click.stop="openPay(row)"
                >
                  <span class="op-more-item op-more-item--warning">{{ t('financeFfPayableList.pay') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div class="pagination-wrap">
      <div class="list-footer-left">
        <el-tooltip :content="t('financeFfPayableList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('financeFfPayableList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onPageSizeChange"
        @current-change="loadData"
      />
    </div>

    <FinanceFreightForwarderPaymentPayDialog
      v-model="payDialogVisible"
      :receipt-id="payReceiptId"
      :pending-amount="payPendingAmount"
      :receipt-currency="payReceiptCurrency"
      :freight-forwarder-company-id="payFfCompanyId"
      @success="onPaySuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  FF_PAYABLE_STATUS,
  financeFreightForwarderPayableApi,
  type FfPayableListItem
} from '@/api/financeFreightForwarderPayable'
import { fetchFreightForwarderCompanies, type FreightForwarderCompany } from '@/api/freightForwarderCompany'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import FinanceFreightForwarderPaymentPayDialog from '@/components/Finance/FinanceFreightForwarderPaymentPayDialog.vue'

const { t } = useI18n()
const router = useRouter()
const { canWriteFinanceReceipt } = useFinanceWriteGate()

const loading = ref(false)
const tableData = ref<FfPayableListItem[]>([])
const total = ref(0)
const ffCompanies = ref<FreightForwarderCompany[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const query = reactive({
  keyword: '',
  payableStatus: undefined as number | undefined,
  freightForwarderCompanyId: '',
  page: 1,
  pageSize: 20
})

const payDialogVisible = ref(false)
const payReceiptId = ref('')
const payPendingAmount = ref(0)
const payReceiptCurrency = ref(1)
const payFfCompanyId = ref<string | undefined>(undefined)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 148
const OP_COL_EXPANDED_MIN_WIDTH = 132
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))

function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'payableStatus', label: t('financeFfPayableList.colStatus'), prop: 'payableStatus', width: 100, align: 'center' },
  { key: 'financeReceiptCode', label: t('financeFfPayableList.colReceiptCode'), prop: 'financeReceiptCode', minWidth: 130, showOverflowTooltip: true },
  { key: 'customerName', label: t('financeFfPayableList.colCustomer'), prop: 'customerName', minWidth: 140, showOverflowTooltip: true },
  { key: 'freightForwarderCompanyName', label: t('financeFfPayableList.colFfCompany'), prop: 'freightForwarderCompanyName', minWidth: 140, showOverflowTooltip: true },
  { key: 'receiptAmount', label: t('financeFfPayableList.colReceiptAmount'), prop: 'receiptAmount', width: 120, align: 'right' },
  { key: 'paidAmount', label: t('financeFfPayableList.colPaidAmount'), prop: 'paidAmount', width: 120, align: 'right' },
  { key: 'pendingAmount', label: t('financeFfPayableList.colPendingAmount'), prop: 'pendingAmount', width: 120, align: 'right' },
  { key: 'receiptCurrency', label: t('financeFfPayableList.colCurrency'), prop: 'receiptCurrency', width: 80, align: 'center' },
  { key: 'receiptDate', label: t('financeFfPayableList.colReceiptDate'), prop: 'receiptDate', width: 120 },
  {
    key: 'actions',
    label: t('financeFfPayableList.colActions'),
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
])

function statusLabel(status: number) {
  if (status === FF_PAYABLE_STATUS.Partial) return t('financeFfPayableList.statusPartial')
  if (status === FF_PAYABLE_STATUS.Completed) return t('financeFfPayableList.statusCompleted')
  return t('financeFfPayableList.statusPending')
}

function statusTagType(status: number) {
  if (status === FF_PAYABLE_STATUS.Completed) return 'success'
  if (status === FF_PAYABLE_STATUS.Partial) return 'warning'
  return 'info'
}

function handleSearch() {
  query.page = 1
  loadData()
}

function resetFilters() {
  query.keyword = ''
  query.payableStatus = undefined
  query.freightForwarderCompanyId = ''
  handleSearch()
}

function onPageSizeChange() {
  query.page = 1
  loadData()
}

async function loadData() {
  loading.value = true
  try {
    const res = await financeFreightForwarderPayableApi.getList({
      keyword: query.keyword || undefined,
      payableStatus: query.payableStatus,
      freightForwarderCompanyId: query.freightForwarderCompanyId || undefined,
      page: query.page,
      pageSize: query.pageSize
    })
    tableData.value = res.items || []
    total.value = res.total || 0
    const maxPage = Math.max(1, Math.ceil(total.value / query.pageSize) || 1)
    if (query.page > maxPage) {
      query.page = maxPage
      if (total.value > 0) await loadData()
    }
  } finally {
    loading.value = false
  }
}

function openDetail(row: FfPayableListItem) {
  router.push({ name: 'FinanceFreightForwarderPayableDetail', params: { id: row.receiptId } })
}

function openPay(row: FfPayableListItem) {
  payReceiptId.value = row.receiptId
  payPendingAmount.value = row.pendingAmount
  payReceiptCurrency.value = row.receiptCurrency
  payFfCompanyId.value = row.freightForwarderCompanyId || undefined
  payDialogVisible.value = true
}

function onPaySuccess() {
  payDialogVisible.value = false
  loadData()
}

function goCompanyManage() {
  router.push({ name: 'FreightForwarderCompanyManage' })
}

onMounted(async () => {
  ffCompanies.value = await fetchFreightForwarderCompanies(true)
  await loadData()
})
</script>

<style lang="scss" scoped>
@import './finance-common.scss';

.page-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

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
